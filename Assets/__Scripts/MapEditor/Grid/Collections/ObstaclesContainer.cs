using UnityEngine;
using UnityEngine.Serialization;

public class ObstaclesContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject obstaclePrefab;
    [FormerlySerializedAs("obstacleAppearanceSO")] [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;

    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Obstacle;

    internal override void SubscribeToCallbacks()
    {
        Shader.SetGlobalFloat("_OutsideAlpha", 0.25f);
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks() => AudioTimeSyncController.PlayToggle -= OnPlayToggle;

    private void OnPlayToggle(bool playing) => Shader.SetGlobalFloat("_OutsideAlpha", playing ? 0 : 0.25f);

    public void UpdateColor(Color obstacle) => obstacleAppearanceSo.DefaultObstacleColor = obstacle;


    protected override void OnObjectSpawned(BeatmapObject _) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    protected override void OnObjectDelete(BeatmapObject _) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Obstacles);

    public override BeatmapObjectContainer CreateContainer() =>
        BeatmapObstacleContainer.SpawnObstacle(null, tracksManager, ref obstaclePrefab);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var obstacle = con as BeatmapObstacleContainer;
        if (!obstacle.IsRotatedByNoodleExtensions)
        {
            var track = tracksManager.GetTrackAtTime(obj.Time);
            track.AttachContainer(con);
        }

        obstacleAppearanceSo.SetObstacleAppearance(obstacle);
    }
}
