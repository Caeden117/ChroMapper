using UnityEngine;

// Built-in pipeline equivalent of the game's DepthTextureController. The
// scene owns this controller and assigns only the selected editor camera.
public sealed class CameraDepthRenderingController : MonoBehaviour
{
    private const string depthTextureKeyword = "DEPTH_TEXTURE";

    private static int depthTextureUsers;
    private static bool depthTextureKeywordWasEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        depthTextureUsers = 0;
        depthTextureKeywordWasEnabled = Shader.IsKeywordEnabled(depthTextureKeyword);
    }

    private Camera activeCamera;
    private DepthTextureMode previousDepthTextureMode;
    private bool active;
    private bool configured;

    public void AssignToCamera(CameraController cameraController)
    {
        DetachCamera();
        activeCamera = cameraController == null ? null : cameraController.Camera;
        AttachCamera();
    }

    private void OnEnable()
    {
        active = true;
        AttachCamera();
    }

    private void OnDisable()
    {
        active = false;
        DetachCamera();
    }

    private void OnDestroy() => DetachCamera();

    private void AttachCamera()
    {
        if (!active || activeCamera == null || configured) return;

        previousDepthTextureMode = activeCamera.depthTextureMode;
        activeCamera.depthTextureMode |= DepthTextureMode.Depth;

        if (depthTextureUsers++ == 0)
        {
            depthTextureKeywordWasEnabled = Shader.IsKeywordEnabled(depthTextureKeyword);
            Shader.EnableKeyword(depthTextureKeyword);
        }

        configured = true;
    }

    private void DetachCamera()
    {
        if (!configured) return;

        var depthWasEnabled = (previousDepthTextureMode & DepthTextureMode.Depth) != 0;
        if (activeCamera != null)
        {
            if (depthWasEnabled) activeCamera.depthTextureMode |= DepthTextureMode.Depth;
            else activeCamera.depthTextureMode &= ~DepthTextureMode.Depth;
        }

        configured = false;
        if (depthTextureUsers > 0) depthTextureUsers--;
        if (depthTextureUsers != 0) return;

        if (depthTextureKeywordWasEnabled) Shader.EnableKeyword(depthTextureKeyword);
        else Shader.DisableKeyword(depthTextureKeyword);
    }
}
