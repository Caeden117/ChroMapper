public class BeatmapEventDeletionAction : BeatmapAction
{
    private BeatmapEventContainer chromaEvent = null;
    private BeatmapObject chromaData;

    public BeatmapEventDeletionAction(BeatmapEventContainer placedEvent, BeatmapEventContainer chroma) : base(placedEvent)
    {
        chromaEvent = chroma;
        if (chroma != null) chromaData = chromaEvent.eventData;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = param.events.SpawnObject(BeatmapObject.GenerateCopy(data), out _);
        if (chromaEvent != null) param.events.SpawnObject(BeatmapObject.GenerateCopy(chromaData), out _);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.events.DeleteObject(container);
        if (chromaEvent != null) param.events.DeleteObject(chromaEvent);
    }
}
