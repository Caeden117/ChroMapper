using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private readonly IEnumerable<BaseObject> conflictingData;

    public StrobeGeneratorGenerationAction(IEnumerable<BaseObject> generated, IEnumerable<BaseObject> conflicting)
        : base(generated) => conflictingData = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        foreach (var obj in conflictingData)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, false, false);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in conflictingData)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, false, false);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
    }
}
