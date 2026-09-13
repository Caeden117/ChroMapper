using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class ObstaclePlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>
{
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private ColorPicker colorPicker;
    private bool hasExpanded;
    private bool hasOffset;

    private int originIndex;
    private Vector3 originPos;
    private Vector3 scale;

    private float startJsonTime;

    private bool v2Mode;

    private float SmallestRankableWallDuration => Atsc.GetBeatFromSeconds(0.016f);

    // Keep a wall's first click when the cursor crosses a gap between grid planes during its two-click placement.
    public override bool RetainsPendingPlacementOnInvalidHit => true;

    public void Awake() => LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;

    public override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Place a Wall.");

    protected override BaseObstacle GenerateOriginalData() => new();
    private void HandleLevelLoaded() => v2Mode = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.ObstacleData = QueuedData;
        obstacleAppearance.SetObstacleAppearance(PlacementVisualContainer, true);
    }

    public override void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState)
    {
        if (IsPlacing && !AllowPlacement) Cancel();
        base.UpdateState(hit, inputState);
    }

    // Wall transform anchor on bottom middle
    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var size = 1f;
        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            size = 1f / precision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePosition(
                localPoint,
                precision,
                BeatmapConstant.ObstacleYOffset);
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - BeatmapConstant.ObstacleYOffset)
                / BeatmapConstant.LaneSize;
            var snappedPosition = SnapWithHysteresis(rawX, rawY);
            LanePosition = new Vector3(snappedPosition.x, snappedPosition.y, 0f);
        }

        LanePosition.x += (size / 2f) + (gridViewController.IsOdd ? 0.5f : 0f);
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);
        LanePosition.z = zPlacement;

        if (!IsPlacing)
            scale = new Vector3(size, size, Mathf.Epsilon);
        else
        {
            var originShove = originPos;
            var sizeX = size;
            var sizeY = size;

            // there's probably elegant way to do this,
            // i just cant think now
            if (LanePosition.x < originPos.x)
            {
                var difference = Mathf.Abs(LanePosition.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (LanePosition.y < originPos.y)
            {
                var difference = Mathf.Abs(LanePosition.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            scale = LanePosition + new Vector3(sizeX, sizeY, 0f) - originShove;
            LanePosition = originShove + new Vector3((scale.x - size) / 2f, 0f, 0f);
        }

        if (v2Mode && !PrecisionPlacementController.IsEnabled)
        {
            if (LanePosition.y < 1.5)
            {
                LanePosition.y = 0;
                scale.y = 5f;
            }
            else
            {
                LanePosition.y = 2;
                scale.y = 3f;
            }
        }

        PlacementVisualContainer.transform.localPosition =
            BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.ObstacleYOffset);
        if (scale != PlacementVisualContainer.ObstacleScale / BeatmapConstant.LaneSize)
            PlacementVisualContainer.SetScale(scale * BeatmapConstant.LaneSize);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (!IsPlacing)
        {
            startJsonTime = RoundedJsonTime;
            PlacementVisualContainer.ObstacleData.Duration = SmallestRankableWallDuration;
        }
        else
        {
            // Normalize reverse drags because obstacles store an earlier start time and positive duration only.
            NormalizePlacementTimeRange();
        }
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);

        // Walls use the gameplay-object Chroma setting, independently from Chroma lighting event placement.
        QueuedData.CustomColor = BasePlacement.CanPlaceChromaObjects && colorPicker != null
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.y -= 0.5f;

        // let's not talk about this
        QueuedData.Type = pos.y < 2
            ? (int)ObstacleType.Full
            : (int)ObstacleType.Crouch;

        var vanillaPos = new Vector2(Mathf.FloorToInt(pos.x - (scale.x / 2f)), Mathf.FloorToInt(pos.y));
        var coordinates = (Vector2)pos - new Vector2(scale.x / 2f, .5f);
        QueuedData.CustomCoordinate = vanillaPos != coordinates ? coordinates + Vector2.up : null;
        QueuedData.PosX = (int)vanillaPos.x + 2;
        QueuedData.PosY = (int)vanillaPos.y + 1;

        var vanillaSize = new Vector2(Mathf.CeilToInt(scale.x), Mathf.CeilToInt(scale.y));
        QueuedData.CustomSize = vanillaSize != (Vector2)scale ? (Vector2)scale : null;
        QueuedData.Width = (int)vanillaSize.x;
        QueuedData.Height = (int)vanillaSize.y;

        // Persist the placement color with the remaining custom wall properties before the placement action captures this wall.
        QueuedData.WriteCustom();

        // Refresh the hover and drag preview from the current queued wall so it immediately reflects the active Chroma color.
        PlacementVisualContainer.ObstacleData = QueuedData;
        obstacleAppearance.SetObstacleAppearance(PlacementVisualContainer, true);
    }

    protected override void HandleRotationChanged(float rotation) => QueuedData.Rotation = (int)rotation;

    public override void HandleApply()
    {
        if (IsPlacing)
        {
            // Normalize again at commit so an input update immediately before release cannot retain a reverse interval.
            NormalizePlacementTimeRange();

            var startSongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(QueuedData.JsonTime);
            var endSongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                QueuedData.JsonTime + QueuedData.Duration);

            if (endSongBpmTime - startSongBpmTime < SmallestRankableWallDuration)
            {
                endSongBpmTime = startSongBpmTime + SmallestRankableWallDuration;
                var endJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(endSongBpmTime);
                QueuedData.Duration = endJsonTime - QueuedData.JsonTime;
            }

            ObjectContainerCollection.SpawnObject(QueuedData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(QueuedData, conflicting));
            QueuedData = BeatmapFactory.Clone(QueuedData);
            PlacementVisualContainer.ObstacleData = QueuedData;
            // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
            State = PlacementState.Idle;
        }
        else
        {
            originPos = LanePosition;
            startJsonTime = RoundedJsonTime;
            State = PlacementState.Placing;
        }
    }

    // Store the two placement clicks as the forward interval required by Beat Saber obstacle data.
    private void NormalizePlacementTimeRange()
    {
        var endJsonTime = RoundedJsonTime;
        if (Mathf.Approximately(startJsonTime, endJsonTime)
            && QueuedData.Duration > SmallestRankableWallDuration)
        {
            // Direct placement callers can provide an authored duration without a hover update; do not replace it with a zero-length drag.
            return;
        }

        QueuedData.JsonTime = Mathf.Min(startJsonTime, endJsonTime);
        QueuedData.Duration = Mathf.Max(Mathf.Abs(endJsonTime - startJsonTime), SmallestRankableWallDuration);
    }

    protected override void TransferQueuedToDraggedObject(ref BaseObstacle dragged, BaseObstacle queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (dragged.Rotation != queued.Rotation)
        {
            dragged.Rotation = queued.Rotation;
            TracksManager.RefreshTracks();
        }
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        QueuedData.Rotation = (int)LaneRotationProvider.EditRotation;
    }

    public override void Cancel()
    {
        base.Cancel();
        if (!IsPlacing) return;
        State = PlacementState.Idle;
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
        PlacementVisualContainer.SetScale(
            new Vector3(
                1,
                PlacementVisualContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f,
                Mathf.Epsilon));
    }
}
