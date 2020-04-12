using BN = BeatmapNote;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Brand new Keybinds Controller for more advanced keybind input depending on the situation.
/// </summary>
public class KeybindsController : MonoBehaviour, CMInput.IUtilsActions
{

    [SerializeField] private SelectionController sc;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private InputField laserSpeed;
    [SerializeField] private AutoSaveController autosave;
    [SerializeField] private BeatmapActionContainer actionContainer;
    [SerializeField] private NotesContainer notesContainer;
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;
    [SerializeField] private ObstaclePlacement obstaclePlacement;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CustomEventsContainer customEventsContainer;
    [SerializeField] private UIWorkflowToggle workflowToggle;
    [SerializeField] private MeasureLinesController measureLinesController;
    [SerializeField] private NotePlacementUI notePlacementUI;
    [SerializeField] private EventPlacementUI eventPlacementUI;
    [SerializeField] private BookmarkManager bookmarkManager;
    [SerializeField] private PlatformSoloEventTypeUIController platformSolo;
    [SerializeField] private RefreshMapController refreshMap;
    [SerializeField] private PlatformToggleDisableableObjects toggleDisableableObjects;

    [SerializeField] private Toggle redNoteToggle;
    [SerializeField] private Toggle blueNoteToggle;
    [SerializeField] private Toggle bombToggle;
    [SerializeField] private Toggle wallToggle;
    [SerializeField] private Toggle deleteToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private Toggle blueEventToggle;
    [SerializeField] private Toggle offToggle;
    [SerializeField] private Toggle onToggle;
    [SerializeField] private Toggle fadeToggle;
    [SerializeField] private Toggle flashToggle;

    public bool InvertNoteKeybinds { get => Settings.Instance.InvertNoteControls; }
    public static bool ShiftHeld { get; private set; }
    public static bool CtrlHeld { get; private set; }
    public static bool AltHeld { get; private set; }
    public static bool AnyCriticalKeys { get => ShiftHeld || CtrlHeld || AltHeld; }

    private System.Array KeyCodeEnums = System.Enum.GetValues(typeof(KeyCode));

    void Update()
    {
        //No keybinds when pausing, loading scenes, or inputting text into an Input box.
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading ||
            PersistentUI.Instance.InputBox_IsEnabled || NodeEditorController.IsActive) return;

        GlobalKeybinds(); //These guys are here all day, all night
        if (notePlacement.IsActive) NotesKeybinds(); //Present when placing a note
        if (eventPlacement.IsActive) EventsKeybinds(); //Present when placing an event.
        if (SelectionController.HasSelectedObjects()) SelectionKeybinds(); //Present if objects are selected
    }

    void GlobalKeybinds()
    {
        foreach (KeyCode vKey in KeyCodeEnums) // stahp with the if else spam; wrap everything in this instead
        {
            if (Input.GetKeyDown(vKey))
            {
                if (CtrlHeld) { // ctrl modifiers

                    switch (vKey)
                    {
                        case KeyCode.T:
                            if (Settings.Instance.AdvancedShit)
                            {
                                if (ShiftHeld) customEventsContainer.CreateNewType();
                                else customEventsContainer.SetTrackFilter();
                            }
                            break;
                        case KeyCode.R:
                            if (ShiftHeld) refreshMap.InitiateRefreshConversation();
                            break;
                    }
                }

                if (ShiftHeld && !Input.GetMouseButton(1))
                {
                    switch (vKey)
                    {
                        case KeyCode.S:
                            platformSolo.UpdateSoloEventType();
                            break;
                    }
                }

                switch (vKey) 
                {
                    case KeyCode.F11:
                        if (!Application.isEditor) Screen.fullScreen = !Screen.fullScreen;
                        break;
                    case KeyCode.B:
                        bookmarkManager.AddNewBookmark();
                        break;
                    case KeyCode.L:
                        toggleDisableableObjects.UpdateDisableableObjects();
                        break;
                }
            }
        }
    }

    void SelectionKeybinds()
    {
        if (NodeEditorController.IsActive || PersistentUI.Instance.InputBox_IsEnabled) return;
        if (Input.GetKeyDown(KeyCode.T) && Settings.Instance.AdvancedShit) sc.AssignTrack();
    }

    void NotesKeybinds()
    {
    }

    void EventsKeybinds()
    {
    }

    //These are temporary
    public void OnControlModifier(InputAction.CallbackContext context)
    {
        CtrlHeld = context.performed;
    }

    public void OnAltModifier(InputAction.CallbackContext context)
    {
        AltHeld = context.performed;
    }

    public void OnShiftModifier(InputAction.CallbackContext context)
    {
        ShiftHeld = context.performed;
    }
}
