using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    private HashSet<Renderer> obstacleRenderer = new HashSet<Renderer>();
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    [SerializeField] private TracksManager tracksManager;

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
        UseChunkLoading = true;
    }

    void OnPlayToggle(bool playing)
    {
        foreach (BeatmapObjectContainer obj in LoadedContainers.Values)
        {
            foreach (Material mat in obj.ModelMaterials)
            {
                if (!mat.HasProperty("_OutsideAlpha")) continue;
                if (playing)
                {
                    mat.SetFloat("_OutsideAlpha", 0);
                }
                else
                {
                    mat.SetFloat("_OutsideAlpha", mat.GetFloat("_MainAlpha"));
                }
            }
        }
    }

    public void UpdateColor(Color obstacle)
    {
        obstacleAppearanceSO.defaultObstacleColor = obstacle;
    }

    protected override bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b)
    {
        BeatmapObstacle obstacleA = a as BeatmapObstacle;
        BeatmapObstacle obstacleB = b as BeatmapObstacle;
        if (obstacleA.IsNoodleExtensionsWall || obstacleB.IsNoodleExtensionsWall) return false;
        return obstacleA._lineIndex == obstacleB._lineIndex && obstacleA._type == obstacleB._type;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        BeatmapObstacleContainer obstacle = con as BeatmapObstacleContainer;
        if (!obstacle.IsRotatedByNoodleExtensions)
        {
            Track track = tracksManager.GetTrackAtTime(obj._time);
            track.AttachContainer(con);
        }
        foreach (Material mat in obstacle.ModelMaterials)
        {
            if (!mat.HasProperty("_OutsideAlpha")) continue;
            if (AudioTimeSyncController.IsPlaying)
            {
                mat.SetFloat("_OutsideAlpha", 0);
            }
            else
            {
                mat.SetFloat("_OutsideAlpha", mat.GetFloat("_MainAlpha"));
            }
        }
        obstacleAppearanceSO.SetObstacleAppearance(obstacle);
    }
}
