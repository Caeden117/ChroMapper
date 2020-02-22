using System.Collections.Generic;
using System.Linq;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction(IEnumerable<BeatmapObjectContainer> objs, string comment) : base(objs, comment) { }

    public BeatmapObjectDeletionAction(BeatmapObjectContainer obj, string comment) : base(new [] { obj }, comment) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObjectContainer obj in containers)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(obj.objectData);
            BeatmapObjectContainerCollection.GetCollectionForType(copy.beatmapType)?.SpawnObject(copy, out _);
        }
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in containers)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false, Comment);
        param.tracksManager.RefreshTracks();
    }
}
