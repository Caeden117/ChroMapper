using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] List<Renderer> obstacleRenderer;
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
        RefreshRenderers();
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        foreach (var t in LoadedContainers)
        {
            if (t.objectData is BeatmapObstacle noteData)
            {
                noteData.id = id;
                t.gameObject.name = "Obstacle " + id;
                id++;
            }
        }
        UseChunkLoading = true;
    }

    private void RefreshRenderers()
    {
        obstacleRenderer = new List<Renderer>();
        foreach (BeatmapObjectContainer obj in LoadedContainers)
            obstacleRenderer.AddRange(obj.GetComponentsInChildren<Renderer>());
    }

    void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            foreach (Renderer g in obstacleRenderer)
                    g.materials.First().SetFloat("_OutsideAlpha", 0);
        }
        else
        {
            foreach (Renderer g in obstacleRenderer)
                g.materials.First().SetFloat("_OutsideAlpha", g.materials.First().GetFloat("_MainAlpha"));
        }
    }

    public void UpdateColor(Color obstacle)
    {
        obstacleAppearanceSO.defaultObstacleColor = obstacle;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        conflicting = null;
        if (removeConflicting)
        {
            conflicting = LoadedContainers.FirstOrDefault(x => x.objectData._time == obj._time &&
                ((BeatmapObstacle)obj)._lineIndex == ((BeatmapObstacle)x.objectData)._lineIndex &&
                ((BeatmapObstacle)obj)._type == ((BeatmapObstacle)x.objectData)._type &&
                ConflictingByTrackIDs(obj, x.objectData)
            );
            if (conflicting != null) DeleteObject(conflicting, true, $"Conflicted with a newer object at time {obj._time}");
        }
        BeatmapObstacleContainer beatmapObstacle = BeatmapObstacleContainer.SpawnObstacle(obj as BeatmapObstacle, AudioTimeSyncController, ref obstaclePrefab, ref obstacleAppearanceSO);
        beatmapObstacle.transform.SetParent(GridTransform);
        beatmapObstacle.UpdateGridPosition();
        LoadedContainers.Add(beatmapObstacle);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapObstacle;
    }
}
