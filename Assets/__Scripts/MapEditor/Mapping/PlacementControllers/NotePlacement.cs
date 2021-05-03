using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>, CMInput.INotePlacementActions
{
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapNoteInputController beatmapNoteInputController;

    private bool diagonal = false;
    private bool flagDirectionsUpdate = false;

    private const int UpKey = 0;
    private const int LeftKey = 1;
    private const int DownKey = 2;
    private const int RightKey = 3;

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private List<bool> HeldKeys = new List<bool>() { false, false, false, false };

    // TODO: Perhaps move this into Settings as a user-configurable option
    private readonly float DIAGONAL_STICK_MAX_TIME = 0.3f; // This controls the maximum time that a note will stay a diagonal

    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    // Toggle Chroma Color Function
    public void PlaceChromaObjects(bool v)
    {
        if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
        {
            Settings.NonPersistentSettings[ChromaColorKey] = v;
        }
        else
        {
            Settings.NonPersistentSettings.Add(ChromaColorKey, v);
        }
    }

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
            {
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            }
            return false;
        }
    }

    public override bool IsValid
    {
        get
        {
            if (Settings.Instance.PrecisionPlacementGrid)
            {
                return base.IsValid || (usePrecisionPlacement && IsActive && !NodeEditorController.IsActive);
            }
            else
            {
                return base.IsValid;
            }
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_A, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData._customData != null && queuedData._customData.HasKey("_color"))
            {
                queuedData._customData.Remove("_color");

                if (queuedData._customData.Count <= 0) //Set customData to null if there is no customData to store
                {
                    queuedData._customData = null;
                }
            }
        }

        if (usePrecisionPlacement)
        {
            queuedData._lineIndex = queuedData._lineLayer = 0;

            instantiatedContainer.transform.localPosition = roundedHit;

            JSONArray position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            queuedData.GetOrCreateCustomData()["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);
            if (queuedData._customData != null && queuedData._customData.HasKey("_position"))
            {
                queuedData._customData.Remove("_position"); //Remove NE position since we are no longer working with it.

                if (queuedData._customData.Count <= 0) //Set customData to null if there is no customData to store
                {
                    queuedData._customData = null;
                }
            }
            queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
            queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        }

        UpdateAppearance();
    }

    public void UpdateCut(int value)
    {
        queuedData._cutDirection = value;
        if (draggedObjectContainer != null && draggedObjectContainer.mapNoteData != null)
        {
            draggedObjectContainer.mapNoteData._cutDirection = value;
            noteAppearanceSO?.SetNoteAppearance(draggedObjectContainer);
        } else if (beatmapNoteInputController.QuickModificationActive && Settings.Instance.QuickNoteEditing) {
            var note = ObjectUnderCursor();
            if (note != null && note.objectData is BeatmapNote noteData) {
                var newData = BeatmapObject.GenerateCopy(noteData);
                newData._cutDirection = value;

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(newData, noteData, noteData, "Quick edit"), true);
            }
        }
        UpdateAppearance();
    }

    public void UpdateType(int type)
    {
        queuedData._type = type;
        UpdateAppearance();
    }

    public void ChangeChromaToggle(bool isChromaToggleNote)
    {
        if (isChromaToggleNote)
        {
            BeatmapChromaNote data = new BeatmapChromaNote(queuedData);
            data.BombRotation = BeatmapChromaNote.ALTERNATE;
            queuedData = data;
        }
        else if (queuedData is BeatmapChromaNote data) queuedData = data.ConvertToNote();
        UpdateAppearance();
    }

    public void UpdateChromaValue(int chromaNoteValue)
    {
        if (queuedData is BeatmapChromaNote chroma)
        {
            chroma.BombRotation = chromaNoteValue;
            UpdateAppearance();
        }
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) return;
        instantiatedContainer.mapNoteData = queuedData;
        noteAppearanceSO.SetNoteAppearance(instantiatedContainer);
        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.material.HasProperty("_AlwaysTranslucent") && renderer.material.GetFloat("_AlwaysTranslucent") == 1)
                continue; //Dont want to do this shit almost every frame.
            renderer.material.SetFloat("_AlwaysTranslucent", 1);
        }
        instantiatedContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
        dragged._cutDirection = queued._cutDirection;
        if (draggedObjectContainer != null)
        {
            draggedObjectContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(dragged);
        }
        noteAppearanceSO?.SetNoteAppearance(draggedObjectContainer);
    }

    //TODO perhaps make a helper function to deal with the context.performed and context.canceled checks
    public void OnDownNote(InputAction.CallbackContext context)
    {
        HandleKeyUpdate(context, DownKey);
    }

    public void OnLeftNote(InputAction.CallbackContext context)
    {
        HandleKeyUpdate(context, LeftKey);
    }

    public void OnUpNote(InputAction.CallbackContext context)
    {
        HandleKeyUpdate(context, UpKey);
    }

    public void OnRightNote(InputAction.CallbackContext context)
    {
        HandleKeyUpdate(context, RightKey);
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ HeldKeys[id]) flagDirectionsUpdate = true;
        HeldKeys[id] = context.performed;
    }

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }

    private void HandleDirectionValues()
    {
        deleteToolController.UpdateDeletion(false);

        bool upNote = HeldKeys[UpKey];
        bool downNote = HeldKeys[DownKey];
        bool leftNote = HeldKeys[LeftKey];
        bool rightNote = HeldKeys[RightKey];
        bool previousDiagonalState = diagonal;

        bool handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        bool handleLeftRightNotes = leftNote ^ rightNote;

        diagonal = handleUpDownNotes && handleLeftRightNotes;

        if (previousDiagonalState == true && diagonal == false)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP);
            else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
            else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);

        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
                else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
            }
            else
            {
                if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
                else UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
            }
        }
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        List<bool> previousHeldKeys = new List<bool>(HeldKeys);
        yield return new WaitForSeconds(DIAGONAL_STICK_MAX_TIME);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys.Except(HeldKeys).Any())
        {
            flagDirectionsUpdate = true;
        }
    }

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(false);
        UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_ANY);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
    }
}
