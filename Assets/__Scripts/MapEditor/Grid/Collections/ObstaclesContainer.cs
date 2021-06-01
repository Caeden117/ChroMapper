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
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    void OnPlayToggle(bool playing)
    {
        foreach (BeatmapObjectContainer obj in LoadedContainers.Values)
        {
            obj.MaterialPropertyBlock.SetFloat("_OutsideAlpha", playing ? 0 : 0.25f);
            obj.UpdateMaterials();
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

        con.MaterialPropertyBlock.SetFloat("_CircleRadius", EditorScaleController.EditorScale * 2);
        con.MaterialPropertyBlock.SetFloat("_OutsideAlpha", AudioTimeSyncController.IsPlaying ? 0 : 0.25f);
        obstacleAppearanceSO.SetObstacleAppearance(obstacle);
    }
}
