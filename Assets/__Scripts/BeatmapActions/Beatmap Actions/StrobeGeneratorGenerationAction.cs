using System.Collections.Generic;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private readonly IEnumerable<BeatmapObject> conflictingData;

    public StrobeGeneratorGenerationAction(IEnumerable<BeatmapObject> generated, IEnumerable<BeatmapObject> conflicting)
        : base(generated) => conflictingData = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false, false);
        foreach (var obj in conflictingData)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).SpawnObject(obj, false, false);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in conflictingData)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false, false);
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).SpawnObject(obj, false, false);
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event).RefreshPool(true);
    }
}
