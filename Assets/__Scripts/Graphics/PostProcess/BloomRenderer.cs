using System;
using UnityEngine;
using UnityEngine.Rendering;

// Custom bloom for the editor cameras, replacing Unity's Post Processing v2
// stack. The scene owns this component. PyramidBloomController calls
// it to produce the bloom-only texture.
//
// The bloom pyramid mirrors the game's PyramidBloomRendererSO.RenderBloom and the
// previous bloom renderer (see Assets/_Graphics/Shaders/README.md): a configured-width pyramid
// with an alpha-gated prefilter, 13-tap downsampling, and tent upsampling,
// and a separate scene compositor. The driver
// mirrors RenderBloom: the iteration/LOD formula, the per-level
// _CombineParams merge weights (including the num8 first/final upsample
// brightness scaling), and the initial (intensity, 1, 0, 0) _CombineParams
// default. Unlike bloom fog, main bloom uses a plain tent merge for its final
// pass. Chromatic
// aberration is a separate scene component with its own shader. The controller
// runs chromatic aberration after bloom. AA is camera MSAA (CameraController UpdateAA ->
// QualitySettings.antiAliasing), matching the game, which has no post-process AA.
public class BloomRenderer : MonoBehaviour
{
    [SerializeField] private Shader bloomShader;
    [SerializeField] private PyramidBloomProfileSO bloomProfile;

    [Space]
    // Values from the captured main-effect asset. Intensity shapes the
    // per-level merge weights. It is not a final bloom multiplier.
    [SerializeField] private float intensity = 1f;
    [SerializeField] private float radius = 5f;
    // Per-level merge weight shaping:
    // x = min(1, pow(intensity * (level + 1) / (levels - 1),
    // pyramidWeightsParam)), y = min(1, 1 + downBloomIntensityOffset - x).
    // With the captured values, y is 1 and x remains close to 1.
    [SerializeField] private float pyramidWeightsParam = 0.01f;
    [SerializeField] private float downBloomIntensityOffset = 1f;
    // Game _alphaWeights (default 4): the prefilter's alpha-gate strength
    // (rgb *= saturate(alpha * k)), serialized on the renderer exactly like
    // the game's PyramidBloomSO. Applied to _BloomParams.z before
    // the pyramid is recorded.
    [SerializeField] private float bloomThreshold = 4f;
    // Game firstUpsampleBrightness / finalUpsampleBrightness (both authored 1
    // in PyramidBloomSO's RenderBloom call): the game's num8 scales
    // BOTH merge weights on the first and final (i == 0) upsample merges
    // (PyramidBloomRendererSO.RenderBloom:186-194).
    [SerializeField] private float firstUpsampleBrightness = 1f;
    [SerializeField] private float finalUpsampleBrightness = 1f;

    private static readonly int sampleScaleId = Shader.PropertyToID("_SampleScale");
    private static readonly int combineParamsId = Shader.PropertyToID("_CombineParams");
    private static readonly int bloomTexId = Shader.PropertyToID("_BloomTex");
    private static readonly int bloomParamsId = Shader.PropertyToID("_BloomParams");
    private static readonly int bloomTexelSizeId = Shader.PropertyToID("_BloomTexelSize");
    private static readonly int[] mipDownIds =
        BloomRenderUtility.CreateTextureIds("_BloomMipDown_");
    private static readonly int[] mipUpIds =
        BloomRenderUtility.CreateTextureIds("_BloomMipUp_");

    private Material bloomMaterial;
    private bool bloomEnabled;
    private bool settingsCallbackSubscribed;

    public bool IsReady => isActiveAndEnabled && bloomEnabled && bloomMaterial != null;

    private void OnEnable()
    {
        UpdateBloom(Settings.Instance.Bloom);
        if (!settingsCallbackSubscribed)
        {
            Settings.NotifyBySettingName(nameof(Settings.Bloom), UpdateBloom);
            settingsCallbackSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(nameof(Settings.Bloom), UpdateBloom);
            settingsCallbackSubscribed = false;
        }
    }

    private void Start()
    {
        bloomMaterial = new Material(bloomShader);
        UpdateBloom(Settings.Instance.Bloom);
    }

    private void OnDestroy()
    {
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(nameof(Settings.Bloom), UpdateBloom);
            settingsCallbackSubscribed = false;
        }

