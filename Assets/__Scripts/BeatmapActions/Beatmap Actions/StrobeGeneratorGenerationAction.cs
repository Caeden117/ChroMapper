using System.Collections.Generic;
using Beatmap.Enums;
using Beatmap.Base;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private readonly IEnumerable<IObject> conflictingData;

    public StrobeGeneratorGenerationAction(IEnumerable<IObject> generated, IEnumerable<IObject> conflicting)
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
