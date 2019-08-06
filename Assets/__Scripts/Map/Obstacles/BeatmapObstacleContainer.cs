using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapObstacleContainer : BeatmapObjectContainer {

	public override BeatmapObject objectData
    {
        get
        {
            return obstacleData;
        }
    }

    public BeatmapObstacle obstacleData;

    public static BeatmapObstacleContainer SpawnObstacle(BeatmapObstacle data, ref GameObject prefab)
    {
        BeatmapObstacleContainer container = Instantiate(prefab).GetComponent<BeatmapObstacleContainer>();
        container.obstacleData = data;
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            obstacleData._lineIndex - 2f,
            obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 0 : 1.5f,
            obstacleData._time * EditorScaleController.EditorScale
            );
        transform.localScale = new Vector3(
            obstacleData._width,
            obstacleData._type == BeatmapObstacle.VALUE_FULL_BARRIER ? 3.5f : 2,
            obstacleData._duration * EditorScaleController.EditorScale
            );
    }
}
