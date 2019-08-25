using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : MonoBehaviour
{

    [SerializeField] AudioTimeSyncController audioTimeSyncController;

    //An easy way to edit notes
    [SerializeField] public List<BeatmapObjectContainer> loadedObstacles = new List<BeatmapObjectContainer>();

    [SerializeField] Renderer[] obstacleRenderer;
    [SerializeField] Transform obstacleGrid;

    [SerializeField] BeatmapObjectCallbackController spawnCallbackController;
    [SerializeField] BeatmapObjectCallbackController despawnCallbackController;

    private void OnEnable()
    {
        audioTimeSyncController.OnPlayToggle += OnPlayToggle;
        foreach(Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 999);
        BeatmapObjectContainer.FlaggedForDeletionEvent += DeleteObstacle;
    }

    private void DeleteObstacle(BeatmapObjectContainer obj)
    {
        if (loadedObstacles.Contains(obj))
        {
            loadedObstacles.Remove(obj);
            Destroy(obj.gameObject);
            SelectionController.RefreshMap();
        }
    }

    private void OnDisable()
    {
        audioTimeSyncController.OnPlayToggle -= OnPlayToggle;
        BeatmapObjectContainer.FlaggedForDeletionEvent -= DeleteObstacle;
    }

    public void SortObstacles()
    {
        obstacleRenderer = obstacleGrid.GetComponentsInChildren<Renderer>();
        loadedObstacles = loadedObstacles.OrderBy(x => x.objectData._time).ToList();
        uint id = 0;
        for (int i = 0; i < loadedObstacles.Count; i++)
        {
            if (loadedObstacles[i].objectData is BeatmapObstacle)
            {
                BeatmapObstacle noteData = (BeatmapObstacle)loadedObstacles[i].objectData;
                noteData.id = id;
                loadedObstacles[i].gameObject.name = "Obstacle " + id;
                id++;
            }
        }
    }

    void OnPlayToggle(bool playing)
    {
        obstacleRenderer = obstacleGrid.GetComponentsInChildren<Renderer>();
        if (playing)
            foreach (Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 6.27f);
        else
            foreach (Renderer g in obstacleRenderer) g.material.SetFloat("_CircleRadius", 999);
    }

}
