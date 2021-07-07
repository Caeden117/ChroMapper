using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class KeybindUpdateUIController : MonoBehaviour, CMInput.IWorkflowsActions, CMInput.IEventUIActions
{
    [SerializeField] private PlacementModeController placeMode;
    [SerializeField] private LightingModeController lightMode;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private PrecisionStepDisplayController stepController;
    [SerializeField] private RightButtonPanel rightButtonPanel;

    [SerializeField] private MirrorSelection mirror;

    [SerializeField] private ColorTypeController colorType;
    [SerializeField] private Toggle redToggle;
    [SerializeField] private Toggle blueToggle;
    [SerializeField] private GameObject precisionRotationContainer;

    void Awake()
    {
        UpdatePrecisionRotationGameObjectState();
    }

    public void UpdatePrecisionRotation(string res)
    {
        if (int.TryParse(res, out int value))
        {
            eventPlacement.PrecisionRotationValue = value;
        }
    }

    private void UpdatePrecisionRotationGameObjectState()
    {
        precisionRotationContainer.SetActive(eventPlacement.PlacePrecisionRotation);
    }

    public void OnToggleRightButtonPanel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        rightButtonPanel.TogglePanel();
    }

    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        blueToggle.onValueChanged.Invoke(true);
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
    }

    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        redToggle.onValueChanged.Invoke(true);
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
    }

    public void OnToggleNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (colorType.LeftSelectedEnabled())
        {
            blueToggle.onValueChanged.Invoke(true);
        }
        else
        {
            redToggle.onValueChanged.Invoke(true);
        }
        lightMode.UpdateValue();
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.BOMB);
    }

    public void OnPlaceObstacle(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.WALL);
    }

    public void OnToggleDeleteTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.DELETE);
    }

    public void OnMirror(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        mirror.Mirror();
    }

    public void OnMirrorinTime(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        mirror.MirrorTime();
    }

    public void OnMirrorColoursOnly(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        mirror.Mirror(false);
    }

    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        (BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE) as NotesContainer)
            .UpdateSwingArcVisualizer();
    }

    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
        lightMode.SetMode(LightingModeController.LightingMode.ON);
    }

    public void OnTypeFlash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
        lightMode.SetMode(LightingModeController.LightingMode.FLASH);
    }

    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
        lightMode.SetMode(LightingModeController.LightingMode.OFF);
    }

    public void OnTypeFade(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.NOTE);
        lightMode.SetMode(LightingModeController.LightingMode.FADE);
    }

    public void OnTogglePrecisionRotation(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        eventPlacement.PlacePrecisionRotation = !eventPlacement.PlacePrecisionRotation;
        UpdatePrecisionRotationGameObjectState();
    }

    public void OnSwapCursorInterval(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        stepController.SwapSelectedInterval();
    }
}
