using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CameraController editingCameraController;
    [SerializeField] private CameraController playingCameraController;

    public CameraController SelectedCameraController;

    private void Start() => SelectedCameraController = editingCameraController;

    public void SelectCamera(CameraType cameraType)
    {
        SelectedCameraController.Camera.enabled = false;
        SelectedCameraController = cameraType == CameraType.Editing ? editingCameraController : playingCameraController;
        SelectedCameraController.Camera.enabled = true;
    }
}

public enum CameraType
{
    Editing,
    Playing
}
