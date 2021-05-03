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
            mat.SetFloat("_CircleRadius", EditorScaleController.EditorScale * 2);
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
