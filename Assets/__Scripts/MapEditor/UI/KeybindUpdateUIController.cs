using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    private void Awake() => UpdatePrecisionRotationGameObjectState();

    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
        lightMode.SetMode(LightingModeController.LightingMode.ON);
    }

    public void OnTypeFlash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
        lightMode.SetMode(LightingModeController.LightingMode.Flash);
    }

    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
        lightMode.SetMode(LightingModeController.LightingMode.Off);
    }

    public void OnTypeFade(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
        lightMode.SetMode(LightingModeController.LightingMode.Fade);
    }

    public void OnTypeTransition(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        lightMode.SetMode(LightingModeController.LightingMode.Transition);
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

    public void OnToggleRightButtonPanel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        rightButtonPanel.TogglePanel();
    }

    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        blueToggle.onValueChanged.Invoke(true);
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
    }

    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        redToggle.onValueChanged.Invoke(true);
        placeMode.SetMode(PlacementModeController.PlacementMode.Note);
    }

    public void OnToggleNoteorEvent(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (colorType.LeftSelectedEnabled())
            blueToggle.onValueChanged.Invoke(true);
        else
            redToggle.onValueChanged.Invoke(true);
        lightMode.UpdateValue();
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Bomb);
        if (Settings.Instance.Load_MapV3)
        {
            colorType.BombNote(true);
            lightMode.UpdateValue();
        }
    }

    public void OnPlaceObstacle(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Wall);
    }

    public void OnToggleDeleteTool(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Delete);
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
        (BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note) as NotesContainer)
            .UpdateSwingArcVisualizer();
    }

    public void UpdatePrecisionRotation(string res)
    {
        if (int.TryParse(res, out var value)) eventPlacement.PrecisionRotationValue = value;
    }

    private void UpdatePrecisionRotationGameObjectState() =>
        precisionRotationContainer.SetActive(eventPlacement.PlacePrecisionRotation);
}
