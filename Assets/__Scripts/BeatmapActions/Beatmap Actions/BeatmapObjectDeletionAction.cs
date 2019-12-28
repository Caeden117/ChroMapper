using System.Collections.Generic;
using System.Linq;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction(IEnumerable<BeatmapObjectContainer> objs) : base(objs) { }

    public BeatmapObjectDeletionAction(BeatmapObjectContainer obj) : base(new List<BeatmapObjectContainer>() { obj }) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObjectContainer obj in containers)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(obj.objectData);
            param.collections.Where(x => x.ContainerType == copy.beatmapType).FirstOrDefault()?.SpawnObject(copy, out _);
            if (obj is BeatmapEventContainer e && e.eventData.IsRotationEvent) param.tracksManager.RefreshTracks();
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in containers) param.collections.ForEach(x => x.DeleteObject(obj));
    }
}
