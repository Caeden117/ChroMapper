using ZLinq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
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
    [SerializeField] private EventBoxViewController eventBoxViewController;

    // Keep the paint action on its owning UI controller so selection state controls the visible button.
    [SerializeField] private Button paintPropertiesButton;
    // The scene now uses the current serialized field name, so no legacy-name migration is needed.
    [SerializeField] private Button mirrorUiButton;

    [SerializeField] private ColorTypeController colorType;
    [SerializeField] private Toggle redToggle;
    [SerializeField] private Toggle blueToggle;
    [SerializeField] private Toggle whiteToggle;
    [SerializeField] private GameObject precisionRotationContainer;

    private void Awake()
    {
        UpdatePrecisionRotationGameObjectState();

        // Disable painting until the entire selection can be replaced as GLS event nodes.
        SelectionController.SelectionChangedEvent += UpdatePaintPropertiesButtonState;
        UpdatePaintPropertiesButtonState();

        // Wire up the paint-properties button to call EventBoxViewController's apply-to-selected workflow.
        if (paintPropertiesButton != null)
        {
            paintPropertiesButton.onClick.AddListener(HandlePaintProperties);
            // Show the configured apply-properties shortcut on the button instead of hiding the workflow behind the icon.
            AddHotkeyTooltip(paintPropertiesButton, "Apply properties to selected GLS nodes", "GLS Color Objects", "Apply To Selected");
        }
        else
        {
            Debug.LogError("[KeybindUpdateUIController] paintPropertiesButton is null!");
        }

        // Wire up the mirror UI button to call MirrorSelection's mirror workflow.
        if (mirrorUiButton != null)
        {
            mirrorUiButton.onClick.AddListener(HandleMirrorUi);
            // Show the mirror shortcut on the button so the lane and value inversion workflow is discoverable.
            AddHotkeyTooltip(mirrorUiButton, "Mirror selected objects", "Workflows", "Mirror");
        }
        else
        {
            Debug.LogError("[KeybindUpdateUIController] mirrorUiButton is null!");
        }
    }

    private void OnDestroy()
    {
        // Remove the selection listener when the controller is destroyed to avoid stale UI updates.
        SelectionController.SelectionChangedEvent -= UpdatePaintPropertiesButtonState;
    }

    private void UpdatePaintPropertiesButtonState()
    {
        if (paintPropertiesButton == null)
            return;

        // Require a non-empty, GLS-only selection because this workflow replaces each node in place.
        var selection = SelectionController.SelectedObjects;
        paintPropertiesButton.interactable = selection.Count > 0 && selection.AsValueEnumerable().All(obj => obj is BaseGLSEvent);
    }

    private void HandlePaintProperties()
    {
        // Debug.Log("[KeybindUpdateUIController] HandlePaintProperties called");
        if (eventBoxViewController != null)
        {
            eventBoxViewController.HandleApplyToSelected();
        }
        else
        {
            Debug.LogError("[KeybindUpdateUIController] eventBoxViewController is null!");
        }
        // Deselect the button to prevent it from staying highlighted
        // Unity objects require their overloaded null comparison instead of C# null propagation.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private static void AddHotkeyTooltip(Button button, string text, string hotkeyActionMap, string hotkeyActionName)
    {
        // Avoid stacking tooltip components if this controller is initialized more than once in the editor.
        var tooltip = button.GetComponent<Tooltip>() ?? button.gameObject.AddComponent<Tooltip>();
        tooltip.TooltipOverride = text;
        tooltip.AdvancedTooltip = text;
        tooltip.AppearDelay = 0.25f;
        tooltip.HotkeyActionMap = hotkeyActionMap;
        tooltip.HotkeyActionName = hotkeyActionName;
    }

    private void HandleMirrorUi()
    {
        // Debug.Log("[KeybindUpdateUIController] HandleMirrorUi called");
        if (mirror != null)
        {
            mirror.Mirror();
        }
        else
        {
            Debug.LogError("[KeybindUpdateUIController] mirror is null!");
        }
        // Deselect the button to prevent it from staying highlighted
        // Unity objects require their overloaded null comparison instead of C# null propagation.
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        lightMode.SetMode(LightingModeController.LightingMode.On);
    }

    public void OnTypeFlash(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        lightMode.SetMode(LightingModeController.LightingMode.Flash);
    }

    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        lightMode.SetMode(LightingModeController.LightingMode.Off);
    }

    public void OnTypeFade(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        lightMode.SetMode(LightingModeController.LightingMode.Fade);
    }

    public void OnTypeTransition(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        lightMode.SetMode(LightingModeController.LightingMode.Transition);
    }

    public void OnTogglePrecisionRotation(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
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
        if (eventPlacement.QueuedData.IsWhite) return;

        if (colorType.LeftSelectedEnabled()) blueToggle.onValueChanged.Invoke(true);
        if (colorType.RightSelectedEnabled())
            redToggle.onValueChanged.Invoke(true);
        else
            whiteToggle.onValueChanged.Invoke(true);
        lightMode.UpdateValue();
    }

    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        placeMode.SetMode(PlacementModeController.PlacementMode.Bomb);
        colorType.BombNote(true);
        lightMode.UpdateValue();
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

    // TODO: Remove from Input Actions
    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context) { }

    // TODO: ?idk what these do
    public void UpdatePrecisionRotation(string res)
    {
        // if (int.TryParse(res, out var value)) eventPlacement.PrecisionRotationValue = value;
    }

    private void UpdatePrecisionRotationGameObjectState()
    {
        // precisionRotationContainer.SetActive(eventPlacement.PlacePrecisionRotation);
    }
}
