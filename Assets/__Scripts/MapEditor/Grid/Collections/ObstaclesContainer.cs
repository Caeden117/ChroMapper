using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] Renderer[] obstacleRenderer;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;

    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
        foreach(Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 999);
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    public override void SortObjects()
    {
        obstacleRenderer = GridTransform.GetComponentsInChildren<Renderer>();
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        for (int i = 0; i < LoadedContainers.Count; i++)
        {
            if (LoadedContainers[i].objectData is BeatmapObstacle)
            {
                BeatmapObstacle noteData = (BeatmapObstacle)LoadedContainers[i].objectData;
                noteData.id = id;
                LoadedContainers[i].gameObject.name = "Obstacle " + id;
                id++;
            }
        }
        UseChunkLoading = true;
    }

    void OnPlayToggle(bool playing)
    {
        obstacleRenderer = GridTransform.GetComponentsInChildren<Renderer>();
        if (playing)
            foreach (Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 6.27f);
        else
            foreach (Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 999);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj)
    {
        BeatmapObstacleContainer beatmapObstacle = BeatmapObstacleContainer.SpawnObstacle(obj as BeatmapObstacle, ref obstaclePrefab, ref obstacleAppearanceSO);
        beatmapObstacle.transform.SetParent(GridTransform);
        beatmapObstacle.UpdateGridPosition();
        LoadedContainers.Add(beatmapObstacle);
        return beatmapObstacle;
    }
}
