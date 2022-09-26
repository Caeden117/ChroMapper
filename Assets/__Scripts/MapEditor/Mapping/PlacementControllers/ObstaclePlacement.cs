using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ObstaclePlacement : PlacementController<BeatmapObstacle, BeatmapObstacleContainer, ObstaclesContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [FormerlySerializedAs("obstacleAppearanceSO")] [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
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

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Place a Wall.");

    public override BeatmapObstacle GenerateOriginalData()
    {
        if (Settings.Instance.Load_MapV3)
        {
            return new BeatmapObstacleV3(new BeatmapObstacle(0, 0, BeatmapObstacle.ValueFullBarrier, 0, 1));
        }
        else
            return new BeatmapObstacle(0, 0, BeatmapObstacle.ValueFullBarrier, 0, 1);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        Bounds = default;
        TestForType<ObstaclePlacement>(hit, BeatmapObject.ObjectType.Obstacle);

        instantiatedContainer.ObstacleData = queuedData;
        instantiatedContainer.ObstacleData.Duration = RoundedTime - startTime;
        obstacleAppearanceSo.SetObstacleAppearance(instantiatedContainer);
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);

        // Check if ChromaToggle notes button is active and apply _color
        if (CanPlaceChromaObjects && dropdown.Visible)
        {
            // Doing the same a Chroma 2.0 events but with notes insted
            queuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            // If not remove _color
            if (queuedData.CustomData != null && queuedData.CustomData.HasKey("_color"))
            {
                queuedData.CustomData.Remove("_color");

                if (queuedData.CustomData.Count <= 0) //Set customData to null if there is no customData to store
                    queuedData.CustomData = null;
            }
        }

        var wallTransform = instantiatedContainer.transform;

        if (IsPlacing)
        {
            if (UsePrecisionPlacement)
            {
                roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

                Vector2 position = queuedData.CustomPosition;
                var localPosition = new Vector3(position.x, position.y, startTime * EditorScaleController.EditorScale);
                wallTransform.localPosition = localPosition;

                var newLocalScale = roundedHit - localPosition;
                newLocalScale = new Vector3(newLocalScale.x, Mathf.Max(newLocalScale.y, 0.01f), newLocalScale.z);
                instantiatedContainer.SetScale(newLocalScale);

                var scale = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
                scale[0] = Math.Round(newLocalScale.x, 3);
                scale[1] = Math.Round(newLocalScale.y, 3);
                queuedData.CustomSize = scale;

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
                    originIndex - 2, queuedData.Type == BeatmapObstacle.ValueFullBarrier ? 0 : 1.5f,
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
            wallTransform.localPosition = roundedHit;
            instantiatedContainer.SetScale(Vector3.one / 2f);
            queuedData.LineIndex = queuedData.Type = 0;

            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x, 3);
            position[1] = Math.Round(roundedHit.y, 3);
            queuedData.CustomPosition = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            var vanillaType = transformedPoint.y <= 1.5f ? 0 : 1;

            wallTransform.localPosition = new Vector3(
                wallTransform.localPosition.x - 0.5f,
                vanillaType * 1.5f,
                wallTransform.localPosition.z);

            instantiatedContainer.SetScale(new Vector3(1, wallTransform.localPosition.y == 0 ? 3.75f : 2.25f, 0));

            queuedData.CustomData = null;
            queuedData.LineIndex = Mathf.RoundToInt(wallTransform.localPosition.x + 2);
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
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.75f : 2.25f, 0);
        }
        else
        {
            IsPlacing = true;
            originIndex = queuedData.LineIndex;
            startTime = RoundedTime;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapObstacle dragged, BeatmapObstacle queued)
    {
        dragged.Time = queued.Time;
        dragged.LineIndex = queued.LineIndex;
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
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.75f : 2.25f, 0);
        }
    }
}
