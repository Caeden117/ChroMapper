using UnityEngine;

public class BloomfogRenderingController : MonoBehaviour
{
    public static BloomfogRenderingController Instance { get; private set; }

    public readonly struct GlobalState
    {
        internal readonly Texture bloomTexture;
        internal readonly Vector4 textureToScreenRatio;
        internal readonly bool bloomFogEnabled;
        internal readonly bool acesToneMappingEnabled;
        internal readonly Vector4 bloomParams;
        internal readonly Vector4 combineParams;
        internal readonly Vector4 bloomTexelSize;
        internal readonly float sampleScale;
        internal readonly Texture bloomTex;
        internal readonly Texture globalIntensityTex;
        internal readonly Vector4 stereoCameraEyeOffsets;

        internal GlobalState(
            Texture bloomTexture,
            Vector4 textureToScreenRatio,
            bool bloomFogEnabled,
            bool acesToneMappingEnabled,
            Vector4 bloomParams,
            Vector4 combineParams,
            Vector4 bloomTexelSize,
            float sampleScale,
            Texture bloomTex,
            Texture globalIntensityTex,
            Vector4 stereoCameraEyeOffsets)
        {
            this.bloomTexture = bloomTexture;
            this.textureToScreenRatio = textureToScreenRatio;
            this.bloomFogEnabled = bloomFogEnabled;
            this.acesToneMappingEnabled = acesToneMappingEnabled;
            this.bloomParams = bloomParams;
            this.combineParams = combineParams;
            this.bloomTexelSize = bloomTexelSize;
            this.sampleScale = sampleScale;
            this.bloomTex = bloomTex;
            this.globalIntensityTex = globalIntensityTex;
            this.stereoCameraEyeOffsets = stereoCameraEyeOffsets;
        }
    }

    private const int skyboxLayer = 29;
    private const int downscalePass = 3;
    private const int upscalePass = 5;
    private const int finalUpscalePass = 13;
    private const int boxUpscalePass = 6;
    private const string bloomFogKeyword = "BLOOM_FOG";
    private const string acesToneMappingKeyword = "ACES_TONE_MAPPING";

    private const int bloomFogResolution = 512;

    private static readonly int combineParamsId = Shader.PropertyToID("_CombineParams");
    private static readonly int sampleScaleId = Shader.PropertyToID("_SampleScale");
    private static readonly int bloomTexId = Shader.PropertyToID("_BloomTex");
    private static readonly int bloomTexelSizeId = Shader.PropertyToID("_BloomTexelSize");
    private static readonly int globalIntensityTexId = Shader.PropertyToID("_GlobalIntensityTex");
    private static readonly int bloomParamsId = Shader.PropertyToID("_BloomParams");
    private static readonly int bloomPrePassTextureId = Shader.PropertyToID("_BloomPrePassTexture");
    private static readonly int customFogTextureToScreenRatioId =
        Shader.PropertyToID("_CustomFogTextureToScreenRatio");
    private static readonly int stereoCameraEyeOffsetsId =
        Shader.PropertyToID("_StereoCameraEyeOffsets");

    [SerializeField] private Shader blurShader;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private BloomfogRendererSO bloomfogRenderer;
    [SerializeField] private PyramidBloomProfileSO bloomProfile;
    [SerializeField] private MeshFilter skyboxQuadMeshFilter;
    [SerializeField] private MeshRenderer skyboxQuadRenderer;
    [Space]
    // Changing these v does nothing, they're set in the Mapper scene itself which overrides this
    [SerializeField] private float bloomIntensity = 0.75f;
    [SerializeField] private float bloomRadius = 10f;
    [SerializeField] private float pyramidWeightsParam = 1f;
    [SerializeField] private float downIntensityOffset = 1f;
    [SerializeField] private float firstUpscaleBrightness = 1.2f;
    [SerializeField] private float finalUpscaleBrightness = 0.25f;
    // Changing these ^ does nothing, they're set in the Mapper scene itself which overrides this
    /*
        Open Assets/__Scenes/03_Mapper.unity.
        In the Hierarchy panel, search for Bloomfog Renderer.
        Select it.
        In the Inspector panel, find the Bloomfog Rendering Controller component.
        Edit the bloom fields there, then save the scene with Ctrl+S.
    */

