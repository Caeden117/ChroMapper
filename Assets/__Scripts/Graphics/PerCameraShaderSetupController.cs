using UnityEngine;

// Applies camera-dependent globals immediately before each draw, including nested
// Camera.Render calls. Auxiliary cameras receive defaults; selected and mirror
// cameras receive the configured main-effect state.
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
    public VivifyCameraManager VivifyCameras { get; set; }
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
        // Reset first so an auxiliary camera cannot inherit the previous camera's state.
        Shader.SetGlobalFloat(baseColorBoostId, 1f);
        Shader.SetGlobalFloat(baseColorBoostThresholdId, 0f);
        SetPostBloomKeyword(false);
        UpdateFrustumPlanes(renderingCamera);

        if (renderingCamera != activeCamera
            && renderingCamera.GetComponent<MirrorCamera>() == null
            && VivifyCameras?.IsCaptureCamera(renderingCamera) != true)
            return;

        // Tonemapping applies to authored camera output independently of bloom-fog.
        SetAcesToneMappingKeyword(true);

        if (pyramidBloomController == null || VivifyCameras?.MainEffectEnabled(renderingCamera) == false)
            return;

        pyramidBloomController.ApplyPreRenderState();
        SetPostBloomKeyword(pyramidBloomController.IsReady);
    }

    private void UpdateFrustumPlanes(Camera renderingCamera)
    {
        // Frustum extraction needs the backend projection convention and must account
        // for the render-texture Y orientation used by off-screen reflection cameras.
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
