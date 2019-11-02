using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] Renderer[] obstacleRenderer;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.OBSTACLE;

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
        UseChunkLoading = !playing;
        foreach (BeatmapObjectContainer c in LoadedContainers) c.SafeSetActive(true);
        obstacleRenderer = GridTransform.GetComponentsInChildren<Renderer>();
        if (playing)
        {
            foreach (Renderer g in obstacleRenderer) g.materials.ToList().ForEach(m =>
            {
                if (m.GetFloat("_CircleRadius") != 6.27f) m.SetFloat("_CircleRadius", 6.27f);
            });
        }
        else
        {
            foreach (Renderer g in obstacleRenderer) g.materials.ToList().ForEach(m => {
                if (m.GetFloat("_CircleRadius") != 999) m.SetFloat("_CircleRadius", 999);
            });
        }
    }

    public void UpdateColor(Color obstacle)
    {
        obstacleAppearanceSO.defaultObstacleColor = obstacle;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting)
    {
        conflicting = LoadedContainers.FirstOrDefault(x => x.objectData._time == obj._time &&
            (obj as BeatmapObstacle)._lineIndex == (x.objectData as BeatmapObstacle)._lineIndex &&
            (obj as BeatmapObstacle)._type == (x.objectData as BeatmapObstacle)._type &&
            ConflictingByTrackIDs(obj, x.objectData)
        );
        if (conflicting != null) DeleteObject(conflicting);
        BeatmapObstacleContainer beatmapObstacle = BeatmapObstacleContainer.SpawnObstacle(obj as BeatmapObstacle, AudioTimeSyncController, ref obstaclePrefab, ref obstacleAppearanceSO);
        beatmapObstacle.transform.SetParent(GridTransform);
        beatmapObstacle.UpdateGridPosition();
        LoadedContainers.Add(beatmapObstacle);
        SelectionController.RefreshMap();
        return beatmapObstacle;
    }
}
