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
    private bool updateAttachedSliderDirection;

    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");

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
    public void PlaceChromaObjects(bool v) => Settings.NonPersistentSettings[ChromaColorKey] = v;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");

    public override BaseNote GenerateOriginalData() => new BaseNote
    {
        Color = (int)NoteColor.Red, CutDirection = (int)NoteCutDirection.Down
    };

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 roundedHit)
    {
        // Check if Chroma Color notes button is active and apply _color
        queuedData.CustomColor = (CanPlaceChromaObjects && dropdown.Visible)
            ? colorPicker.CurrentColor
            : null;

        var posX = (int)roundedHit.x;
        var posY = (int)roundedHit.y;

        var vanillaX = Mathf.Clamp(posX, 0, 3);
        var vanillaY = Mathf.Clamp(posY, 0, 2);

        var vanillaBounds = vanillaX == posX && vanillaY == posY;

        queuedData.PosX = vanillaX;
        queuedData.PosY = vanillaY;

        if (UsePrecisionPlacement)
        {
            var rawHit = ParentTrack.InverseTransformPoint(hit.Point);
            rawHit.z = SongBpmTime * EditorScaleController.EditorScale;

            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit = ((Vector2)Vector2Int.RoundToInt((precisionOffset + (Vector2)rawHit) * precision)) / precision;
            instantiatedContainer.transform.localPosition = roundedHit;

            queuedData.CustomCoordinate = (Vector2)roundedHit;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            queuedData.CustomCoordinate = !vanillaBounds
                ? (Vector2)roundedHit - vanillaOffset + precisionOffset
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
            updateAttachedSliderDirection = true;
        }
        // TODO: This IsActive is a workaround to prevent ghost notes. This happens because bomb placement could be
        //       dragging a note and quick editing results in issues
        else if (IsActive && beatmapNoteInputController.QuickModificationActive && Settings.Instance.QuickNoteEditing)
        {
            var note = ObjectUnderCursor();
            if (note != null && note.ObjectData is BaseNote noteData)
            {
                var originalData = BeatmapFactory.Clone(noteData);
                ToggleDiagonalAngleOffset(noteData, value);
                noteData.CutDirection = value;

                var actions = new List<BeatmapAction>{
                    new BeatmapObjectModifiedAction(noteData, noteData, originalData, "Quick edit", true, mergeType: ActionMergeType.NoteDirectionChange)
                };
                CommonNotePlacement.UpdateAttachedSlidersDirection(noteData, actions);

                if (actions.Count > 1)
                {
                    BeatmapActionContainer.AddAction(new ActionCollectionAction(actions, true, false, "Quick edit", mergeType: ActionMergeType.NoteDirectionChange), true);
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
        if (note.CutDirection == (int)NoteCutDirection.Any && newCutDirection == (int)NoteCutDirection.Any
            && note.AngleOffset != 45)
        {
            note.AngleOffset = 45;
        }
        else
        {
            note.AngleOffset = 0;
        }
        
    }

    public void UpdateType(int type)
    {
        queuedData.Type = type;
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) return;
        instantiatedContainer.NoteData = queuedData;
        noteAppearanceSo.SetNoteAppearance(instantiatedContainer);
        instantiatedContainer.MaterialPropertyBlock.SetFloat(alwaysTranslucent, 1);
        instantiatedContainer.UpdateMaterials();
        instantiatedContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CutDirection = queued.CutDirection;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (DraggedObjectContainer != null)
        {
            DraggedObjectContainer.DirectionTarget.localEulerAngles = NoteContainer.Directionalize(dragged);
            DraggedObjectContainer.DirectionTargetEuler = NoteContainer.Directionalize(dragged);
        }
        noteAppearanceSo.SetNoteAppearance(DraggedObjectContainer);

        TransferQueuedToAttachedDraggedSliders(queued);
    }

    private void TransferQueuedToAttachedDraggedSliders(BaseNote queued)
    {
        var epsilon = BeatmapObjectContainerCollection.Epsilon;
        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Head])
        {
            baseSlider.JsonTime = queued.JsonTime;
            baseSlider.PosX = queued.PosX;
            baseSlider.PosY = queued.PosY;
            if (updateAttachedSliderDirection) baseSlider.CutDirection = queued.CutDirection;
            baseSlider.CustomCoordinate = queued.CustomCoordinate;
        }

        foreach (var baseSlider in DraggedAttachedSliderDatas[IndicatorType.Tail])
        {
            baseSlider.TailJsonTime = queued.JsonTime;
            baseSlider.TailPosX = queued.PosX;
            baseSlider.TailPosY = queued.PosY;
            baseSlider.CustomTailCoordinate = queued.CustomCoordinate;

            if (baseSlider is BaseArc baseArc && updateAttachedSliderDirection)
            {
                baseArc.TailCutDirection = queued.CutDirection;
            }
        }

        foreach (var baseSliderContainer in DraggedAttachedSliderContainers)
        {
            switch (baseSliderContainer)
            {
                case ArcContainer arcContainer:
                    arcContainer.NotifySplineChanged();
                    break;
                case ChainContainer chainContainer:
                    chainContainer.AdjustTimePlacement();
                    chainContainer.GenerateChain();
                    break;
            }
        }

        updateAttachedSliderDirection = false;
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
