public class BeatmapObstaclePlacementAction : BeatmapAction
{
    public BeatmapObstaclePlacementAction(BeatmapObstacleContainer obstacle) : base(obstacle) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.obstacles.DeleteObject(container);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = param.obstacles.SpawnObject(new BeatmapObstacle(data.ConvertToJSON()));
    }
}
