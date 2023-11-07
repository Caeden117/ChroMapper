using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcIndicatorPlacement : PlacementController<BaseArc, ArcIndicatorContainer, ArcGridContainer>,
    CMInput.INotePlacementActions
{
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;

    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private LaserSpeedController laserSpeedController;

    public override int PlacementXMin => PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Edited an arc.");

    public override BaseArc GenerateOriginalData() => new V3Arc();

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 roundedHit)
    {
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

            if (IsDraggingObject || IsDraggingObjectAtTime)
            {
                if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
                {
                    queuedData.CustomCoordinate = (Vector2)roundedHit;
                }

                if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
                {
                    queuedData.CustomTailCoordinate = (Vector2)roundedHit;
                }
            }

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            precisionPlacement.TogglePrecisionPlacement(false);

            if (IsDraggingObject || IsDraggingObjectAtTime)
            {
                if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
                {
                    queuedData.CustomCoordinate = !vanillaBounds
                        ? (JSONNode)((Vector2)roundedHit - vanillaOffset + precisionOffset)
                        : null;
                }

                if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
                {
                    queuedData.CustomTailCoordinate = !vanillaBounds
                        ? (JSONNode)((Vector2)roundedHit - vanillaOffset + precisionOffset)
                        : null;
                }
            }
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseArc dragged, BaseArc queued)
    {
        if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
        {
            dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
            dragged.PosX = queued.PosX;
            dragged.PosY = queued.PosY;
            dragged.CutDirection = queued.CutDirection;
            dragged.CustomCoordinate = queued.CustomCoordinate;
        }

        if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
        {
            dragged.SetTailTimes(queued.JsonTime, queued.SongBpmTime);
            dragged.TailPosX = queued.PosX;
            dragged.TailPosY = queued.PosY;
            dragged.TailCutDirection = queued.TailCutDirection;
            dragged.CustomTailCoordinate = queued.CustomTailCoordinate;
        }

        DraggedObjectContainer.ParentArc.NotifySplineChanged(dragged);
    }

    public override void OnPlaceObject(InputAction.CallbackContext context)
    {
        // This placement controller is only used for dragging the arc indicator
    }

    protected override float GetContainerPosZ(ObjectContainer con)
    {
        if (con is ArcIndicatorContainer indicator)
        {
            if (indicator.IndicatorType == IndicatorType.Head)
            {
                return (indicator.ParentArc.ArcData.SongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
            }
            if (indicator.IndicatorType == IndicatorType.Tail)
            {
                return (indicator.ParentArc.ArcData.TailSongBpmTime - Atsc.CurrentSongBpmTime) * EditorScaleController.EditorScale;
            }
        }

        return base.GetContainerPosZ(con);
    }

    protected override float GetDraggedObjectJsonTime()
    {
        if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
        {
            return draggedObjectData.TailJsonTime;
        }
        else
        {
            return draggedObjectData.JsonTime;
        }
    }

    public void UpdateCut(int value)
    {
        if (DraggedObjectContainer != null && DraggedObjectContainer.ParentArc != null)
        {
            if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
            {
                queuedData.CutDirection = value;
                DraggedObjectContainer.ParentArc.ArcData.CutDirection = value;
            }
            if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
            {
                queuedData.TailCutDirection = value;
                DraggedObjectContainer.ParentArc.ArcData.TailCutDirection = value;
            }
        }
    }

    // Below is copied from NotePlacement. Would be nice to have some kind of shared placement.
    private readonly float diagonalStickMAXTime = 0.3f;
    private readonly List<bool> heldKeys = new List<bool> { false, false, false, false };
    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;
    private bool diagonal;
    private bool flagDirectionsUpdate;


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

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }
}
