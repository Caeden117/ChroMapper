using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ObstaclePlacement : PlacementController<BaseObstacle, ObstacleContainer, ObstacleGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [FormerlySerializedAs("obstacleAppearanceSO")][SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private PrecisionPlacementGridController precisionPlacement;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    private int originIndex;
    private Vector2 originPosition;

    private float startJsonTime;
    private float startSongBpmTime;

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

    public static bool IsPlacing { get; private set; }

    public override int PlacementXMin => base.PlacementXMax * -1;

    private float SmallestRankableWallDuration => Atsc.GetBeatFromSeconds(0.016f);

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Place a Wall.");

    public override BaseObstacle GenerateOriginalData() => BeatmapFactory.Obstacle(0, 0, 0, (int)ObstacleType.Full, 0, 1, (int)ObstacleHeight.Full);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        Bounds = default;
        TestForType<ObstaclePlacement>(hit, ObjectType.Obstacle);

        instantiatedContainer.ObstacleData = queuedData;
        instantiatedContainer.ObstacleData.Duration = RoundedJsonTime - startJsonTime;
        obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);

        // Check if Chroma Color notes button is active and apply _color
        queuedData.CustomColor = (CanPlaceChromaObjects && dropdown.Visible)
            ? (Color?)colorPicker.CurrentColor
            : null;

        var wallTransform = instantiatedContainer.Animator.LocalTarget;

        if (IsPlacing)
        {
            if (UsePrecisionPlacement)
            {
                var precision = Settings.Instance.PrecisionPlacementGridPrecision;
                var precision_r = 1.0f / precision;
                roundedHit.x = Mathf.Floor((roundedHit.x) * precision) * precision_r;
                roundedHit.y = Mathf.Floor((roundedHit.y - 0.6f) * precision) * precision_r + 0.5f;
                roundedHit.z = SongBpmTime * EditorScaleController.EditorScale;

                var position = (Vector3)originPosition;
                position.z = startSongBpmTime * EditorScaleController.EditorScale;
                var newLocalScale = roundedHit - position + new Vector3(precision_r, precision_r, 0);
                if (newLocalScale.x <= 0)
                {
                    position.x = originPosition.x + newLocalScale.x - precision_r;
                    queuedData.CustomCoordinate[0] = position.x;
                    newLocalScale.x = 2 * precision_r - newLocalScale.x;
                }
                if (newLocalScale.y <= 0)
                {
                    position.y = originPosition.y + newLocalScale.y - precision_r;
                    queuedData.CustomCoordinate[1] = position.y;
                    newLocalScale.y = 2 * precision_r - newLocalScale.y;
                }

                var localPosition = new Vector3(
                    position.x + (newLocalScale.x * 0.5f),
                    position.y,
                    0);
                wallTransform.localPosition = localPosition;
                instantiatedContainer.transform.localPosition = new Vector3(0, 0.1f, startSongBpmTime * EditorScaleController.EditorScale);

                instantiatedContainer.SetScale(newLocalScale);

                if (queuedData.CustomSize == null)
                {
                    queuedData.CustomSize = new JSONArray();
                }
                queuedData.CustomSize[0] = newLocalScale.x;
                queuedData.CustomSize[1] = newLocalScale.y;

                precisionPlacement.TogglePrecisionPlacement(true);
                precisionPlacement.UpdateMousePosition(hit.Point);
            }
            else
            {
                queuedData.CustomCoordinate = null;
                queuedData.CustomSize = null;

                roundedHit = new Vector3(
                    Mathf.Ceil(Math.Min(Math.Max(roundedHit.x, Bounds.min.x + 0.01f), Bounds.max.x)),
                    Mathf.Ceil(Math.Min(Math.Max(roundedHit.y, 0.01f), 3f)),
                    SongBpmTime * EditorScaleController.EditorScale
                );

                queuedData.Width = Mathf.CeilToInt(roundedHit.x + 2) - originIndex;
                if (queuedData.Width < 1)
                {
                    queuedData.PosX = originIndex + 1;
                    queuedData.Width -= 2;
                }
                else
                {
                    queuedData.PosX = originIndex;
                }
                wallTransform.localPosition = new Vector3(
                    queuedData.PosX - 2f + (queuedData.Width / 2.0f),
                    (queuedData.Type == (int)ObstacleType.Full) ? 0 : 2,
                    0);
                instantiatedContainer.transform.localPosition = new Vector3(0, 0.1f, startSongBpmTime * EditorScaleController.EditorScale);

                instantiatedContainer.SetScale(new Vector3(queuedData.Width,
                    wallTransform.localScale.y, queuedData.Duration * EditorScaleController.EditorScale));

                precisionPlacement.TogglePrecisionPlacement(false);
            }

            return;
        }

        startJsonTime = RoundedJsonTime;
        instantiatedContainer.ObstacleData.Duration = SmallestRankableWallDuration;

        if (UsePrecisionPlacement)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit.x = Mathf.Floor((roundedHit.x) * precision) / precision;
            roundedHit.y = Mathf.Floor((roundedHit.y - 0.6f) * precision) / precision + 0.5f;
            var size = Vector3.one / precision;

            wallTransform.localPosition = roundedHit + new Vector3(size.x * 0.5f, 0, 0);

            instantiatedContainer.SetScale(size);
            queuedData.PosX = queuedData.Type = 0;

            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();
            queuedData.CustomCoordinate = (Vector2)roundedHit;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            queuedData.CustomCoordinate = null;
            queuedData.CustomSize = null;

            var vanillaType = roundedHit.y <= 2 ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;

            queuedData.PosX = Mathf.RoundToInt(transformedPoint.x);
            queuedData.PosY = vanillaType == (int)ObstacleType.Full ? 0 : 2;
            queuedData.Height = vanillaType == (int)ObstacleType.Full ? 5 : 3;
            queuedData.Type = vanillaType;

            wallTransform.localPosition = new Vector3(
                transformedPoint.x - 1.5f,
                queuedData.PosY,
                0);
            instantiatedContainer.transform.localPosition = new Vector3(0, 0.1f, transformedPoint.z);

            instantiatedContainer.SetScale(new Vector3(1, vanillaType == (int)ObstacleType.Full ? 5f : 3f, 0));

            precisionPlacement.TogglePrecisionPlacement(false);
        }
    }

    public override void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        base.OnMousePositionUpdate(context);
        if (IsPlacing)
        {
            var scale = instantiatedContainer.GetScale();
            instantiatedContainer.SetScale(new Vector3(scale.x,
                scale.y,
                (SongBpmTime - startSongBpmTime) * EditorScaleController.EditorScale));
        }
    }

    internal override void ApplyToMap()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            queuedData.SetTimes(startJsonTime, startSongBpmTime);

            var endSongBpmTime = startSongBpmTime + (instantiatedContainer.GetScale().z / EditorScaleController.EditorScale);
            var endJsonTime = BpmChangeGridContainer.SongBpmTimeToJsonTime(endSongBpmTime);

            if (endSongBpmTime - startSongBpmTime < SmallestRankableWallDuration &&
                Settings.Instance.DontPlacePerfectZeroDurationWalls)
            {
                endSongBpmTime = startSongBpmTime + SmallestRankableWallDuration;
                endJsonTime = BpmChangeGridContainer.SongBpmTimeToJsonTime(endSongBpmTime);
                queuedData.Duration = endJsonTime - startJsonTime;
            }

            objectContainerCollection.SpawnObject(queuedData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(queuedData, conflicting));
            queuedData = BeatmapFactory.Clone(queuedData);
            instantiatedContainer.ObstacleData = queuedData;
            obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.SetScale(new Vector3(
                1, instantiatedContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f, 0));
        }
        else
        {
            IsPlacing = true;
            originIndex = queuedData.PosX;
            originPosition = (queuedData.CustomCoordinate?.ReadVector2() ?? new Vector2(originIndex, 5 - queuedData.Height));
            startJsonTime = RoundedJsonTime;
            startSongBpmTime = SongBpmTime;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseObstacle dragged, BaseObstacle queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
        dragged.PosX = queued.PosX;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }

    public override void CancelPlacement()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            queuedData = GenerateOriginalData();
            instantiatedContainer.ObstacleData = queuedData;
            obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.SetScale(new Vector3(
                1, instantiatedContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f, 0));
        }
    }
}
