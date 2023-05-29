using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class NotePlacement : PlacementController<BaseNote, NoteContainer, NoteGridContainer>,
    CMInput.INotePlacementActions
{
    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private NoteAppearanceSO noteAppearanceSo;
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
    private readonly List<bool> heldKeys = new List<bool> { false, false, false, false };

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
    public void OnDownNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);

    public void OnLeftNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnUpNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);

    public void OnRightNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(false);
        UpdateCut((int)NoteCutDirection.Any);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut((int)NoteCutDirection.UpLeft);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut((int)NoteCutDirection.UpRight);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut((int)NoteCutDirection.DownRight);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated)
            UpdateCut((int)NoteCutDirection.DownLeft);
    }

    // Toggle Chroma Color Function
    public void PlaceChromaObjects(bool v)
    {
        if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
            Settings.NonPersistentSettings[ChromaColorKey] = v;
        else
            Settings.NonPersistentSettings.Add(ChromaColorKey, v);
    }

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");

    public override BaseNote GenerateOriginalData() => BeatmapFactory.Note(0, 0, 0, (int)NoteColor.Red, (int)NoteCutDirection.Down, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 _)
    {
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, SongBpmTime * EditorScaleController.EditorScale);

        // Check if Chroma Color notes button is active and apply _color
        queuedData.CustomColor = (CanPlaceChromaObjects && dropdown.Visible)
            ? (Color?)colorPicker.CurrentColor
            : null;

        var posX = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
        var posY = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 1.5f);

        var vanillaX = Mathf.Clamp(posX, 0, 3);
        var vanillaY = Mathf.Clamp(posY, 0, 2);

        var vanillaBounds = vanillaX == posX && vanillaY == posY;

        queuedData.PosX = vanillaX;
        queuedData.PosY = vanillaY;

        if (UsePrecisionPlacement)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit.x = Mathf.Round((roundedHit.x - 0.5f) * precision) / precision;
            roundedHit.y = Mathf.Round((roundedHit.y - 0.5f) * precision) / precision;
            instantiatedContainer.transform.localPosition = roundedHit;

            queuedData.CustomCoordinate = new Vector2(roundedHit.x, roundedHit.y - 1f);

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            queuedData.CustomCoordinate = !vanillaBounds
                ? (JSONNode)new Vector2(Mathf.Round(roundedHit.x - 0.5f), Mathf.Round(roundedHit.y - 1.5f))
                : null;
        }

        UpdateAppearance();
    }

    public void UpdateCut(int value)
    {
        ToggleDiagonalAngleOffset(queuedData, value);
        queuedData.CutDirection = value;
        if (DraggedObjectContainer != null && DraggedObjectContainer.NoteData != null)
        {
            ToggleDiagonalAngleOffset(DraggedObjectContainer.NoteData, value);
            DraggedObjectContainer.NoteData.CutDirection = value;
            noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);
        }
        else if (beatmapNoteInputController.QuickModificationActive && Settings.Instance.QuickNoteEditing)
        {
            var note = ObjectUnderCursor();
            if (note != null && note.ObjectData is BaseNote noteData)
            {
                var originalData = BeatmapFactory.Clone(noteData);
                ToggleDiagonalAngleOffset(noteData, value);
                noteData.CutDirection = value;

                var actions = new List<BeatmapAction>{
                    new BeatmapObjectModifiedAction(noteData, noteData, originalData, "Quick edit", true)
                };
                CommonNotePlacement.UpdateAttachedSlidersDirection(noteData, actions);

                if (actions.Count > 1)
                {
                    BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, false, "Quick edit"), true);
                    SelectionController.SelectionChangedEvent?.Invoke();
                }
                else
                {
                    BeatmapActionContainer.AddAction(actions[0], true);
                }
            }
        }

        UpdateAppearance();
    }

    private void ToggleDiagonalAngleOffset(BaseNote note, int newCutDirection)
    {
        if (note is V3ColorNote colorNote)
        {
            if (colorNote.CutDirection == (int)NoteCutDirection.Any && newCutDirection == (int)NoteCutDirection.Any
                && colorNote.AngleOffset != 45)
            {
                colorNote.AngleOffset = 45;
            }
            else
            {
                colorNote.AngleOffset = 0;
            }
        }
    }

    public void UpdateType(int type)
    {
        queuedData.Type = type;
        UpdateAppearance();
    }

    public void ChangeChromaToggle(bool isChromaToggleNote)
    {
        if (isChromaToggleNote)
        {
            var data = new V2ChromaNote(queuedData) { BombRotation = V2ChromaNote.Alternate };
            queuedData = data;
        }
        else if (queuedData is V2ChromaNote data)
        {
            queuedData = data.ConvertToNote();
        }

        UpdateAppearance();
    }

    public void UpdateChromaValue(int chromaNoteValue)
    {
        if (queuedData is V2ChromaNote chroma)
        {
            chroma.BombRotation = chromaNoteValue;
            UpdateAppearance();
        }
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) return;
        instantiatedContainer.NoteData = queuedData;
        noteAppearanceSo.SetNoteAppearance(instantiatedContainer);
        instantiatedContainer.MaterialPropertyBlock.SetFloat("_AlwaysTranslucent", 1);
        instantiatedContainer.UpdateMaterials();
        instantiatedContainer.transform.localEulerAngles = NoteContainer.Directionalize(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CutDirection = queued.CutDirection;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (DraggedObjectContainer != null)
            DraggedObjectContainer.transform.localEulerAngles = NoteContainer.Directionalize(dragged);
        noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);

        TransferQueuedToAttachedDraggedSliders(queued);
    }

    private void TransferQueuedToAttachedDraggedSliders(BaseNote queued)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Head])
        {
            baseSlider.SetTimes(queued.JsonTime, queued.SongBpmTime);
            baseSlider.PosX = queued.PosX;
            baseSlider.PosY = queued.PosY;
            baseSlider.CutDirection = queued.CutDirection;
            baseSlider.CustomCoordinate = queued.CustomCoordinate;
        }

        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Tail])
        {
            baseSlider.SetTailTimes(queued.JsonTime, queued.SongBpmTime);
            baseSlider.TailPosX = queued.PosX;
            baseSlider.TailPosY = queued.PosY;
            baseSlider.CustomTailCoordinate = queued.CustomCoordinate;

            if (baseSlider is BaseArc baseArc)
            {
                baseArc.TailCutDirection = queued.CutDirection;
            }
        }

        foreach (var baseSliderContainer in DraggedAttachedSliderContainers)
        {
            if (baseSliderContainer is ArcContainer arcContainer) arcContainer.NotifySplineChanged();
            if (baseSliderContainer is ChainContainer chainContainer) chainContainer.GenerateChain();
        }
    }

    internal override void RefreshVisuals()
    {
        base.RefreshVisuals();
        instantiatedContainer.SetArcVisible(false);
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleDirectionValues()
    {
        deleteToolController.UpdateDeletion(false);

        var upNote = heldKeys[upKey];
        var downNote = heldKeys[downKey];
        var leftNote = heldKeys[leftKey];
        var rightNote = heldKeys[rightKey];
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
            if (upNote) UpdateCut((int)NoteCutDirection.Up);
            else UpdateCut((int)NoteCutDirection.Down);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote) UpdateCut((int)NoteCutDirection.Left);
            else UpdateCut((int)NoteCutDirection.Right);
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote) UpdateCut((int)NoteCutDirection.UpLeft);
                else UpdateCut((int)NoteCutDirection.DownLeft);
            }
            else
            {
                if (upNote) UpdateCut((int)NoteCutDirection.UpRight);
                else UpdateCut((int)NoteCutDirection.DownRight);
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
