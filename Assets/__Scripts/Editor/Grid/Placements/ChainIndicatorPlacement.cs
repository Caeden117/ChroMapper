using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

// This is almost the same as ArcIndicatorPlacement
public class ChainIndicatorPlacement : BasePlacement<BaseChain, ChainIndicatorContainer, ChainGridContainer>
{
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapSharedNoteInputController beatmapSharedNoteInputController;

    public override void Start()
    {
        base.Start();
        beatmapSharedNoteInputController.OnCutDirectionChanged += HandleOnCutDirectionChanged;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        beatmapSharedNoteInputController.OnCutDirectionChanged -= HandleOnCutDirectionChanged;
    }

    private void HandleOnCutDirectionChanged(int value)
    {
        if (DraggedObjectContainer != null && DraggedObjectContainer.ParentChain != null)
        {
            if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
            {
                QueuedData.CutDirection = value;
                DraggedObjectContainer.ParentChain.ChainData.CutDirection = value;
            }
        }
    }

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = base.StartDrag(draggedObject);
        if (IsDragging) DraggedObjectContainer.ParentChain.Dragged = true;

        return con;
    }

    protected override List<BeatmapAction> PerformPreFinishDragActions()
    {
        DraggedObjectContainer.ParentChain.Dragged = false;

        return new List<BeatmapAction>();
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Edited a chain.");

    protected override BaseChain GenerateOriginalData() => new();

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;

        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            var roundedPoint = new Vector3(
                Mathf.Round(localPoint.x * precision) / precision,
                Mathf.Round(localPoint.y * precision) / precision,
                placementZ);
            PlacementVisualContainer.transform.localPosition = roundedPoint;
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - (BeatmapConstant.PlayerYOffset / 2f))
                / BeatmapConstant.LaneSize;
            var snappedPosition = SnapWithHysteresis(rawX, rawY);

            LanePosition = new Vector3(
                snappedPosition.x + (gridViewController.IsOdd ? 0.5f : 0f),
                snappedPosition.y,
                0f);
            var snappedPoint = BeatmapPositionHelper.LanePositionToLocalPosition(
                LanePosition,
                Bounds,
                BeatmapConstant.PlayerYOffset / 2f);
            snappedPoint.z = placementZ;
            PlacementVisualContainer.transform.localPosition = snappedPoint;
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        var pos = (Vector2)LanePosition;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        var coordinate = new Vector2(pos.x - 2f, pos.y);
        if (PrecisionPlacementController.IsEnabled)
        {
            if (inputState == PlacementInputState.Hover) return;
            switch (DraggedObjectContainer.IndicatorType)
            {
                case IndicatorType.Head:
                    QueuedData.CustomCoordinate = coordinate;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = coordinate;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            if (inputState == PlacementInputState.Hover) return;
            switch (DraggedObjectContainer.IndicatorType)
            {
                case IndicatorType.Head:
                    QueuedData.CustomCoordinate = !(Mathf.Approximately(vanillaX, pos.x)
                        && Mathf.Approximately(vanillaY, pos.y))
                        ? coordinate
                        : null;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = !(Mathf.Approximately(vanillaX, pos.x)
                        && Mathf.Approximately(vanillaY, pos.y))
                        ? coordinate
                        : null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override void TransferQueuedToDraggedObject(ref BaseChain dragged, BaseChain queued)
    {
        switch (DraggedObjectContainer.IndicatorType)
        {
            case IndicatorType.Head:
                dragged.JsonTime = queued.JsonTime;
                dragged.PosX = queued.PosX;
                dragged.PosY = queued.PosY;
                dragged.CutDirection = queued.CutDirection;
                dragged.CustomCoordinate = queued.CustomCoordinate;
                if (dragged.Rotation != queued.Rotation)
                {
                    dragged.Rotation = queued.Rotation;
                    TracksManager.RefreshTracks();
                }

                break;
            case IndicatorType.Tail:
                dragged.TailJsonTime = queued.JsonTime;
                dragged.TailPosX = queued.PosX;
                dragged.TailPosY = queued.PosY;
                dragged.CustomTailCoordinate = queued.CustomTailCoordinate;
                if (dragged.TailRotation != queued.Rotation)
                {
                    dragged.TailRotation = queued.Rotation;
                    TracksManager.RefreshTracks();
                }

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        DraggedObjectContainer.ParentChain.AdjustTimePlacement();
        DraggedObjectContainer.ParentChain.GenerateChain(dragged);
    }

    public override void Apply() { }

    // This placement controller is only used for dragging the chain indicator
    public void OnPlaceObject(InputAction.CallbackContext context) { }

    public override float GetContainerPosZ(ObjectContainer con)
    {
        if (con is ChainIndicatorContainer indicator)
        {
            if (indicator.IndicatorType == IndicatorType.Head)
            {
                return (indicator.ParentChain.ChainData.SongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }

            if (indicator.IndicatorType == IndicatorType.Tail)
            {
                return (indicator.ParentChain.ChainData.TailSongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }
        }

        return base.GetContainerPosZ(con);
    }

    protected override float GetDraggedObjectJsonTime()
    {
        if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail) return DraggedObjectData.TailJsonTime;

        return DraggedObjectData.JsonTime;
    }
}
