using UnityEngine;
using UnityEngine.InputSystem;

public class LightshowController : MonoBehaviour, CMInput.ILightshowActions
{
    [SerializeField] private GameObject[] ThingsToToggle;
    [SerializeField] private CameraController cameraController;

    private bool showObjects = true;
    private bool previouslyLocked;

    public void UpdateLightshow(bool enable)
    {
        showObjects = enable;
        foreach (GameObject obj in ThingsToToggle) obj.SetActive(enable);
    }

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
}
