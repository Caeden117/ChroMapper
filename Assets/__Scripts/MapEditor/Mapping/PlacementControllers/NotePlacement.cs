using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>,
    CMInput.INotePlacementActions
{
    private const int UP_KEY = 0;
    private const int LEFT_KEY = 1;
    private const int DOWN_KEY = 2;
    private const int RIGHT_KEY = 3;

    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [FormerlySerializedAs("noteAppearanceSO")] [SerializeField] private NoteAppearanceSO noteAppearanceSo;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapNoteInputController beatmapNoteInputController;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    // TODO: Perhaps move this into Settings as a user-configurable option
    private readonly float
        diagonalStickMAXTime = 0.3f; // This controls the maximum time that a note will stay a diagonal

    // REVIEW: Perhaps partner with Obama to turn this list of bools
    // into some binary shifting goodness
    private readonly List<bool> heldKeys = new List<bool> {false, false, false, false};

    private bool diagonal;
    private bool flagDirectionsUpdate;

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            return false;
        }
    }

    public override bool IsValid
    {
        get
        {
            if (Settings.Instance.PrecisionPlacementGrid)
                return base.IsValid || (UsePrecisionPlacement && IsActive && !NodeEditorController.IsActive);
            return base.IsValid;
        }
    }

    public override int PlacementXMin => base.PlacementXMax * -1;

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }

    //TODO perhaps make a helper function to deal with the context.performed and context.canceled checks
    public void OnDownNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, DOWN_KEY);

    public void OnLeftNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, LEFT_KEY);

    public void OnUpNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, UP_KEY);

    public void OnRightNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, RIGHT_KEY);

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(false);
        UpdateCut(BeatmapNote.NoteCutDirectionAny);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NoteCutDirectionUpLeft);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NoteCutDirectionUpRight);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NoteCutDirectionDownRight);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut(BeatmapNote.NoteCutDirectionDownLeft);
    }

    // Toggle Chroma Color Function
    public void PlaceChromaObjects(bool v)
    {
        if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
            Settings.NonPersistentSettings[ChromaColorKey] = v;
        else
            Settings.NonPersistentSettings.Add(ChromaColorKey, v);
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");

    public override BeatmapNote GenerateOriginalData() =>
        new BeatmapNote(0, 0, 0, BeatmapNote.NoteTypeA, BeatmapNote.NoteCutDirectionDown);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            QueuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (QueuedData.CustomData != null && QueuedData.CustomData.HasKey("_color"))
            {
                QueuedData.CustomData.Remove("_color");

                if (QueuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    QueuedData.CustomData = null;
            }
        }

        if (UsePrecisionPlacement)
        {
            QueuedData.LineIndex = QueuedData.LineLayer = 0;

            InstantiatedContainer.transform.localPosition = roundedHit;

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x - 0.5f, 3);
            position[1] = Math.Round(roundedHit.y - 0.5f, 3);
            QueuedData.GetOrCreateCustomData()["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);
            if (QueuedData.CustomData != null && QueuedData.CustomData.HasKey("_position"))
            {
                QueuedData.CustomData.Remove("_position"); //Remove NE position since we are no longer working with it.

                if (QueuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    QueuedData.CustomData = null;
            }

            QueuedData.LineIndex = Mathf.RoundToInt(InstantiatedContainer.transform.localPosition.x + 1.5f);
            QueuedData.LineLayer = Mathf.RoundToInt(InstantiatedContainer.transform.localPosition.y - 0.5f);
        }

        UpdateAppearance();
    }

    public void UpdateCut(int value)
    {
        QueuedData.CutDirection = value;
        if (DraggedObjectContainer != null && DraggedObjectContainer.MapNoteData != null)
        {
            DraggedObjectContainer.MapNoteData.CutDirection = value;
            noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);
        }
        else if (beatmapNoteInputController.QuickModificationActive && Settings.Instance.QuickNoteEditing)
        {
            var note = ObjectUnderCursor();
            if (note != null && note.ObjectData is BeatmapNote noteData)
            {
                var newData = BeatmapObject.GenerateCopy(noteData);
                newData.CutDirection = value;

                BeatmapActionContainer.AddAction(
                    new BeatmapObjectModifiedAction(newData, noteData, noteData, "Quick edit"), true);
            }
        }

        UpdateAppearance();
    }

    public void UpdateType(int type)
    {
        QueuedData.Type = type;
        UpdateAppearance();
    }

    public void ChangeChromaToggle(bool isChromaToggleNote)
    {
        if (isChromaToggleNote)
        {
            var data = new BeatmapChromaNote(QueuedData) {BombRotation = BeatmapChromaNote.Alternate};
            QueuedData = data;
        }
        else if (QueuedData is BeatmapChromaNote data)
        {
            QueuedData = data.ConvertToNote();
        }

        UpdateAppearance();
    }

    public void UpdateChromaValue(int chromaNoteValue)
    {
        if (QueuedData is BeatmapChromaNote chroma)
        {
            chroma.BombRotation = chromaNoteValue;
            UpdateAppearance();
        }
    }

    private void UpdateAppearance()
    {
        if (InstantiatedContainer is null) return;
        InstantiatedContainer.MapNoteData = QueuedData;
        noteAppearanceSo.SetNoteAppearance(InstantiatedContainer);
        InstantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        InstantiatedContainer.UpdateMaterials();
        InstantiatedContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(QueuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged.Time = queued.Time;
        dragged.LineIndex = queued.LineIndex;
        dragged.LineLayer = queued.LineLayer;
        dragged.CutDirection = queued.CutDirection;
        if (DraggedObjectContainer != null)
            DraggedObjectContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(dragged);
        noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);
    }

    internal override void RefreshVisuals()
    {
        base.RefreshVisuals();
        InstantiatedContainer.SetArcVisible(false);
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleDirectionValues()
    {
        deleteToolController.UpdateDeletion(false);

        var upNote = heldKeys[UP_KEY];
        var downNote = heldKeys[DOWN_KEY];
        var leftNote = heldKeys[LEFT_KEY];
        var rightNote = heldKeys[RIGHT_KEY];
        var previousDiagonalState = diagonal;

        var handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        var handleLeftRightNotes = leftNote ^ rightNote;

        diagonal = handleUpDownNotes && handleLeftRightNotes;

        if (previousDiagonalState && diagonal == false)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote) UpdateCut(BeatmapNote.NoteCutDirectionUp);
            else UpdateCut(BeatmapNote.NoteCutDirectionDown);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote) UpdateCut(BeatmapNote.NoteCutDirectionLeft);
            else UpdateCut(BeatmapNote.NoteCutDirectionRight);
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote) UpdateCut(BeatmapNote.NoteCutDirectionUpLeft);
                else UpdateCut(BeatmapNote.NoteCutDirectionDownLeft);
            }
            else
            {
                if (upNote) UpdateCut(BeatmapNote.NoteCutDirectionUpRight);
                else UpdateCut(BeatmapNote.NoteCutDirectionDownRight);
            }
        }
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(diagonalStickMAXTime);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys.Except(heldKeys).Any()) flagDirectionsUpdate = true;
    }
}
