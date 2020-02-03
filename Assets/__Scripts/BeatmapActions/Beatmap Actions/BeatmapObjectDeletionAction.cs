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
            param.collections.FirstOrDefault(x => x.ContainerType == copy.beatmapType)?.SpawnObject(copy, out _);
            if (obj is BeatmapEventContainer e && e.eventData.IsRotationEvent) param.tracksManager.RefreshTracks();
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in containers)
            param.collections.ForEach(x => x.DeleteObject(obj, false, Comment));
    }
}
