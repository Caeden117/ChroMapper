public class BeatmapEventPlacementAction : BeatmapObjectPlacementAction
{
    private BeatmapEventContainer chromaEvent = null;
    private BeatmapObject chromaData;

    public BeatmapEventPlacementAction(BeatmapEventContainer placedEvent, BeatmapEventContainer chroma,
        BeatmapObjectContainerCollection collection, BeatmapObjectContainer conflicting = null) : base(placedEvent, collection, conflicting) {
        chromaEvent = chroma;
        if (chroma != null) chromaData = chromaEvent.eventData;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(container);
        if (chromaEvent != null) collection.DeleteObject(chromaEvent);
        if (removedConflictObject != null)
            removedConflictObject = collection.SpawnObject(BeatmapObject.GenerateCopy(removedConflictObject.objectData), out _);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = collection.SpawnObject(BeatmapObject.GenerateCopy(data), out _);
        if (chromaEvent != null) collection.SpawnObject(BeatmapObject.GenerateCopy(chromaData), out _);
        if (removedConflictObject != null) collection.DeleteObject(removedConflictObject);
    }
}
