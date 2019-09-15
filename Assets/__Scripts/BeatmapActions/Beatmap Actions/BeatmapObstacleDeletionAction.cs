public class BeatmapObstacleDeletionAction : BeatmapAction
{
    public BeatmapObstacleDeletionAction(BeatmapObstacleContainer obstacle) : base(obstacle) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = param.obstacles.SpawnObject(new BeatmapObstacle(data.ConvertToJSON()));
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.obstacles.DeleteObject(container);
    }
}
