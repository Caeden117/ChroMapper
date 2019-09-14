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
    [SerializeField] private NotePlacement notePlacement;
    [SerializeField] private BombPlacement bombPlacement;

    public bool InvertNoteKeybinds = false;

    public static bool ShiftHeld = false;
    public static bool CtrlHeld = false;
    public static bool AltHeld = false;

	void Update()
    {
        if (PauseManager.IsPaused) return;
        ShiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        AltHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        CtrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand); //Can't forget our Apple friends.

        GlobalKeybinds(); //These guys are here all day, all night
        if (notePlacement.IsActive) NotesKeybinds(); //Present when placing a note
        if (bombPlacement.IsActive) BombsKeybinds(); //Present when placing a bomb
        if (EventPreview.IsActive) EventsKeybinds(); //Present when placing an event.
        if (SelectionController.HasSelectedObjects()) SelectionKeybinds(); //Present if objects are selected
    }

    void GlobalKeybinds()
    {
        if (CtrlHeld)
        {
            if (Input.GetKeyDown(KeyCode.S)) autosave.Save();
            if (Input.GetKeyDown(KeyCode.Alpha1)) laserSpeed.text = "1";
            else if (Input.GetKeyDown(KeyCode.Alpha2)) laserSpeed.text = "2";
            else if (Input.GetKeyDown(KeyCode.Alpha3)) laserSpeed.text = "3";
            else if (Input.GetKeyDown(KeyCode.Alpha4)) laserSpeed.text = "4";
            else if (Input.GetKeyDown(KeyCode.Alpha5)) laserSpeed.text = "5";
            else if (Input.GetKeyDown(KeyCode.Alpha6)) laserSpeed.text = "6";
            else if (Input.GetKeyDown(KeyCode.Alpha7)) laserSpeed.text = "7";
            else if (Input.GetKeyDown(KeyCode.Alpha8)) laserSpeed.text = "8";
            else if (Input.GetKeyDown(KeyCode.Alpha9)) laserSpeed.text = "9";
            else if (Input.GetKeyDown(KeyCode.Alpha0)) laserSpeed.text = "0";

            if (Input.GetKeyDown(KeyCode.Z) || (ShiftHeld && Input.GetKeyDown(KeyCode.Y))) actionContainer.Undo();
            else if (Input.GetKeyDown(KeyCode.Y) || (ShiftHeld && Input.GetKeyDown(KeyCode.Z))) actionContainer.Redo();


            if (Input.GetKeyDown(KeyCode.V) && SelectionController.HasCopiedObjects()) sc.Paste();
        }
        if (Input.GetKeyDown(KeyCode.F11) && !Application.isEditor) Screen.fullScreen = !Screen.fullScreen;
        //if (Input.GetKeyDown(KeyCode.Z) || (ShiftHeld && Input.GetKeyDown(KeyCode.Y))) undoRedo.Undo();
        //if (Input.GetKeyDown(KeyCode.Y) || (ShiftHeld && Input.GetKeyDown(KeyCode.Z))) undoRedo.Redo();
    }

    void SelectionKeybinds()
    {
        if (NodeEditorController.IsActive) return;
        if (Input.GetKeyDown(KeyCode.Delete) || (ShiftHeld && Input.GetMouseButtonDown(2))) sc.Delete();
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
            notePlacement.UpdateType(BN.NOTE_TYPE_A);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            notePlacement.UpdateType(BN.NOTE_TYPE_B);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            notePlacement.IsActive = false;
            bombPlacement.IsActive = true;
        }

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
        else if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.E))
        {
            notePlacement.ChangeChromaToggle(true);
            notePlacement.UpdateChromaValue(BeatmapChromaNote.DEFLECT);
        }
        else if (Input.GetKeyDown(KeyCode.KeypadPeriod) || Input.GetKeyDown(KeyCode.Q))
        {
            notePlacement.ChangeChromaToggle(true);
            notePlacement.UpdateChromaValue(BeatmapChromaNote.BIDIRECTIONAL);
        }
        if (Input.GetKeyDown(KeyCode.R)) notePlacement.ChangeChromaToggle(false);
    }

    void BombsKeybinds()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKey(KeyCode.Alpha2))
        {
            bombPlacement.IsActive = false;
            notePlacement.IsActive = true;
            notePlacement.UpdateType(Input.GetKeyDown(KeyCode.Alpha1) ? BN.NOTE_TYPE_A : BN.NOTE_TYPE_B);
        }
    }

    void EventsKeybinds()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_ON || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_ON)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_ON);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FADE || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FADE)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_FADE);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FLASH || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FLASH)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_FLASH);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_ON || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_ON)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_ON);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FADE || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FADE)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_FADE);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FLASH || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FLASH)
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_FLASH);
        }

        if (Input.GetKeyDown(KeyCode.W))
            EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
        else if (Input.GetKeyDown(KeyCode.D))
            EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
        else if (Input.GetKeyDown(KeyCode.S))
            EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_OFF);
        else if (Input.GetKeyDown(KeyCode.A))
            EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
        else if (Input.GetKeyDown(KeyCode.F)) {
            if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_ON || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_ON)
                EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_BLUE_ON : MapEvent.LIGHT_VALUE_RED_ON);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FADE || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FADE)
                EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_BLUE_FADE : MapEvent.LIGHT_VALUE_RED_FADE);
            else if (EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_RED_FLASH || EventPreview.QueuedValue == MapEvent.LIGHT_VALUE_BLUE_FLASH)
                EventPreview.UpdateHoverEventValue(IsRedNote() ? MapEvent.LIGHT_VALUE_BLUE_FLASH : MapEvent.LIGHT_VALUE_RED_FLASH);
        }
    }

    private bool IsRedNote()
    {
        switch (EventPreview.QueuedValue)
        {
            case MapEvent.LIGHT_VALUE_RED_ON: return true;
            case MapEvent.LIGHT_VALUE_RED_FLASH: return true;
            case MapEvent.LIGHT_VALUE_RED_FADE: return true;
            default: return false;
        }
    }
}
