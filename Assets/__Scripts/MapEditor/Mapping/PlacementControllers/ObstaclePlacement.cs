using UnityEngine;
using System.Collections.Generic;

public class ObstaclePlacement : PlacementController<BeatmapObstacle, BeatmapObstacleContainer, ObstaclesContainer>
{
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    private bool isPlacing;
    private int originIndex;
    private float startTime;

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
        //TODO: Reposition wall to snap to half/full length (Holding alt = special case?)
        if (isPlacing)
        {
            instantiatedContainer.transform.localPosition = new Vector3(
                originIndex - 2, queuedData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f,
                startTime * EditorScaleController.EditorScale);
            queuedData._width = Mathf.CeilToInt(transformedPoint.x + 2) - originIndex;
            instantiatedContainer.transform.localScale = new Vector3(
                queuedData._width, instantiatedContainer.transform.localScale.y, instantiatedContainer.transform.localScale.z
                );
            return;
        }
        instantiatedContainer.transform.localPosition = new Vector3(
            Mathf.CeilToInt(transformedPoint.x) - 1,
            transformedPoint.y <= 1.5f ? 0 : 1.5f,
            instantiatedContainer.transform.localPosition.z);
        instantiatedContainer.transform.localScale = new Vector3(
            instantiatedContainer.transform.localScale.x,
            instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 2);
        queuedData._type = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.y);
    }

    public override void OnMousePositionUpdate(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        base.OnMousePositionUpdate(context);
        if (isPlacing) 
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
        if (isPlacing)
        {
            isPlacing = false;
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
            isPlacing = true;
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
        if (isPlacing)
        {
            isPlacing = false;
            queuedData = GenerateOriginalData();
            instantiatedContainer.obstacleData = queuedData;
            obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
        }
    }
}