using System.Collections.Generic;
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
    private static readonly int customFogOffsetId = Shader.PropertyToID("_CustomFogOffset");
    private static readonly int customFogStartYId = Shader.PropertyToID("_CustomFogHeightFogStartY");
    private static readonly int customFogHeightId = Shader.PropertyToID("_CustomFogHeightFogHeight");
    private static readonly int customFogAttenuationId = Shader.PropertyToID("_CustomFogAttenuation");

    [SerializeField] private Shader blurShader;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private BloomfogRendererSO bloomfogRenderer;
    [SerializeField] private PyramidBloomProfileSO bloomProfile;
    [SerializeField] private MeshFilter skyboxQuadMeshFilter;
    [SerializeField] private MeshRenderer skyboxQuadRenderer;
    [Space]
    // Used only when no profile is assigned; SetProfileDefaults copies profile values at startup.
    [SerializeField] private float bloomIntensity = 0.75f;
    [SerializeField] private float bloomRadius = 10f;
    [SerializeField] private float pyramidWeightsParam = 1f;
    [SerializeField] private float downIntensityOffset = 1f;
    [SerializeField] private float firstUpscaleBrightness = 1.2f;
    [SerializeField] private float finalUpscaleBrightness = 0.25f;

    private Camera activeCamera;
    public VivifyCameraManager VivifyCameras { get; set; }
    /// <summary>Lets an authored map skybox replace the generated fog skybox during camera draws.</summary>
    public bool MapSkyboxOverride { get; set; }
    private readonly Dictionary<Camera, CameraRenderState> cameraRenderStates = new();
    private Material blurMaterial;
    private CameraClearFlags previousClearFlags;
    private int previousSkyboxLayerMask;
    private bool cameraConfigured;
    private Material previousRenderSettingsSkybox;
    private bool renderSettingsSkyboxSuppressed;
    private Mesh skyboxQuadMesh;

    private RenderTexture bloomfogRaw = null;
    private RenderTexture bloomfogTex = null;
    private readonly Level[] bloomfogPasses =
        new Level[BloomRenderUtility.MaxPyramidSize];
    private bool active;
    private bool bloomFogKeywordWasEnabled;
    private float bloomFogAutoExposureLimit = 1000f;
    private bool bloomFogLegacyAutoExposure;

    public bool CanRenderReflections => active && blurMaterial != null;

    /// <summary>Captures every shader global that a reflection bloom-fog render can replace.</summary>
    /// <remarks>The returned textures are borrowed references; this controller does not transfer ownership.</remarks>
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

    /// <summary>Restores a shader-global snapshot captured by <see cref="CaptureGlobalState"/>.</summary>
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

    /// <summary>Renders bloom fog into caller-owned reflection targets and publishes the result globally.</summary>
    /// <remarks>The caller retains ownership of both render textures and must restore global state when needed.</remarks>
    public void RenderReflection(
        Matrix4x4 viewMatrix,
        Matrix4x4 projectionMatrix,
        RenderTexture rawTexture,
        RenderTexture finalTexture)
    {
        if (!CanRenderReflections || rawTexture == null || finalTexture == null) return;

        SetKeyword(bloomFogKeyword, true);
        SetKeyword(acesToneMappingKeyword, true);
        // Prepass geometry must not feed the previously published fog back into its emission.
        Shader.SetGlobalTexture(bloomPrePassTextureId, Texture2D.blackTexture);
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
        Activate();
    }

    private void OnEnable()
    {
        Instance = this;
        if (blurMaterial != null) Activate();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
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
        Camera.onPreCull += OnCameraPreCull;
        Camera.onPreRender += OnCameraPreRender;
        Camera.onPostRender += OnCameraPostRender;
        if (context != null)
        {
            context.OnEnvironmentLoaded += HandleEnvironmentLoaded;
            context.OnEnvironmentUnloaded += HandleEnvironmentUnloaded;
            context.OnBloomFogParamsChanged += HandleBloomFogParamsChanged;
        }
        AttachCamera();
        SuppressRenderSettingsSkybox();
        if (skyboxQuadRenderer != null) skyboxQuadRenderer.enabled = true;
    }

    private void Deactivate()
    {
        foreach (var entry in cameraRenderStates) RestoreCameraState(entry.Key, entry.Value);
        cameraRenderStates.Clear();
        var wasActive = active;
        if (active)
        {
            active = false;
            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPreRender -= OnCameraPreRender;
            Camera.onPostRender -= OnCameraPostRender;
            if (context != null)
            {
                context.OnEnvironmentLoaded -= HandleEnvironmentLoaded;
                context.OnEnvironmentUnloaded -= HandleEnvironmentUnloaded;
                context.OnBloomFogParamsChanged -= HandleBloomFogParamsChanged;
            }
        }
        if (wasActive) SetKeyword(bloomFogKeyword, bloomFogKeywordWasEnabled);
        // Per-camera setup owns ACES; restoring the activation-time value here could be stale.
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

    private void OnCameraPreCull(Camera renderingCamera)
    {
        var capture = VivifyCameras?.IsCaptureCamera(renderingCamera) == true;
        if (renderingCamera != activeCamera && !capture) return;
        if (bloomfogRaw == null || bloomfogTex == null ||
            !bloomfogRaw.IsCreated() || !bloomfogTex.IsCreated()) return;

        var enabledForCamera = VivifyCameras?.BloomPrePassEnabled(renderingCamera) != false;
        var restoreGlobals = capture || !enabledForCamera;
        var changeClearFlags = MapSkyboxOverride && VivifyCameras?.HasClearFlagsOverride(renderingCamera) != true;
        cameraRenderStates[renderingCamera] = new CameraRenderState(
            restoreGlobals, restoreGlobals ? CaptureGlobalState() : default,
            changeClearFlags, renderingCamera.clearFlags,
            skyboxQuadRenderer != null && skyboxQuadRenderer.enabled);
        if (changeClearFlags) renderingCamera.clearFlags = CameraClearFlags.Skybox;
        if (skyboxQuadRenderer != null)
            skyboxQuadRenderer.enabled = enabledForCamera && !MapSkyboxOverride;
    }

    // Publish the completed fog texture before the camera consumes it.
    private void OnCameraPreRender(Camera renderingCamera)
    {
        if (!cameraRenderStates.ContainsKey(renderingCamera)) return;
        var enabledForCamera = VivifyCameras?.BloomPrePassEnabled(renderingCamera) != false;

        if (!enabledForCamera)
        {
            SetKeyword(bloomFogKeyword, false);
            Shader.SetGlobalTexture(bloomPrePassTextureId, Texture2D.blackTexture);
            Shader.SetGlobalVector(customFogTextureToScreenRatioId, Vector4.zero);
            return;
        }

        // This shader consumes clip-space quad vertices, so a skybox-cube draw would duplicate the fog.
        SuppressRenderSettingsSkybox();
        SetKeyword(bloomFogKeyword, true);
        // Both prepass phases and the following scene render use ACES.
        SetKeyword(acesToneMappingKeyword, true);

        // Match the native prepass: generate fog without sampling an earlier fog result.
        Shader.SetGlobalTexture(bloomPrePassTextureId, Texture2D.blackTexture);
        bloomfogRenderer.RenderToTexture(renderingCamera, bloomfogRaw, out _, VivifyCameras);
        RenderBloomTexture(bloomfogRaw, bloomfogTex, upscalePass);
        Shader.SetGlobalTexture(bloomPrePassTextureId, bloomfogTex);
        bloomfogRenderer.PublishGlobals();
    }

    private void RenderBloomTexture(
        RenderTexture rawTexture,
        RenderTexture finalTexture,
        int intermediateUpscalePass)
    {
        // The pyramid starts at half resolution; raw and final targets remain full size.
        var descriptor = BloomRenderUtility.CreateDescriptor(
            Mathf.Max(finalTexture.width / 2, 1),
            Mathf.Max(finalTexture.height / 2, 1),
            finalTexture.format);

        // Radius changes both the pyramid depth and the fractional sampling scale.
        BloomRenderUtility.CalculatePyramidParameters(
            descriptor.width,
            descriptor.height,
            bloomRadius,
            out var realBloomfogPasses,
            out var blurRadius);

        try
        {
            // Main bloom reuses these globals later, so restore fog-specific values each frame.
            Shader.SetGlobalVector(
                bloomParamsId,
                new Vector4(
                    bloomFogAutoExposureLimit,
                    blurRadius,
                    0f,
                    bloomFogLegacyAutoExposure ? 1f : 0f));

            // Initialize merge weights for the one-level route; multi-level merges replace them.
            SetCombineStrengths(bloomIntensity, 1f);
            Shader.SetGlobalFloat(sampleScaleId, blurRadius);

            // Every level, including level zero, runs the downsample pass.
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

            // The final downsample is the auto-exposure input and must survive the final pass.
            Shader.SetGlobalTexture(globalIntensityTexId, downscaleSrc);

            var upscaleSrc = bloomfogPasses[realBloomfogPasses - 1].down;
            if (realBloomfogPasses == 1)
            {
                // A one-level pyramid has no destination level to merge.
                SetCombineStrengths(1f, 0f);
                SetPreviousTexture(Texture2D.blackTexture);
                SetSourceTexture(upscaleSrc);
                Graphics.Blit(upscaleSrc, finalTexture, blurMaterial, finalUpscalePass);
            }
            else
            {
                for (var i = realBloomfogPasses - 2; i >= 0; i--)
                {
                    // x weights this level; y weights the accumulated lower-resolution source.
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

            // After-blur objects are composited only after the final ACES bloom pass.
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
        RestoreCameraState(renderingCamera);
        if (renderingCamera == activeCamera && !bloomFogKeywordWasEnabled)
            SetKeyword(bloomFogKeyword, false);
    }

    /// <summary>Restores globals and temporary camera changes after a render, including a failed capture.</summary>
    public void RestoreCameraState(Camera camera)
    {
        if (ReferenceEquals(camera, null) || !cameraRenderStates.Remove(camera, out var state)) return;
        RestoreCameraState(camera, state);
    }

    private void RestoreCameraState(Camera camera, CameraRenderState state)
    {
        if (state.RestoreGlobals) RestoreGlobalState(state.Globals);
        if (state.RestoreClearFlags && camera != null) camera.clearFlags = state.ClearFlags;
        if (skyboxQuadRenderer != null) skyboxQuadRenderer.enabled = state.SkyboxQuadEnabled;
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        if (descriptor == null) return;
        if (descriptor.BloomFogParams == null)
        {
            HandleEnvironmentUnloaded();
            return;
        }

        HandleBloomFogParamsChanged(descriptor.BloomFogParams);
    }

    private void HandleBloomFogParamsChanged(BloomFogParams parameters) =>
        UpdateBloomFogParams(parameters.AutoExposureLimit, parameters.Offset, parameters.Height,
            parameters.StartY, parameters.Attenuation, parameters.LegacyAutoExposure);

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
        ReleaseSkyboxQuad();
        ClearRenderTextures();
        if (blurMaterial != null)
        {
            if (Application.isPlaying) Destroy(blurMaterial);
            else DestroyImmediate(blurMaterial);
            blurMaterial = null;
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
        if (MapSkyboxOverride) return;
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

    /// <summary>Restores animated fog globals for the active owning context after other events write them.</summary>
    /// <remarks>Only changed native values are written. Parameters are borrowed from the context descriptor.</remarks>
    public void ReconcileBloomFogParams(BeatmapRuntimeContext owner, BloomFogParams parameters)
    {
        if (!active || context != owner || parameters == null) return;
        HandleBloomFogParamsChanged(parameters);
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
        if (Shader.GetGlobalFloat(customFogOffsetId) != offset)
            Shader.SetGlobalFloat(customFogOffsetId, offset);
        if (Shader.GetGlobalFloat(customFogStartYId) != startY)
            Shader.SetGlobalFloat(customFogStartYId, startY);
        if (Shader.GetGlobalFloat(customFogHeightId) != height)
            Shader.SetGlobalFloat(customFogHeightId, height);
        if (Shader.GetGlobalFloat(customFogAttenuationId) != attenuation)
            Shader.SetGlobalFloat(customFogAttenuationId, attenuation);
    }

    private void ClearRenderTextures()
    {
        ReleaseOwnedRenderTexture(ref bloomfogRaw);
        ReleaseOwnedRenderTexture(ref bloomfogTex);
    }

    private void RegenerateRenderTexture()
    {
        Shader.SetGlobalTexture(bloomPrePassTextureId, null);
        ClearRenderTextures();

        var width = bloomFogResolution;
        var height = bloomFogResolution;
        var format = BloomRenderUtility.GetBloomTextureFormat();

        // These persistent targets are owned by the controller, unlike temporary pyramid levels.
        try
        {
            bloomfogTex = CreateOwnedRenderTexture(width, height, format, "Bloomfog Final Texture");
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
        // A failed pass can leave any subset allocated, so scan the complete fixed-size pyramid.
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

    private readonly struct CameraRenderState
    {
        public readonly bool RestoreGlobals;
        public readonly GlobalState Globals;
        public readonly bool RestoreClearFlags;
        public readonly CameraClearFlags ClearFlags;
        public readonly bool SkyboxQuadEnabled;

        public CameraRenderState(bool restoreGlobals, GlobalState globals,
            bool restoreClearFlags, CameraClearFlags clearFlags, bool skyboxQuadEnabled)
        {
            RestoreGlobals = restoreGlobals;
            Globals = globals;
            RestoreClearFlags = restoreClearFlags;
            ClearFlags = clearFlags;
            SkyboxQuadEnabled = skyboxQuadEnabled;
        }
    }

    private struct Level
    {
        internal RenderTexture down;
        internal RenderTexture up;
    }
}
