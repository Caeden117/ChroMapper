using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class LightshowController : MonoBehaviour, CMInput.ILightshowActions
{
    [FormerlySerializedAs("ThingsToToggle")] [SerializeField] private GameObject[] thingsToToggle;
    [SerializeField] private CameraController cameraController;
    private bool previouslyLocked;

    private bool showObjects = true;

    public void OnToggleLightshowMode(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Order matters here so that we don't briefly disable the camera
        // It has to be removed from the objects before they're disabled and added back after they're enabled again
        if (showObjects)
        {
            previouslyLocked = cameraController.LockedOntoNoteGrid;
            cameraController.LockedOntoNoteGrid = false;
            UpdateLightshow(false);
        }
        else
        {
            UpdateLightshow(true);
            cameraController.LockedOntoNoteGrid = previouslyLocked;
        }
    }

    public void UpdateLightshow(bool enable)
    {
        showObjects = enable;
        foreach (var obj in thingsToToggle) obj.SetActive(enable);
    }
}