    private Camera activeCamera;
    private Material blurMaterial;
    private CameraClearFlags previousClearFlags;
    private int previousSkyboxLayerMask;
    private bool cameraConfigured;
    private Material previousRenderSettingsSkybox;
    private bool renderSettingsSkyboxSuppressed;
    private Mesh skyboxQuadMesh;

    private RenderTexture bloomfogRaw = null;
    private RenderTexture bloomfogTex = null;
    private RenderTexture publishedBloomfogTex = null;
    private bool hasPublishedBloomfogTexture;
    private readonly Level[] bloomfogPasses =
        new Level[BloomRenderUtility.MaxPyramidSize];
    private bool active;
    private bool bloomFogEnabled;
    private bool settingsCallbackSubscribed;
    private bool bloomFogKeywordWasEnabled;
    private float bloomFogAutoExposureLimit = 1000f;
    private bool bloomFogLegacyAutoExposure;

    public bool CanRenderReflections => active && bloomFogEnabled && blurMaterial != null;

    public GlobalState CaptureGlobalState() => new(
        Shader.GetGlobalTexture(bloomPrePassTextureId),
        Shader.GetGlobalVector(customFogTextureToScreenRatioId),
        Shader.IsKeywordEnabled(bloomFogKeyword),
        Shader.IsKeywordEnabled(acesToneMappingKeyword),
        Shader.GetGlobalVector(bloomParamsId),
        Shader.GetGlobalVector(combineParamsId),
        Shader.GetGlobalVector(bloomTexelSizeId),
        Shader.GetGlobalFloat(sampleScaleId),
        Shader.GetGlobalTexture(bloomTexId),
        Shader.GetGlobalTexture(globalIntensityTexId),
        Shader.GetGlobalVector(stereoCameraEyeOffsetsId));

    public void RestoreGlobalState(GlobalState state)
    {
        Shader.SetGlobalTexture(bloomPrePassTextureId, state.bloomTexture);
        Shader.SetGlobalVector(customFogTextureToScreenRatioId, state.textureToScreenRatio);
        SetKeyword(bloomFogKeyword, state.bloomFogEnabled);
        SetKeyword(acesToneMappingKeyword, state.acesToneMappingEnabled);
        Shader.SetGlobalVector(bloomParamsId, state.bloomParams);
        Shader.SetGlobalVector(combineParamsId, state.combineParams);
        Shader.SetGlobalVector(bloomTexelSizeId, state.bloomTexelSize);
        Shader.SetGlobalFloat(sampleScaleId, state.sampleScale);
        Shader.SetGlobalTexture(bloomTexId, state.bloomTex);
        Shader.SetGlobalTexture(globalIntensityTexId, state.globalIntensityTex);
        Shader.SetGlobalVector(stereoCameraEyeOffsetsId, state.stereoCameraEyeOffsets);
    }

    public void RenderReflection(
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix,
        RenderTexture rawTexture,
        RenderTexture finalTexture)
    {
        if (!CanRenderReflections || rawTexture == null || finalTexture == null) return;

        SetKeyword(bloomFogKeyword, true);
        SetKeyword(acesToneMappingKeyword, true);
        bloomfogRenderer.RenderToTexture(
            viewMatrix, projectionMatrix, rawTexture, out _);
        RenderBloomTexture(rawTexture, finalTexture, boxUpscalePass);
        Shader.SetGlobalTexture(bloomPrePassTextureId, finalTexture);
        bloomfogRenderer.PublishGlobals();
    }

    public void AssignToCamera(CameraController cameraController)
    {
        DetachCamera();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        AttachCamera();
    }

    private void Start()
    {
        blurMaterial = new Material(blurShader);
        SetProfileDefaults();
        if (bloomFogEnabled) Activate();
    }

    private void OnEnable()
    {
        Instance = this;
        UpdateBloomFog(Settings.Instance.BloomFog);
        if (!settingsCallbackSubscribed)
        {
            Settings.NotifyBySettingName(nameof(Settings.BloomFog), UpdateBloomFog);
            settingsCallbackSubscribed = true;
        }
        if (blurMaterial != null && bloomFogEnabled) Activate();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (settingsCallbackSubscribed)
        {
            Settings.StopNotifyingBySettingName(nameof(Settings.BloomFog), UpdateBloomFog);
            settingsCallbackSubscribed = false;
        }
        Deactivate();
    }

