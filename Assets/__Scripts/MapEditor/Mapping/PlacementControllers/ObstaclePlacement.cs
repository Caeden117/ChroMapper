using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstaclePlacement : PlacementController<BeatmapObstacle, BeatmapObstacleContainer, ObstaclesContainer>
{
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    private bool isPlacing = false;
    private int originIndex = 0;
    private float startTime = 0;
    private float newTime = 0;

    public override BeatmapAction GenerateAction(BeatmapObstacleContainer spawned)
    {
        return new BeatmapObstaclePlacementAction(spawned);
    }

    public override BeatmapObstacle GenerateOriginalData()
    {
        return new BeatmapObstacle(0, 0, BeatmapObstacle.VALUE_FULL_BARRIER, 0, 1);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        instantiatedContainer.obstacleData = queuedData;
        instantiatedContainer.obstacleData._duration = (newTime - startTime);
        obstacleAppearanceSO.SetObstacleAppearance(instantiatedContainer);
        //TODO: Reposition wall to snap to half/full length (Holding alt = special case?)
        if (isPlacing)
        {
            instantiatedContainer.transform.position = new Vector3(
                originIndex - 2, queuedData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f,
                instantiatedContainer.transform.position.z);
            queuedData._width = Mathf.CeilToInt(Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                                    Mathf.Ceil(hit.collider.bounds.min.x),
                                    Mathf.Floor(hit.collider.bounds.max.x)
                                ) + 2) - originIndex;
            instantiatedContainer.transform.localScale = new Vector3(
                queuedData._width, instantiatedContainer.transform.localScale.y, instantiatedContainer.transform.localScale.z
                );
            float roundedToPrecision = Mathf.Round((hit.point.z / EditorScaleController.EditorScale) /
                 (1 / (float)atsc.gridMeasureSnapping)) * (1 / (float)atsc.gridMeasureSnapping)
                 * EditorScaleController.EditorScale;
            newTime = (roundedToPrecision / EditorScaleController.EditorScale) + atsc.CurrentBeat;
            return;
        }
        instantiatedContainer.transform.position = new Vector3(
            Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                Mathf.Ceil(hit.collider.bounds.min.x),
                Mathf.Floor(hit.collider.bounds.max.x)
            ) - 1f,
            hit.point.y <= 1.5f ? 0 : 1.5f,
            instantiatedContainer.transform.position.z);
        instantiatedContainer.transform.localScale = new Vector3(
            instantiatedContainer.transform.localScale.x,
            instantiatedContainer.transform.position.y == 0 ? 3.5f : 2, 0);
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.position.x + 2);
        queuedData._type = Mathf.FloorToInt(instantiatedContainer.transform.position.y);
        //TODO: find a way to click to start wall placement, not straight up add it.
    }

    private void Update()
    {
        if (isPlacing) 
        {
            if (Input.GetMouseButtonDown(1)) //Cancel wall placement with a right click.
            {
                isPlacing = false;
                return;
            }
            instantiatedContainer.transform.position = new Vector3(instantiatedContainer.transform.position.x,
                instantiatedContainer.transform.position.y,
                (startTime - atsc.CurrentBeat) * EditorScaleController.EditorScale
                );
            instantiatedContainer.transform.localScale = new Vector3(instantiatedContainer.transform.localScale.x,
                instantiatedContainer.transform.localScale.y, (newTime - startTime) * EditorScaleController.EditorScale);
        }
    }

    internal override void ApplyToMap()
    {
        if (isPlacing)
        {
            isPlacing = false;
            queuedData._time = startTime;
            queuedData._duration = instantiatedContainer.transform.localScale.z / EditorScaleController.EditorScale;
            BeatmapObstacleContainer spawned = objectContainerCollection.SpawnObject(queuedData) as BeatmapObstacleContainer;
            BeatmapActionContainer.AddAction(GenerateAction(spawned));
        }
        else
        {
            isPlacing = true;
            originIndex = queuedData._lineIndex;
            startTime = (instantiatedContainer.transform.position.z / EditorScaleController.EditorScale)
            + atsc.CurrentBeat;
        }
    }
}