        if (bloomMaterial != null) Destroy(bloomMaterial);
    }

    public void UpdateBloom(object obj) => bloomEnabled = Convert.ToBoolean(obj);

    public RenderTextureDescriptor GetOutputDescriptor(
        int sourceWidth, int sourceHeight, int targetWidth)
    {
        var targetHeight = Mathf.Max(1,
            (int)(targetWidth * (sourceHeight / (float)sourceWidth)));
        return BloomRenderUtility.CreateDescriptor(
            targetWidth, targetHeight, BloomRenderUtility.GetBloomTextureFormat());
    }

    public void RecordRender(
        CommandBuffer commandBuffer,
        RenderTargetIdentifier source,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        RenderTargetIdentifier destination)
    {
        var outputDescriptor = GetOutputDescriptor(sourceWidth, sourceHeight, targetWidth);

        // The first downsample pass uses the camera source. Later passes use
        // the previous pyramid level.
        BloomRenderUtility.CalculatePyramidParameters(
            outputDescriptor.width, outputDescriptor.height, GetRadius(), out var iterations, out var sampleScale);
        var descriptors = BloomRenderUtility.BuildPyramidDescriptors(
            outputDescriptor.width,
            outputDescriptor.height,
            iterations,
            outputDescriptor.colorFormat);

        // The game pre-sets _CombineParams to its (intensity, 1, 0, 0) default
        // once before the pyramid (RenderBloom:167). The downsample passes do
        // not read it and the upsample loop overwrites it per merge, so this
        // mirrors the game's driver ordering exactly.
        commandBuffer.SetGlobalVector(
            bloomParamsId, new Vector4(0f, 0f, GetBloomThreshold(), 0f));
        commandBuffer.SetGlobalVector(combineParamsId, new Vector4(GetIntensity(), 1f, 0f, 0f));

        var lastDown = source;
        var lastDownWidth = sourceWidth;
        var lastDownHeight = sourceHeight;
        for (var i = 0; i < iterations; i++)
        {
            commandBuffer.GetTemporaryRT(mipDownIds[i], descriptors[i], FilterMode.Bilinear);
            commandBuffer.SetGlobalVector(
                bloomTexelSizeId, GetTexelSize(lastDownWidth, lastDownHeight));
            commandBuffer.Blit(
                lastDown, mipDownIds[i], bloomMaterial, i == 0 ? 0 : 2);
            lastDown = new RenderTargetIdentifier(mipDownIds[i]);
            lastDownWidth = descriptors[i].width;
            lastDownHeight = descriptors[i].height;
        }

        // Upsample all but the final level. The captured runtime main effect
        // uses the same tent kernel for the final merge.
        commandBuffer.SetGlobalFloat(sampleScaleId, sampleScale);
        var lastUp = new RenderTargetIdentifier(mipDownIds[iterations - 1]);
        var lastUpWidth = descriptors[iterations - 1].width;
        var lastUpHeight = descriptors[iterations - 1].height;
        for (var i = iterations - 2; i >= 1; i--)
        {
            commandBuffer.GetTemporaryRT(mipUpIds[i], descriptors[i], FilterMode.Bilinear);
            commandBuffer.SetGlobalVector(bloomTexelSizeId, GetTexelSize(lastUpWidth, lastUpHeight));
            commandBuffer.SetGlobalTexture(bloomTexId, mipDownIds[i]);
            SetMergeParams(
                commandBuffer, i, iterations, i == iterations - 2 ? GetFirstUpsampleBrightness() : 1f);
            commandBuffer.Blit(lastUp, mipUpIds[i], bloomMaterial, 5);
            lastUp = new RenderTargetIdentifier(mipUpIds[i]);
            lastUpWidth = descriptors[i].width;
            lastUpHeight = descriptors[i].height;
        }

        commandBuffer.SetGlobalTexture(bloomTexId, mipDownIds[0]);
        commandBuffer.SetGlobalVector(bloomTexelSizeId, GetTexelSize(lastUpWidth, lastUpHeight));

        if (iterations == 1)
            commandBuffer.SetGlobalVector(combineParamsId, new Vector4(1f, 0f, 0f, 0f));
        else
            SetMergeParams(commandBuffer, 0, iterations, GetFinalUpsampleBrightness());

        // The captured runtime main effect uses UpsampleTent for intermediate
        // and final merges. Auto-exposure and ACES belong only to bloom fog.
        commandBuffer.Blit(lastUp, destination, bloomMaterial, 5);

        commandBuffer.SetGlobalTexture(bloomTexId, Texture2D.blackTexture);
        for (var i = 0; i < iterations; i++) commandBuffer.ReleaseTemporaryRT(mipDownIds[i]);
        for (var i = 1; i < iterations - 1; i++) commandBuffer.ReleaseTemporaryRT(mipUpIds[i]);
    }

    private void SetMergeParams(
        CommandBuffer commandBuffer, int level, int iterations, float brightness)
    {
        var mergeWeights = BloomRenderUtility.CalculateMergeWeights(
            GetIntensity(), GetDownIntensityOffset(), GetPyramidWeightsParam(), level, iterations);
        commandBuffer.SetGlobalVector(
            combineParamsId,
            new Vector4(
                mergeWeights.x * brightness,
                mergeWeights.y * brightness,
                0f,
                0f));
    }

    private static Vector4 GetTexelSize(int width, int height) =>
        new(1f / width, 1f / height, width, height);

    private float GetRadius() => bloomProfile == null ? radius : bloomProfile.Radius;
    private float GetIntensity() => bloomProfile == null ? intensity : bloomProfile.Intensity;
    private float GetPyramidWeightsParam() =>
        bloomProfile == null ? pyramidWeightsParam : bloomProfile.PyramidWeightsParam;
    private float GetDownIntensityOffset() =>
        bloomProfile == null ? downBloomIntensityOffset : bloomProfile.DownIntensityOffset;
    private float GetFirstUpsampleBrightness() =>
        bloomProfile == null ? firstUpsampleBrightness : bloomProfile.FirstUpsampleBrightness;
    private float GetFinalUpsampleBrightness() =>
        bloomProfile == null ? finalUpsampleBrightness : bloomProfile.FinalUpsampleBrightness;
    private float GetBloomThreshold() => bloomProfile == null ? bloomThreshold : bloomProfile.BloomThreshold;
}
