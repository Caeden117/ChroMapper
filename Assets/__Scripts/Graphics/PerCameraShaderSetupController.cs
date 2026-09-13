using UnityEngine;

// Built-in pipeline equivalent of SetShaderDefaults, SetFrustumPlanes, and
// MainEffectPreRenderPass. Every camera receives defaults before its draw. The
// selected editor camera then receives the configured main-effect state,
// including the recovered ACES selection for the scene render that follows the
// prepass phases. That selection is unconditional for the authored cameras and
// must not depend on the bloom-fog option.
[DefaultExecutionOrder(-1000)]
public sealed class PerCameraShaderSetupController : MonoBehaviour
{
    public static PerCameraShaderSetupController Instance { get; private set; }

    private const string postBloomKeyword = "POST_BLOOM";
    private const string acesToneMappingKeyword = "ACES_TONE_MAPPING";

    private static readonly int baseColorBoostId = Shader.PropertyToID("_BaseColorBoost");
    private static readonly int baseColorBoostThresholdId = Shader.PropertyToID("_BaseColorBoostThreshold");
    private static readonly int frustumPlanesId = Shader.PropertyToID("_FrustumPlanes");

    [SerializeField] private PyramidBloomController pyramidBloomController;

    private readonly Plane[] planes = new Plane[6];
    private readonly Vector4[] vectorPlanes = new Vector4[6];

    private Camera activeCamera;
    private bool active;
    private bool postBloomKeywordWasEnabled;
    private bool acesToneMappingKeywordWasEnabled;

    public void AssignToCamera(CameraController cameraController) =>
        activeCamera = cameraController == null ? null : cameraController.Camera;

    private void OnEnable()
    {
        if (active) return;
        Instance = this;
        active = true;
        postBloomKeywordWasEnabled = Shader.IsKeywordEnabled(postBloomKeyword);
        acesToneMappingKeywordWasEnabled =
            Shader.IsKeywordEnabled(acesToneMappingKeyword);
        Camera.onPreRender += OnCameraPreRender;
    }

    private void OnDisable()
    {
        if (!active) return;
        if (Instance == this) Instance = null;
        active = false;
        Camera.onPreRender -= OnCameraPreRender;

        Shader.SetGlobalFloat(baseColorBoostId, 1f);
        Shader.SetGlobalFloat(baseColorBoostThresholdId, 0f);
        Shader.SetGlobalVectorArray(frustumPlanesId, new Vector4[6]);
        SetPostBloomKeyword(postBloomKeywordWasEnabled);
        SetAcesToneMappingKeyword(acesToneMappingKeywordWasEnabled);
    }

    private void OnCameraPreRender(Camera renderingCamera)
    {
        ApplyCameraState(renderingCamera);
    }

    public void ApplyCameraState(Camera renderingCamera)
    {
        Shader.SetGlobalFloat(baseColorBoostId, 1f);
        Shader.SetGlobalFloat(baseColorBoostThresholdId, 0f);
        SetPostBloomKeyword(false);
        UpdateFrustumPlanes(renderingCamera);

        if (renderingCamera != activeCamera
            && renderingCamera.GetComponent<MirrorCamera>() == null)
            return;

        // The game's prepass phases select ACES for the scene render that
        // follows them. Apply it for the authored cameras regardless of any
        // bloom-fog option so tonemapping never depends on that toggle.
        SetAcesToneMappingKeyword(true);

        if (pyramidBloomController == null)
            return;

        pyramidBloomController.ApplyPreRenderState();
        SetPostBloomKeyword(pyramidBloomController.IsReady);
    }

    private void UpdateFrustumPlanes(Camera renderingCamera)
    {
        var projectionMatrix = GL.GetGPUProjectionMatrix(
            renderingCamera.projectionMatrix,
            renderingCamera.targetTexture != null);
        GeometryUtility.CalculateFrustumPlanes(
            projectionMatrix * renderingCamera.worldToCameraMatrix,
            planes);

        for (var i = 0; i < planes.Length; i++)
        {
            var plane = planes[i];
            vectorPlanes[i] = new Vector4(
                plane.normal.x,
                plane.normal.y,
                plane.normal.z,
                plane.distance);
        }

        Shader.SetGlobalVectorArray(frustumPlanesId, vectorPlanes);
    }

    private static void SetPostBloomKeyword(bool enabled)
    {
        if (enabled)
        {
            if (!Shader.IsKeywordEnabled(postBloomKeyword)) Shader.EnableKeyword(postBloomKeyword);
        }
        else if (Shader.IsKeywordEnabled(postBloomKeyword))
        {
            Shader.DisableKeyword(postBloomKeyword);
        }
    }

    private static void SetAcesToneMappingKeyword(bool enabled)
    {
        if (enabled)
        {
            if (!Shader.IsKeywordEnabled(acesToneMappingKeyword))
                Shader.EnableKeyword(acesToneMappingKeyword);
        }
        else if (Shader.IsKeywordEnabled(acesToneMappingKeyword))
        {
            Shader.DisableKeyword(acesToneMappingKeyword);
        }
    }
}