    private void Activate()
    {
        if (active) return;
        if (blurMaterial == null || bloomfogRenderer == null) return;
        bloomfogRenderer.Initialize();
        InitializeSkyboxQuad();
        bloomFogKeywordWasEnabled = Shader.IsKeywordEnabled(bloomFogKeyword);

        if (context != null && context.Descriptor != null)
            HandleEnvironmentLoaded(context.Descriptor);
        else
            HandleEnvironmentUnloaded();
        RegenerateRenderTexture();

        active = true;
        Camera.onPreRender += OnCameraPreRender;
        Camera.onPostRender += OnCameraPostRender;
        if (context != null)
        {
            context.OnEnvironmentLoaded += HandleEnvironmentLoaded;
            context.OnEnvironmentUnloaded += HandleEnvironmentUnloaded;
        }
        AttachCamera();
        SuppressRenderSettingsSkybox();
        if (skyboxQuadRenderer != null) skyboxQuadRenderer.enabled = true;
    }

    private void Deactivate()
    {
        var wasActive = active;
        if (active)
        {
            active = false;
            Camera.onPreRender -= OnCameraPreRender;
            Camera.onPostRender -= OnCameraPostRender;
            if (context != null)
            {
                context.OnEnvironmentLoaded -= HandleEnvironmentLoaded;
                context.OnEnvironmentUnloaded -= HandleEnvironmentUnloaded;
            }
        }
        if (wasActive) SetKeyword(bloomFogKeyword, bloomFogKeywordWasEnabled);
        // PerCameraShaderSetupController owns the ACES lifecycle. Bloom fog only
        // selects ACES for its bounded render passes and must not restore a stale snapshot here.
        Shader.SetGlobalTexture(bloomPrePassTextureId, null);
        Shader.SetGlobalVector("_CustomFogTextureToScreenRatio", Vector2.zero);
        Shader.SetGlobalFloat("_CustomFogOffset", 0f);
        Shader.SetGlobalVector(stereoCameraEyeOffsetsId, Vector2.zero);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", 0f);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", 0f);
        Shader.SetGlobalFloat("_CustomFogAttenuation", 0f);
        if (skyboxQuadRenderer != null) skyboxQuadRenderer.enabled = false;
        RestoreRenderSettingsSkybox();
        DetachCamera();
        ClearRenderTextures();
    }

    // Render bloomfog and perform blur passes before the active editor camera renders
    // This ensures the main render has up-to-date bloomfog texture
    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera) return;
        if (bloomfogRaw == null || bloomfogTex == null ||
            !bloomfogRaw.IsCreated() || !bloomfogTex.IsCreated()) return;

        // The same material used by the game-style fullscreen quad was also
        // assigned as Unity's global skybox in the old mapper setup. Rendering
        // both produces duplicate fog images because this shader expects quad
        // clip-space vertices, not a skybox cube.
        SuppressRenderSettingsSkybox();
        SetKeyword(bloomFogKeyword, true);
        // The HD bloom-prepass effect selects ACES for every prepass phase and
        // for the scene render that follows it.
        SetKeyword(acesToneMappingKeyword, true);

        // Render bloomfog to raw texture
        Shader.SetGlobalTexture(
            bloomPrePassTextureId,
            hasPublishedBloomfogTexture ? publishedBloomfogTex : Texture2D.blackTexture);
        bloomfogRenderer.RenderToTexture(activeCamera, bloomfogRaw, out _);
        RenderBloomTexture(bloomfogRaw, bloomfogTex, upscalePass);
        Shader.SetGlobalTexture(bloomPrePassTextureId, bloomfogTex);
        bloomfogRenderer.PublishGlobals();

        (bloomfogTex, publishedBloomfogTex) = (publishedBloomfogTex, bloomfogTex);
        hasPublishedBloomfogTexture = true;
    }

    private void RenderBloomTexture(
        RenderTexture rawTexture,
        RenderTexture finalTexture,
        int intermediateUpscalePass)
    {
        // Beat Saber downsamples the first pyramid level before it starts the
        // blur. The final/raw targets retain their authored size.
        var descriptor = BloomRenderUtility.CreateDescriptor(
            Mathf.Max(finalTexture.width / 2, 1),
            Mathf.Max(finalTexture.height / 2, 1),
            finalTexture.format);

        // Determine number of passes based on resolution and radius
        BloomRenderUtility.CalculatePyramidParameters(
            descriptor.width,
            descriptor.height,
            bloomRadius,
            out var realBloomfogPasses,
            out var blurRadius);

        try
        {
            // Main bloom uses the same shader globals later in the camera
            // render. Restore bloom fog's exposure values every frame before
            // any fog pass reads them.
            Shader.SetGlobalVector(
                bloomParamsId,
                new Vector4(
                    bloomFogAutoExposureLimit,
                    blurRadius,
                    0f,
                    bloomFogLegacyAutoExposure ? 1f : 0f));

            // The game initializes the combine vector before the pyramid. The
            // non-uniform path replaces it on each upsample, but keeping this
            // ordering also keeps the one-level route well-defined.
            SetCombineStrengths(bloomIntensity, 1f);
            Shader.SetGlobalFloat(sampleScaleId, blurRadius);

            // Downscale. Pass 0 is the authored 4-tap downsample for both HD
            // and LD bloom; the first pass is intentionally not skipped.
            var downscaleSrc = (Texture)rawTexture;
            for (var i = 0; i < realBloomfogPasses; i++)
            {
                bloomfogPasses[i].down = BloomRenderUtility.GetTemporary(descriptor);

                SetSourceTexture(downscaleSrc);
                Graphics.Blit(downscaleSrc, bloomfogPasses[i].down, blurMaterial, downscalePass);

                downscaleSrc = bloomfogPasses[i].down;
                descriptor.width = Mathf.Max(descriptor.width / 2, 1);
                descriptor.height = Mathf.Max(descriptor.height / 2, 1);
            }

            // The smallest downsampled level is the game's auto-exposure
            // probe. Keep the material binding alive through the final pass.
            Shader.SetGlobalTexture(globalIntensityTexId, downscaleSrc);

            var upscaleSrc = bloomfogPasses[realBloomfogPasses - 1].down;
            if (realBloomfogPasses == 1)
            {
                // There is no destination-level bloom to merge at one level.
                // The accumulated source is the complete input to the final
                // tent/knee/ACES pass.
                SetCombineStrengths(1f, 0f);
                SetPreviousTexture(Texture2D.blackTexture);
                SetSourceTexture(upscaleSrc);
                Graphics.Blit(upscaleSrc, finalTexture, blurMaterial, finalUpscalePass);
            }
            else
            {
                for (var i = realBloomfogPasses - 2; i >= 0; i--)
                {
                    // x weights the destination level. y weights the blurred,
                    // accumulated pyramid source.
                    var mergeWeights = BloomRenderUtility.CalculateMergeWeights(
                        bloomIntensity,
                        downIntensityOffset,
                        pyramidWeightsParam,
                        i,
                        realBloomfogPasses);
                    var brightness = 1f;

                    if (i == 0)
                        brightness = finalUpscaleBrightness;
                    else if (i == realBloomfogPasses - 2)
                        brightness = firstUpscaleBrightness;

                    SetCombineStrengths(
                        mergeWeights.x * brightness,
                        mergeWeights.y * brightness);
                    SetPreviousTexture(bloomfogPasses[i].down);
                    SetSourceTexture(upscaleSrc);

                    var upscaleDst = i == 0 ? finalTexture : GetUpscaleTexture(i, finalTexture.format);
                    var shaderPass = i == 0 ? finalUpscalePass : intermediateUpscalePass;

                    Graphics.Blit(upscaleSrc, upscaleDst, blurMaterial, shaderPass);
                    upscaleSrc = upscaleDst;
                }
            }

            // After the final bloom pass, draw non-light prepass objects into
            // the published texture with the authored ACES state.
            bloomfogRenderer.RenderAfterBlur(finalTexture);
        }
        finally
        {
            ClearMaterialTextures();
            ReleaseTemporaryPyramid();
        }
    }

    private void OnCameraPostRender(Camera renderingCamera)
    {
        if (renderingCamera != activeCamera || bloomFogKeywordWasEnabled) return;
        SetKeyword(bloomFogKeyword, false);
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        if (descriptor == null) return;
        if (descriptor.BloomFogParams == null)
        {
            HandleEnvironmentUnloaded();
            return;
        }

        UpdateBloomFogParams(
            descriptor.BloomFogParams.AutoExposureLimit,
            descriptor.BloomFogParams.Offset,
            descriptor.BloomFogParams.Height,
            descriptor.BloomFogParams.StartY,
            descriptor.BloomFogParams.Attenuation,
            descriptor.BloomFogParams.LegacyAutoExposure);
    }

    private void HandleEnvironmentUnloaded() =>
        UpdateBloomFogParams(
            bloomProfile == null ? 1000f : bloomProfile.AutoExposureLimit,
            0f,
            25f,
            -50f,
            0.00025f,
            bloomProfile != null && bloomProfile.LegacyAutoExposure);

    private void OnDestroy()
    {
        OnDisable();
        if (bloomfogRenderer != null) bloomfogRenderer.Release();
        ReleaseSkyboxQuad();
        ClearRenderTextures();
        if (blurMaterial != null)
        {
            if (Application.isPlaying) Destroy(blurMaterial);
            else DestroyImmediate(blurMaterial);
            blurMaterial = null;
        }
    }

    private void UpdateBloomFog(object value)
    {
        bloomFogEnabled = System.Convert.ToBoolean(value);
        if (bloomFogEnabled)
        {
            if (isActiveAndEnabled && blurMaterial != null) Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private void SetProfileDefaults()
    {
        if (bloomProfile == null) return;
        bloomIntensity = bloomProfile.Intensity;
        bloomRadius = bloomProfile.Radius;
        pyramidWeightsParam = bloomProfile.PyramidWeightsParam;
        downIntensityOffset = bloomProfile.DownIntensityOffset;
        firstUpscaleBrightness = bloomProfile.FirstUpsampleBrightness;
        finalUpscaleBrightness = bloomProfile.FinalUpsampleBrightness;
    }

    private void InitializeSkyboxQuad()
    {
        if (skyboxQuadMeshFilter == null || skyboxQuadMesh != null) return;

        skyboxQuadMesh = new Mesh
        {
            name = "Bloom Skybox Quad",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = new[]
            {
                new Vector3(-1f, -1f, 0f),
                new Vector3(1f, -1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(-1f, 1f, 0f)
            },
            triangles = new[] { 0, 1, 2, 2, 3, 0 },
            bounds = new Bounds(Vector3.zero, Vector3.one * 100000000f)
        };
        skyboxQuadMeshFilter.sharedMesh = skyboxQuadMesh;
    }

    private void ReleaseSkyboxQuad()
    {
        if (skyboxQuadMesh == null) return;
        if (skyboxQuadMeshFilter != null && skyboxQuadMeshFilter.sharedMesh == skyboxQuadMesh)
            skyboxQuadMeshFilter.sharedMesh = null;
        if (Application.isPlaying) Destroy(skyboxQuadMesh);
        else DestroyImmediate(skyboxQuadMesh);
        skyboxQuadMesh = null;
    }

    private void AttachCamera()
    {
        if (!active || activeCamera == null || cameraConfigured) return;

        previousClearFlags = activeCamera.clearFlags;
        var layerBit = 1 << skyboxLayer;
        previousSkyboxLayerMask = activeCamera.cullingMask & layerBit;
        activeCamera.clearFlags = CameraClearFlags.Color;
        activeCamera.cullingMask |= layerBit;
        cameraConfigured = true;
    }

    private void DetachCamera()
    {
        if (!cameraConfigured) return;

        if (activeCamera != null)
        {
            activeCamera.clearFlags = previousClearFlags;
            var layerBit = 1 << skyboxLayer;
            activeCamera.cullingMask =
                (activeCamera.cullingMask & ~layerBit) | previousSkyboxLayerMask;
        }

        cameraConfigured = false;
    }

    private void SuppressRenderSettingsSkybox()
    {
        if (!renderSettingsSkyboxSuppressed)
        {
            previousRenderSettingsSkybox = RenderSettings.skybox;
            renderSettingsSkyboxSuppressed = true;
        }

        RenderSettings.skybox = null;
    }

    private void RestoreRenderSettingsSkybox()
    {
        if (!renderSettingsSkyboxSuppressed) return;
        if (RenderSettings.skybox == null)
            RenderSettings.skybox = previousRenderSettingsSkybox;
        previousRenderSettingsSkybox = null;
        renderSettingsSkyboxSuppressed = false;
    }

    private void UpdateBloomFogParams(
        float autoExposureLimit,
        float offset,
        float height,
        float startY,
        float attenuation,
        bool legacyAutoExposure)
    {
        bloomFogAutoExposureLimit = autoExposureLimit;
        bloomFogLegacyAutoExposure = legacyAutoExposure;
        Shader.SetGlobalFloat("_CustomFogOffset", offset);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", startY);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", height);
        Shader.SetGlobalFloat("_CustomFogAttenuation", attenuation);
    }

    private void ClearRenderTextures()
    {
        ReleaseOwnedRenderTexture(ref bloomfogRaw);
        ReleaseOwnedRenderTexture(ref bloomfogTex);
        ReleaseOwnedRenderTexture(ref publishedBloomfogTex);
        hasPublishedBloomfogTexture = false;
    }

    private void RegenerateRenderTexture()
    {
        Shader.SetGlobalTexture(bloomPrePassTextureId, null);
        ClearRenderTextures();

        var width = bloomFogResolution;
        var height = bloomFogResolution;
        var format = BloomRenderUtility.GetBloomTextureFormat();

        try
        {
            bloomfogTex = CreateOwnedRenderTexture(width, height, format, "Bloomfog Final Texture");
            publishedBloomfogTex = CreateOwnedRenderTexture(
                width, height, format, "Bloomfog Published Texture");
            bloomfogRaw = CreateOwnedRenderTexture(width, height, format, "Bloomfog Raw Texture");
        }
        catch
        {
            ClearRenderTextures();
            throw;
        }

        Shader.SetGlobalTexture(bloomPrePassTextureId, Texture2D.blackTexture);
    }

    private void SetCombineStrengths(float sourceStrength, float destinationStrength)
    {
        Shader.SetGlobalVector(
            combineParamsId,
            new Vector4(sourceStrength, destinationStrength, 0f, 0f));
    }

    private static void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            if (!Shader.IsKeywordEnabled(keyword)) Shader.EnableKeyword(keyword);
        }
        else if (Shader.IsKeywordEnabled(keyword))
        {
            Shader.DisableKeyword(keyword);
        }
    }

    private void SetSourceTexture(Texture texture)
    {
        blurMaterial.mainTexture = texture;
        Shader.SetGlobalVector(bloomTexelSizeId, BloomRenderUtility.GetTexelSize(texture));
    }

    private void SetPreviousTexture(Texture texture)
    {
        Shader.SetGlobalTexture(bloomTexId, texture);
    }

    private void ClearMaterialTextures()
    {
        if (blurMaterial == null) return;
        blurMaterial.mainTexture = null;
        Shader.SetGlobalTexture(bloomTexId, Texture2D.blackTexture);
        Shader.SetGlobalTexture(globalIntensityTexId, Texture2D.blackTexture);
        Shader.SetGlobalVector(bloomTexelSizeId, Vector4.zero);
        Shader.SetGlobalVector(combineParamsId, Vector4.zero);
    }

    private RenderTexture GetUpscaleTexture(int level, RenderTextureFormat format)
    {
        // The up texture for this level has the same dimensions and format as
        // its destination-level down texture.
        var downTexture = bloomfogPasses[level].down;
        var upscaleDescriptor = BloomRenderUtility.CreateDescriptor(
            downTexture.width, downTexture.height, format);
        bloomfogPasses[level].up = BloomRenderUtility.GetTemporary(upscaleDescriptor);
        return bloomfogPasses[level].up;
    }

    private void ReleaseTemporaryPyramid()
    {
        for (var i = 0; i < BloomRenderUtility.MaxPyramidSize; i++)
        {
            if (bloomfogPasses[i].down != null)
                RenderTexture.ReleaseTemporary(bloomfogPasses[i].down);
            if (bloomfogPasses[i].up != null)
                RenderTexture.ReleaseTemporary(bloomfogPasses[i].up);
            bloomfogPasses[i] = default;
        }
    }

    private static RenderTexture CreateOwnedRenderTexture(int width, int height, RenderTextureFormat format, string textureName)
    {
        var texture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear)
        {
            name = textureName,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            useMipMap = false,
            autoGenerateMips = false
        };
        texture.Create();
        return texture;
    }

    private void ReleaseOwnedRenderTexture(ref RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        if (Application.isPlaying) Destroy(texture);
        else DestroyImmediate(texture);
        texture = null;
    }

    private struct Level
    {
        internal RenderTexture down;
        internal RenderTexture up;
    }
}
