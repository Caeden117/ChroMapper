using System.Collections.Generic;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction(IEnumerable<BeatmapObject> objs, string comment) : base(objs, comment) { }

    public BeatmapObjectDeletionAction(BeatmapObject obj, string comment) : base(new [] { obj }, comment) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType)?.SpawnObject(obj, refreshesPool: false);
        }
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        RefreshPools(Data);
    }
}
