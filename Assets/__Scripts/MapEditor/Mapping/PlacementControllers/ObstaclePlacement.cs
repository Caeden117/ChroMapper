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

    private float startTime;

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

    public override bool IsValid
    {
        get
        {
            if (Settings.Instance.PrecisionPlacementGrid)
                return base.IsValid || (UsePrecisionPlacement && IsActive && !NodeEditorController.IsActive);
            return base.IsValid;
        }
    }

    private float SmallestRankableWallDuration => Atsc.GetBeatFromSeconds(0.016f);

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Place a Wall.");

    public override BaseObstacle GenerateOriginalData() => BeatmapFactory.Obstacle(0, 0, 0, (int)ObstacleType.Full, 0, 1, (int)ObstacleHeight.Full);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        Bounds = default;
        TestForType<ObstaclePlacement>(hit, ObjectType.Obstacle);

        instantiatedContainer.ObstacleData = queuedData;
        instantiatedContainer.ObstacleData.Duration = RoundedTime - startTime;
        obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);

        // Check if ChromaToggle notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData.CustomColor != null)
            {
                queuedData.CustomColor = null;

                if (queuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    queuedData.CustomData = null;
            }
        }

        var wallTransform = instantiatedContainer.transform;

        if (IsPlacing)
        {
            if (UsePrecisionPlacement)
            {
                var precision = Atsc.GridMeasureSnapping;
                roundedHit.x = Mathf.Round(roundedHit.x * precision) / precision;
                roundedHit.y = Mathf.Round(roundedHit.y * precision) / precision;
                roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

                var position = Vector2.zero;
                if (queuedData.CustomCoordinate != null && queuedData.CustomCoordinate.IsArray)
                {
                    if (queuedData.CustomCoordinate[0].IsNumber) position.x = queuedData.CustomCoordinate[0];
                    if (queuedData.CustomCoordinate[1].IsNumber) position.y = queuedData.CustomCoordinate[1];
                }
                var localPosition = new Vector3(position.x, position.y, startTime * EditorScaleController.EditorScale);
                wallTransform.localPosition = localPosition;

                var newLocalScale = roundedHit - localPosition;
                newLocalScale = new Vector3(newLocalScale.x, Mathf.Max(newLocalScale.y, 0.01f), newLocalScale.z);
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
                roundedHit = new Vector3(
                    Mathf.Ceil(Math.Min(Math.Max(roundedHit.x, Bounds.min.x + 0.01f), Bounds.max.x)),
                    Mathf.Ceil(Math.Min(Math.Max(roundedHit.y, 0.01f), 3f)),
                    RoundedTime * EditorScaleController.EditorScale
                );

                wallTransform.localPosition = new Vector3(
                    originIndex - 2, queuedData.Type == (int)ObstacleType.Full ? -0.5f : 1.5f,
                    startTime * EditorScaleController.EditorScale);
                queuedData.Width = Mathf.CeilToInt(roundedHit.x + 2) - originIndex;

                instantiatedContainer.SetScale(new Vector3(queuedData.Width,
                    wallTransform.localScale.y, wallTransform.localScale.z));

                precisionPlacement.TogglePrecisionPlacement(false);
            }

            return;
        }

        if (UsePrecisionPlacement)
        {
            var precision = Atsc.GridMeasureSnapping;
            roundedHit.x = Mathf.Round(roundedHit.x * precision) / precision;
            roundedHit.y = Mathf.Round(roundedHit.y * precision) / precision;

            wallTransform.localPosition = roundedHit;
            instantiatedContainer.SetScale(Vector3.one / 2f);
            queuedData.PosX = queuedData.Type = 0;

            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();
            queuedData.CustomCoordinate = new Vector2(roundedHit.x, roundedHit.y);

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            var vanillaType = transformedPoint.y <= 2f ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;

            wallTransform.localPosition = new Vector3(
                wallTransform.localPosition.x - 0.5f,
                vanillaType == (int)ObstacleType.Full ? -0.5f : 1.5f,
                wallTransform.localPosition.z);

            instantiatedContainer.SetScale(new Vector3(1, vanillaType == (int)ObstacleType.Full ? 5f : 3f, 0));

            queuedData.CustomData = null;
            queuedData.PosX = Mathf.RoundToInt(wallTransform.localPosition.x + 2);
            queuedData.PosY = vanillaType == (int)ObstacleType.Full ? 0 : 2;
            queuedData.Height = vanillaType == (int)ObstacleType.Full ? 5 : 3;
            queuedData.Type = vanillaType;

            precisionPlacement.TogglePrecisionPlacement(false);
        }
    }

    public override void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        base.OnMousePositionUpdate(context);
        if (IsPlacing)
        {
            instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
                instantiatedContainer.transform.localPosition.y,
                startTime * EditorScaleController.EditorScale
            );
            instantiatedContainer.transform.localScale = new Vector3(instantiatedContainer.transform.localScale.x,
                instantiatedContainer.transform.localScale.y,
                (RoundedTime - startTime) * EditorScaleController.EditorScale);
        }
    }

    internal override void ApplyToMap()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            queuedData.Time = startTime;
            queuedData.Duration = instantiatedContainer.transform.localScale.z / EditorScaleController.EditorScale;
            if (queuedData.Duration < SmallestRankableWallDuration &&
                Settings.Instance.DontPlacePerfectZeroDurationWalls)
            {
                queuedData.Duration = SmallestRankableWallDuration;
            }

            objectContainerCollection.SpawnObject(queuedData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(queuedData, conflicting));
            queuedData = GenerateOriginalData();
            instantiatedContainer.ObstacleData = queuedData;
            obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f, 0);
        }
        else
        {
            IsPlacing = true;
            originIndex = queuedData.PosX;
            startTime = RoundedTime;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BaseObstacle dragged, BaseObstacle queued)
    {
        dragged.Time = queued.Time;
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
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f, 0);
        }
    }
}
