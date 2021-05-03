using System.Collections.Generic;
using System.Linq;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> conflictingData;

    public StrobeGeneratorGenerationAction(IEnumerable<BeatmapObject> generated, IEnumerable<BeatmapObject> conflicting) : base(generated)
    {
        conflictingData = conflicting;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        foreach (BeatmapObject obj in conflictingData)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).SpawnObject(obj, false, false);
        }
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in conflictingData)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).SpawnObject(obj, false, false);
        }
        BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT).RefreshPool(true);
    }
}
