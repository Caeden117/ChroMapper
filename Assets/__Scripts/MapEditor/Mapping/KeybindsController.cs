using BN = BeatmapNote;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Brand new Keybinds Controller for more advanced keybind input depending on the situation.
/// </summary>
public class KeybindsController : MonoBehaviour {

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

    void Update()
    {
        //No keybinds when pausing, loading scenes, or inputting text into an Input box.
        if (PauseManager.IsPaused || SceneTransitionManager.IsLoading ||
            PersistentUI.Instance.InputBox_IsEnabled) return;
        ShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        AltHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        CtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); //Can't forget our Apple friends.

        GlobalKeybinds(); //These guys are here all day, all night
        if (notePlacement.IsActive) NotesKeybinds(); //Present when placing a note
        if (eventPlacement.IsActive) EventsKeybinds(); //Present when placing an event.
        if (SelectionController.HasSelectedObjects()) SelectionKeybinds(); //Present if objects are selected
    }

    void GlobalKeybinds()
    {
        foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode))) // stahp with the if else spam; wrap everything in this instead
        {
            if (Input.GetKeyDown(vKey))
            {
                if (CtrlHeld) { // ctrl modifiers
                    if ((int)vKey >= 48 && (int)vKey <= 57)
                    { // laserspeed text done using this instead
                        laserSpeed.text = (((int)vKey + 2) % 50).ToString();
                        return;
                    }
                    if ((int)vKey >= 256 && (int)vKey <= 265) {
                        laserSpeed.text = ((int)vKey + 4 - 260).ToString();
                        return;
                    }

                    switch (vKey)
                    {
                        case KeyCode.T:
                            if (Settings.Instance.AdvancedShit)
                            {
                                if (ShiftHeld) customEventsContainer.CreateNewType();
                                else customEventsContainer.SetTrackFilter();
                            }
                            break;
                        case KeyCode.S:
                            if (!Input.GetMouseButton(1)) autosave.Save();
                            break;
                        case KeyCode.Z:
                            if (!ShiftHeld) actionContainer.Undo();
                            else actionContainer.Redo();
                            break;
                        case KeyCode.Y:
                            if (!ShiftHeld) actionContainer.Redo();
                            else actionContainer.Undo();
                            break;
                        case KeyCode.V:
                            if (SelectionController.HasCopiedObjects() && !NodeEditorController.IsActive) sc.Paste();
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
                    case KeyCode.Tab:
                        if (!NodeEditorController.IsActive) workflowToggle.UpdateWorkflowGroup();
                        break;
                    case KeyCode.V:
                        if (!AnyCriticalKeys && !NodeEditorController.IsActive) notesContainer.UpdateSwingArcVisualizer();
                        break;
                    case KeyCode.Alpha1: // ui keybinds ( for top bar )
                        if (AnyCriticalKeys || Input.GetMouseButton(1) || eventPlacementUI.IsTypingRotation) continue;
                        redEventToggle.isOn = true;
                        redNoteToggle.isOn = true;
                        eventPlacement.IsActive = true;
                        break;
                    case KeyCode.Alpha2:
                        if (AnyCriticalKeys || Input.GetMouseButton(1) || eventPlacementUI.IsTypingRotation) continue;
                        blueEventToggle.isOn = true;
                        blueNoteToggle.isOn = true;
                        eventPlacement.IsActive = true;
                        break;
                    case KeyCode.Alpha3:
                        if (AnyCriticalKeys || Input.GetMouseButton(1) || eventPlacementUI.IsTypingRotation) continue;
                        bombToggle.isOn = true;
                        eventPlacement.IsActive = true;
                        break;
                    case KeyCode.Alpha4:
                        if (AnyCriticalKeys || Input.GetMouseButton(1) || eventPlacementUI.IsTypingRotation) continue;
                        wallToggle.isOn = true;
                        eventPlacement.IsActive = true;
                        break;
                    case KeyCode.Alpha5:
                        if (eventPlacementUI.IsTypingRotation) continue;
                        deleteToggle.isOn = !deleteToggle.isOn;
                        break; // end of ui keybinds
                    case KeyCode.W:
                    case KeyCode.A:
                    case KeyCode.S:
                    case KeyCode.D:
                    case KeyCode.F:
                        wasdCase(vKey);
                        break;
                    case KeyCode.R:
                        if (!AnyCriticalKeys) eventPlacementUI.UpdatePrecisionRotationValue();
                        break;
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
        if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) sc.Delete();
        if (CtrlHeld)
        {
            if (Input.GetKeyDown(KeyCode.A)) SelectionController.DeselectAll();
            if (Input.GetKeyDown(KeyCode.C)) sc.Copy();
            if (Input.GetKeyDown(KeyCode.X)) sc.Copy(true);
        }
        if (ShiftHeld)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) sc.MoveSelection(1f / atsc.gridMeasureSnapping);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) sc.MoveSelection(-1f / atsc.gridMeasureSnapping);
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) sc.ShiftSelection(-1, 0);
            if (Input.GetKeyDown(KeyCode.RightArrow)) sc.ShiftSelection(1, 0);
            if (Input.GetKeyDown(KeyCode.UpArrow)) sc.ShiftSelection(0, 1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) sc.ShiftSelection(0, -1);
        }
    }

    void NotesKeybinds()
    {
        /*if (Input.GetKeyDown(KeyCode.Alpha1))
            notePlacement.UpdateType(BN.NOTE_TYPE_A);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            notePlacement.UpdateType(BN.NOTE_TYPE_B);*/

        if (!notePlacement.IsValid) return;

        if (Input.GetKeyDown(KeyCode.Keypad8) || Input.GetKeyDown(KeyCode.W))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_UP : BN.NOTE_CUT_DIRECTION_DOWN);
        else if (Input.GetKeyDown(KeyCode.Keypad9) || (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D)))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_UP_RIGHT : BN.NOTE_CUT_DIRECTION_DOWN_LEFT);
        else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.D))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_RIGHT : BN.NOTE_CUT_DIRECTION_LEFT);
        else if (Input.GetKeyDown(KeyCode.Keypad3) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.D)))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_DOWN_RIGHT : BN.NOTE_CUT_DIRECTION_UP_LEFT);
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.S))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_DOWN : BN.NOTE_CUT_DIRECTION_UP);
        else if (Input.GetKeyDown(KeyCode.Keypad1) || (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A)))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_DOWN_LEFT : BN.NOTE_CUT_DIRECTION_UP_RIGHT);
        else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.A))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_LEFT : BN.NOTE_CUT_DIRECTION_RIGHT);
        else if (Input.GetKeyDown(KeyCode.Keypad7) || (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A)))
            notePlacement.UpdateCut(InvertNoteKeybinds ? BN.NOTE_CUT_DIRECTION_UP_LEFT : BN.NOTE_CUT_DIRECTION_DOWN_RIGHT);
        else if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.F))
            notePlacement.UpdateCut(BN.NOTE_CUT_DIRECTION_ANY);
        /*else if ((!CtrlHeld && Input.GetKeyDown(KeyCode.Keypad0)) || Input.GetKeyDown(KeyCode.E))
        {
            notePlacement.ChangeChromaToggle(true);
            notePlacement.UpdateChromaValue(BeatmapChromaNote.DEFLECT);
        }
        else if ((!CtrlHeld && Input.GetKeyDown(KeyCode.KeypadPeriod)) || Input.GetKeyDown(KeyCode.Q))
        {
            notePlacement.ChangeChromaToggle(true);
            notePlacement.UpdateChromaValue(BeatmapChromaNote.BIDIRECTIONAL);
        }*/ //ChromaToggle is currently not a thing atm so disabling these
        if (Input.GetKeyDown(KeyCode.R)) notePlacement.ChangeChromaToggle(false);
    }

    void EventsKeybinds()
    {
        if (!eventPlacement.IsValid) return;
        /*if (Input.GetKeyDown(KeyCode.Alpha1)) eventPlacement.SwapColors(true);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) eventPlacement.SwapColors(false);*/

        if (Input.GetKeyDown(KeyCode.P))
        {
            eventPlacement.objectContainerCollection.RingPropagationEditing =
                !eventPlacement.objectContainerCollection.RingPropagationEditing;
        }

        if (eventPlacement.queuedData.IsRotationEvent)
        {
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode))) //big brain to change rotation values
            {
                if (Input.GetKeyDown(vKey))
                {
                    int[] numPadToValues = new int[] { 4, 5, 6, 7 };
                    int[] shiftNumPadToValues = new int[] { 3, 2, 1, 0 };
                    if ((int)vKey >= (int)KeyCode.Alpha1 && (int)vKey <= (int)KeyCode.Alpha4)
                    {
                        int index = (int)vKey - (int)KeyCode.Alpha1;
                        eventPlacement.UpdateValue(ShiftHeld ? shiftNumPadToValues[index] : numPadToValues[index]);
                    }
                }
            }
        }

        /*if (Input.GetKeyDown(KeyCode.W))
            eventPlacement.UpdateValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
        else if (Input.GetKeyDown(KeyCode.D))
            eventPlacement.UpdateValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
        else if (Input.GetKeyDown(KeyCode.S))
            eventPlacement.UpdateValue(MapEvent.LIGHT_VALUE_OFF);
        else if (Input.GetKeyDown(KeyCode.A))
            eventPlacement.UpdateValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
        else if (Input.GetKeyDown(KeyCode.F)) eventPlacement.SwapColors(!IsRedNote());*/
    }

    public void wasdCase(KeyCode vKey = KeyCode.Escape)
    {
        if (AnyCriticalKeys || Input.GetMouseButton(1)) return;
        switch (vKey)
        {
            case KeyCode.W:
                onToggle.isOn = true;
                break;
            case KeyCode.A:
                flashToggle.isOn = true;
                break;
            case KeyCode.S:
                offToggle.isOn = true;
                break;
            case KeyCode.D:
                fadeToggle.isOn = true;
                break;
        }
        if (notePlacement.queuedData._type == BeatmapNote.NOTE_TYPE_B) blueNoteToggle.isOn = true;
        else redNoteToggle.isOn = true;
    }

    private bool IsRedNote()
    {
        switch (eventPlacement.queuedData._value)
        {
            case MapEvent.LIGHT_VALUE_OFF: return eventPlacement.PlaceRedNote;
            case MapEvent.LIGHT_VALUE_RED_ON: return true;
            case MapEvent.LIGHT_VALUE_RED_FLASH: return true;
            case MapEvent.LIGHT_VALUE_RED_FADE: return true;
            default: return false;
        }
    }
}
