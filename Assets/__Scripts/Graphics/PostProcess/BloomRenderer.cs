using System;
using UnityEngine;
using UnityEngine.Rendering;

// Records an alpha-gated downsample pyramid and tent-upsampled bloom texture.
public class BloomRenderer : MonoBehaviour
{
    [SerializeField] private Shader bloomShader;
    [SerializeField] private PyramidBloomProfileSO bloomProfile;

    [Space]
    // Fallback controls used only when no profile is assigned.
    [SerializeField] private float intensity = 1f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float pyramidWeightsParam = 0.01f;
    [SerializeField] private float downBloomIntensityOffset = 1f;
    [SerializeField] private float bloomThreshold = 4f;
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
            MapGraphicsSettings.Changed += HandleMapGraphicsChanged;
            settingsCallbackSubscribed = true;
        }
    }

    private void OnDisable()
    {
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(nameof(Settings.Bloom), UpdateBloom);
            MapGraphicsSettings.Changed -= HandleMapGraphicsChanged;
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
            MapGraphicsSettings.Changed -= HandleMapGraphicsChanged;
            settingsCallbackSubscribed = false;
        }

        if (bloomMaterial != null) Destroy(bloomMaterial);
    }

    private void HandleMapGraphicsChanged() => UpdateBloom(Settings.Instance.Bloom);

    public void UpdateBloom(object obj) => bloomEnabled = MapGraphicsSettings.Bloom ?? Convert.ToBoolean(obj);

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

        // Level zero reads the camera copy; each later level reads its predecessor.
        BloomRenderUtility.CalculatePyramidParameters(
            outputDescriptor.width, outputDescriptor.height, GetRadius(), out var iterations, out var sampleScale);
        var descriptors = BloomRenderUtility.BuildPyramidDescriptors(
            outputDescriptor.width,
            outputDescriptor.height,
            iterations,
            outputDescriptor.colorFormat);

        // Initialize merge state before the pyramid; each multi-level upsample replaces it.
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

        // Intermediate and final merges use the same tent kernel.
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

        // Main bloom stops at the tent merge; auto-exposure and ACES are bloom-fog stages.
        commandBuffer.Blit(lastUp, destination, bloomMaterial, 5);

        // Clear borrowed global bindings before releasing command-buffer temporaries.
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
