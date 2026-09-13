using UnityEngine;

// Environment dust/smoke objects surround the player's camera. The original
// component glued the object to every rendering camera through
// Camera.onPreRender, so whichever camera rendered last (the scene view in
// the editor) left the dust at its own position and the map camera never
// saw it centered. React to Camera.onPreRender like the other rendering
// controllers, but only move the dust for ChroMapper's active map camera.
public class FollowCamera : MonoBehaviour
{
    private Quaternion rotationOffset;
    private Vector3 positionOffset;
    private Transform tr;
    private CameraManager cameraManager;

    protected void Awake()
    {
        tr = transform;
        rotationOffset = tr.rotation;
        positionOffset = tr.position;
    }

    protected void OnEnable()
    {
        Camera.onPreRender -= HandleCameraPreRender;
        Camera.onPreRender += HandleCameraPreRender;
    }

    private void OnDisable() => Camera.onPreRender -= HandleCameraPreRender;

    private void HandleCameraPreRender(Camera renderingCamera)
    {
        if (renderingCamera != Camera.main) return;

        var camTransform = renderingCamera.transform;
        tr.SetPositionAndRotation(camTransform.position + positionOffset, camTransform.rotation * rotationOffset);
    }
}
