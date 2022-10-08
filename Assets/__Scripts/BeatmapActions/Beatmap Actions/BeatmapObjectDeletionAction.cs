using System.Collections.Generic;
using Beatmap.Base;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction(IEnumerable<BaseObject> objs, string comment) : base(objs, comment) { }

    public BeatmapObjectDeletionAction(BaseObject obj, string comment) : base(new[] { obj }, comment) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, refreshesPool: false);
        }

        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);
    }
}
