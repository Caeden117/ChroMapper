public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction(BeatmapObjectContainer obj) : base(obj) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        BeatmapObject copy = BeatmapObject.GenerateCopy(data);
        switch (data.beatmapType)
        {
            case BeatmapObject.Type.NOTE:
                container = param.notes.SpawnObject(copy);
                break;
            case BeatmapObject.Type.BOMB:
                container = param.notes.SpawnObject(copy);
                break;
            case BeatmapObject.Type.CUSTOM_NOTE:
                container = param.notes.SpawnObject(copy);
                break;
            case BeatmapObject.Type.OBSTACLE:
                container = param.obstacles.SpawnObject(copy);
                break;
            case BeatmapObject.Type.EVENT:
                container = param.events.SpawnObject(copy);
                break;
            case BeatmapObject.Type.CUSTOM_EVENT:
                container = param.events.SpawnObject(copy);
                break;
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        switch (data.beatmapType)
        {
            case BeatmapObject.Type.NOTE:
                param.notes.DeleteObject(container);
                break;
            case BeatmapObject.Type.BOMB:
                param.notes.DeleteObject(container);
                break;
            case BeatmapObject.Type.CUSTOM_NOTE:
                param.notes.DeleteObject(container);
                break;
            case BeatmapObject.Type.OBSTACLE:
                param.obstacles.DeleteObject(container);
                break;
            case BeatmapObject.Type.EVENT:
                param.events.DeleteObject(container);
                break;
            case BeatmapObject.Type.CUSTOM_EVENT:
                param.events.DeleteObject(container);
                break;
        }
    }
}
