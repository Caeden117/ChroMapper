using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSO;
    [SerializeField] private TracksManager tracksManager;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.OBSTACLE;

    internal override void SubscribeToCallbacks()
    {
        Shader.SetGlobalFloat("_OutsideAlpha", 0.25f);
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    private void OnPlayToggle(bool playing)
    {
        Shader.SetGlobalFloat("_OutsideAlpha", playing ? 0 : 0.25f);
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
        obstacleAppearanceSO.SetObstacleAppearance(obstacle);
    }
}
