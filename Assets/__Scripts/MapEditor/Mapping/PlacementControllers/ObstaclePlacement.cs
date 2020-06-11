using UnityEngine;
using System.Collections.Generic;

public class ObstaclePlacement : PlacementController<BeatmapObstacle, BeatmapObstacleContainer, ObstaclesContainer>
{
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    public static bool IsPlacing { get; private set; } = false;

    private int originIndex;
    private float startTime;

    public override bool IsValid => base.IsValid || KeybindsController.ShiftHeld;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Place a Wall.");
    }

    public override BeatmapObstacle GenerateOriginalData()
    {
        return new BeatmapObstacle(0, 0, BeatmapObstacle.VALUE_FULL_BARRIER, 0, 1);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.obstacleData = queuedData;
        instantiatedContainer.obstacleData._duration = RoundedTime - startTime;
        obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
        Vector3 roundedHit = parentTrack.InverseTransformPoint(hit.point);
        roundedHit = new Vector3(roundedHit.x, roundedHit.y, RoundedTime * EditorScaleController.EditorScale);
        //TODO: Reposition wall to snap to half/full length (Holding alt = special case?)
        if (IsPlacing)
        {
            if (KeybindsController.ShiftHeld)
            {
                Vector2 position = queuedData._customData["_position"];
                Vector3 localPosition = new Vector3(position.x, position.y, startTime * EditorScaleController.EditorScale);
                instantiatedContainer.transform.localPosition = localPosition;
                Vector3 newLocalScale = roundedHit - localPosition;
                newLocalScale = new Vector3(newLocalScale.x, Mathf.Max(newLocalScale.y, 0.01f), newLocalScale.z);
                instantiatedContainer.transform.localScale = newLocalScale;
                queuedData._customData["_scale"] = (Vector2)newLocalScale;
            }
            else
            {
                instantiatedContainer.transform.localPosition = new Vector3(
                    originIndex - 2, queuedData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f,
                    startTime * EditorScaleController.EditorScale);
                queuedData._width = Mathf.CeilToInt(transformedPoint.x + 2) - originIndex;
                instantiatedContainer.transform.localScale = new Vector3(
                    queuedData._width, instantiatedContainer.transform.localScale.y, instantiatedContainer.transform.localScale.z
                    );
            }
            return;
        }
        if (KeybindsController.ShiftHeld)
        {
            instantiatedContainer.transform.localPosition = roundedHit;
            instantiatedContainer.transform.localScale = Vector3.one / 2f;
            queuedData._lineIndex = queuedData._type = 0;
            if (queuedData._customData == null) queuedData._customData = new SimpleJSON.JSONObject();
            queuedData._customData["_position"] = (Vector2)roundedHit;
        }
        else
        {
            instantiatedContainer.transform.localPosition = new Vector3(
                Mathf.CeilToInt(transformedPoint.x) - 1,
                transformedPoint.y <= 1.5f ? 0 : 1.5f,
                instantiatedContainer.transform.localPosition.z);
            instantiatedContainer.transform.localScale = new Vector3(
                1,
                instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
            queuedData._customData = null;
            queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 2);
            queuedData._type = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.y);
        }
    }

    public override void OnControllerInactive()
    {
        IsPlacing = false;
    }

    public override void OnMousePositionUpdate(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        base.OnMousePositionUpdate(context);
        if (IsPlacing) 
        {
            instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
                instantiatedContainer.transform.localPosition.y,
                startTime * EditorScaleController.EditorScale
                );
            instantiatedContainer.transform.localScale = new Vector3(instantiatedContainer.transform.localScale.x,
                instantiatedContainer.transform.localScale.y, (RoundedTime - startTime) * EditorScaleController.EditorScale);
        }
    }

    internal override void ApplyToMap()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            queuedData._time = startTime;
            queuedData._duration = instantiatedContainer.transform.localScale.z / EditorScaleController.EditorScale;
            if (queuedData._duration == 0 && Settings.Instance.DontPlacePerfectZeroDurationWalls)
                queuedData._duration = 0.01f;
            objectContainerCollection.SpawnObject(queuedData, out IEnumerable<BeatmapObject> conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(queuedData, conflicting));
            queuedData = GenerateOriginalData();
            instantiatedContainer.obstacleData = queuedData;
            obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        }
        else
        {
            IsPlacing = true;
            originIndex = queuedData._lineIndex;
            startTime = RoundedTime;
        }
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapObstacle dragged, BeatmapObstacle queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
    }

    public override void CancelPlacement()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            queuedData = GenerateOriginalData();
            instantiatedContainer.obstacleData = queuedData;
            obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        }
    }
}