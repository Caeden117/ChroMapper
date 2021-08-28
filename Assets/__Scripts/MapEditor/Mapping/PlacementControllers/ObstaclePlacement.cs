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

    public override BeatmapObstacle GenerateOriginalData() =>
        new BeatmapObstacle(0, 0, BeatmapObstacle.ValueFullBarrier, 0, 1);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        Bounds = default;
        TestForType<ObstaclePlacement>(hit, BeatmapObject.ObjectType.Obstacle);

        InstantiatedContainer.ObstacleData = QueuedData;
        InstantiatedContainer.ObstacleData.Duration = RoundedTime - startTime;
        obstacleAppearanceSo.SetObstacleAppearance(InstantiatedContainer);
        var roundedHit = ParentTrack.InverseTransformPoint(hit.Point);

        // Check if ChromaToggle notes button is active and apply _color
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

        if (IsPlacing)
        {
            if (UsePrecisionPlacement)
            {
                roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);

                Vector2 position = QueuedData.CustomData["_position"];
                var localPosition = new Vector3(position.x, position.y, startTime * EditorScaleController.EditorScale);
                InstantiatedContainer.transform.localPosition = localPosition;
                var newLocalScale = roundedHit - localPosition;
                newLocalScale = new Vector3(newLocalScale.x, Mathf.Max(newLocalScale.y, 0.01f), newLocalScale.z);
                InstantiatedContainer.transform.localScale = newLocalScale;

                var scale = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
                scale[0] = Math.Round(newLocalScale.x, 3);
                scale[1] = Math.Round(newLocalScale.y, 3);
                QueuedData.CustomData["_scale"] = scale;

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

                InstantiatedContainer.transform.localPosition = new Vector3(
                    originIndex - 2, QueuedData.Type == BeatmapObstacle.ValueFullBarrier ? 0 : 1.5f,
                    startTime * EditorScaleController.EditorScale);
                QueuedData.Width = Mathf.CeilToInt(roundedHit.x + 2) - originIndex;
                InstantiatedContainer.transform.localScale = new Vector3(
                    QueuedData.Width, InstantiatedContainer.transform.localScale.y,
                    InstantiatedContainer.transform.localScale.z
                );
                precisionPlacement.TogglePrecisionPlacement(false);
            }

            return;
        }

        if (UsePrecisionPlacement)
        {
            InstantiatedContainer.transform.localPosition = roundedHit;
            InstantiatedContainer.transform.localScale = Vector3.one / 2f;
            QueuedData.LineIndex = QueuedData.Type = 0;

            if (QueuedData.CustomData == null) QueuedData.CustomData = new JSONObject();

            var position = new JSONArray(); //We do some manual array stuff to get rounding decimals to work.
            position[0] = Math.Round(roundedHit.x, 3);
            position[1] = Math.Round(roundedHit.y, 3);
            QueuedData.CustomData["_position"] = position;

            precisionPlacement.TogglePrecisionPlacement(true);
            precisionPlacement.UpdateMousePosition(hit.Point);
        }
        else
        {
            var vanillaType = transformedPoint.y <= 1.5f ? 0 : 1;

            InstantiatedContainer.transform.localPosition = new Vector3(
                InstantiatedContainer.transform.localPosition.x - 0.5f,
                vanillaType * 1.5f,
                InstantiatedContainer.transform.localPosition.z);

            InstantiatedContainer.transform.localScale = new Vector3(
                1,
                InstantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);

            QueuedData.CustomData = null;
            QueuedData.LineIndex = Mathf.RoundToInt(InstantiatedContainer.transform.localPosition.x + 2);
            QueuedData.Type = vanillaType;

            precisionPlacement.TogglePrecisionPlacement(false);
        }
    }

    public override void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        base.OnMousePositionUpdate(context);
        if (IsPlacing)
        {
            InstantiatedContainer.transform.localPosition = new Vector3(InstantiatedContainer.transform.localPosition.x,
                InstantiatedContainer.transform.localPosition.y,
                startTime * EditorScaleController.EditorScale
            );
            InstantiatedContainer.transform.localScale = new Vector3(InstantiatedContainer.transform.localScale.x,
                InstantiatedContainer.transform.localScale.y,
                (RoundedTime - startTime) * EditorScaleController.EditorScale);
        }
    }

    internal override void ApplyToMap()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            QueuedData.Time = startTime;
            QueuedData.Duration = InstantiatedContainer.transform.localScale.z / EditorScaleController.EditorScale;
            if (QueuedData.Duration < SmallestRankableWallDuration &&
                Settings.Instance.DontPlacePerfectZeroDurationWalls)
            {
                QueuedData.Duration = SmallestRankableWallDuration;
            }

            ObjectContainerCollection.SpawnObject(QueuedData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(QueuedData, conflicting));
            QueuedData = GenerateOriginalData();
            InstantiatedContainer.ObstacleData = QueuedData;
            obstacleAppearanceSo.SetObstacleAppearance(InstantiatedContainer);
            InstantiatedContainer.transform.localScale = new Vector3(
                1, InstantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        }
        else
        {
            IsPlacing = true;
            originIndex = QueuedData.LineIndex;
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
            QueuedData = GenerateOriginalData();
            InstantiatedContainer.ObstacleData = QueuedData;
            obstacleAppearanceSo.SetObstacleAppearance(InstantiatedContainer);
            InstantiatedContainer.transform.localScale = new Vector3(
                1, InstantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        }
    }
}
