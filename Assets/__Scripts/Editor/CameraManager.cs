using System;
using UnityEngine;

public class CameraManager : MonoBehaviour, IEditorStateProvider
{
    [SerializeField] private CameraController editingCameraController;
    [SerializeField] private CameraController playingCameraController;
    [SerializeField] private PerCameraShaderSetupController perCameraShaderSetupController;
    [SerializeField] private CameraDepthRenderingController cameraDepthRenderingController;
    [SerializeField] private ScreenDisplacementRenderingController screenDisplacementRenderingController;
    [SerializeField] private BloomfogRenderingController bloomfogRenderingController;
    [SerializeField] private PostProcessRenderingController postProcessRenderingController;

    public CameraController SelectedCameraController;

    public CameraController[] CameraControllers { get; } = new CameraController[2];

    // Keep the editing camera transform with the camera manager that owns it.
    public string StateKey => "editingCamera";

    private void Start()
    {
        SelectedCameraController = editingCameraController;
        perCameraShaderSetupController.AssignToCamera(SelectedCameraController);
        cameraDepthRenderingController.AssignToCamera(SelectedCameraController);
        screenDisplacementRenderingController.AssignToCamera(SelectedCameraController);
        bloomfogRenderingController.AssignToCamera(SelectedCameraController);
        postProcessRenderingController.AssignToCamera(SelectedCameraController);
        CameraControllers[0] = editingCameraController;
        CameraControllers[1] = playingCameraController;
        EditorStateService.Register(this);
    }

    // Release this camera owner when its scene is destroyed.
    private void OnDestroy() => EditorStateService.Unregister(this);

    // Save only the editing camera; the playing camera is a transient preview.
    public void CaptureEditorState(SimpleJSON.JSONObject data)
    {
        var position = editingCameraController.transform.position;
        var rotation = editingCameraController.transform.rotation;
        // SimpleJSON arrays are not enumerable, so add each persisted transform component explicitly.
        var savedPosition = new SimpleJSON.JSONArray();
        savedPosition.Add(position.x);
        savedPosition.Add(position.y);
        savedPosition.Add(position.z);
        data["position"] = savedPosition;
        var savedRotation = new SimpleJSON.JSONArray();
        savedRotation.Add(rotation.x);
        savedRotation.Add(rotation.y);
        savedRotation.Add(rotation.z);
        savedRotation.Add(rotation.w);
        data["rotation"] = savedRotation;
    }

    // Apply the saved transform after CameraManager has assigned its editing camera.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        var position = data["position"].AsArray;
        var rotation = data["rotation"].AsArray;
        if (position == null || rotation == null || position.Count != 3 || rotation.Count != 4)
        {
            return;
        }

        editingCameraController.transform.SetPositionAndRotation(
            new Vector3(position[0].AsFloat, position[1].AsFloat, position[2].AsFloat),
            new Quaternion(rotation[0].AsFloat, rotation[1].AsFloat, rotation[2].AsFloat, rotation[3].AsFloat));
    }

    public void SelectCamera(CameraType cameraType)
    {
        SelectedCameraController.Camera.enabled = false;
        SelectedCameraController = cameraType == CameraType.Editing ? editingCameraController : playingCameraController;
        SelectedCameraController.Camera.enabled = true;
        perCameraShaderSetupController.AssignToCamera(SelectedCameraController);
        cameraDepthRenderingController.AssignToCamera(SelectedCameraController);
        screenDisplacementRenderingController.AssignToCamera(SelectedCameraController);
        bloomfogRenderingController.AssignToCamera(SelectedCameraController);
        postProcessRenderingController.AssignToCamera(SelectedCameraController);
    }
}

public enum CameraType
{
    Editing,
    Playing
}
