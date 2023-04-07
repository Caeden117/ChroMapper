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
    [SerializeField] private LaserSpeedController laserSpeedController;

    public override int PlacementXMin => PlacementXMax * -1;

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Edited an arc.");

    public override BaseArc GenerateOriginalData() => new V3Arc();

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        queuedData.PosX = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
        queuedData.PosY = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
    }

    public override void TransferQueuedToDraggedObject(ref BaseArc dragged, BaseArc queued)
    {
        if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
        {
            dragged.JsonTime = queued.JsonTime;
            dragged.PosX = queued.PosX;
            dragged.PosY = queued.PosY;
            dragged.CutDirection = queued.CutDirection;
        }

        if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail)
        {
            dragged.TailTime = queued.JsonTime;
            dragged.TailPosX = queued.PosX;
            dragged.TailPosY = queued.PosY;
            dragged.TailCutDirection = queued.TailCutDirection;
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
                return (indicator.ParentArc.ArcData.JsonTime - Atsc.CurrentBeat) * EditorScaleController.EditorScale;
            }
            if (indicator.IndicatorType == IndicatorType.Tail)
            {
                return (indicator.ParentArc.ArcData.TailTime - Atsc.CurrentBeat) * EditorScaleController.EditorScale;
            }
        }

        return base.GetContainerPosZ(con);
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
