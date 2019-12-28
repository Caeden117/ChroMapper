using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstaclePlacement : PlacementController<BeatmapObstacle, BeatmapObstacleContainer, ObstaclesContainer>
{
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    private bool isPlacing = false;
    private int originIndex = 0;
    private float startTime = 0;

    public override BeatmapAction GenerateAction(BeatmapObstacleContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, container);
    }

    public override BeatmapObstacle GenerateOriginalData()
    {
        return new BeatmapObstacle(0, 0, BeatmapObstacle.VALUE_FULL_BARRIER, 0, 1);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        instantiatedContainer.obstacleData = queuedData;
        instantiatedContainer.obstacleData._duration = RoundedTime - startTime;
        obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
        CalculateTimes(hit, out Vector3 transformedPoint, out _, out _, out _);
        //TODO: Reposition wall to snap to half/full length (Holding alt = special case?)
        if (isPlacing)
        {
            instantiatedContainer.transform.localPosition = new Vector3(
                originIndex - 2, queuedData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f,
                instantiatedContainer.transform.localPosition.z);
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
        //TODO: find a way to click to start wall placement, not straight up add it.
    }

    internal override void Update()
    {
        base.Update();
        if (isPlacing) 
        {
            if (Input.GetMouseButtonDown(1)) //Cancel wall placement with a right click.
            {
                isPlacing = false;
                queuedData = GenerateOriginalData();
                instantiatedContainer.obstacleData = queuedData;
                obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
                instantiatedContainer.transform.localScale = new Vector3(
                    1, instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
                return;
            }
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
            BeatmapObstacleContainer spawned = objectContainerCollection.SpawnObject(queuedData, out BeatmapObjectContainer conflicting) as BeatmapObstacleContainer;
            BeatmapActionContainer.AddAction(GenerateAction(spawned, conflicting));
            SelectionController.RefreshMap();
            queuedData = GenerateOriginalData();
            instantiatedContainer.obstacleData = queuedData;
            obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
            instantiatedContainer.transform.localScale = new Vector3(
                1, instantiatedContainer.transform.localPosition.y == 0 ? 3.5f : 2, 0);
            if (AssignTo360Tracks)
            {
                Vector3 localRotation = spawned.transform.localEulerAngles;
                Track track = tracksManager.GetTrackForRotationValue(gridRotation.Rotation);
                track?.AttachContainer(spawned, gridRotation.Rotation);
                spawned.UpdateGridPosition();
                spawned.transform.localEulerAngles = localRotation;
            }
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
}