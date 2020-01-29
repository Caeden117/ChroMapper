using System.Collections.Generic;
using System.Linq;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    internal List<BeatmapObjectContainer> removedConflictObjects = new List<BeatmapObjectContainer>();
    internal List<BeatmapObject> removedConflictObjectsData = new List<BeatmapObject>();

    public BeatmapObjectPlacementAction(IEnumerable<BeatmapObjectContainer> conflictingObjects, 
        IEnumerable<BeatmapObjectContainer> placedContainers, string comment) : base(placedContainers, comment) {
        foreach (BeatmapObjectContainer con in conflictingObjects)
        {
            if (con is null) continue;
            removedConflictObjects.Add(con);
            removedConflictObjectsData.Add(con.objectData);
        }
    }

    public BeatmapObjectPlacementAction(BeatmapObjectContainer placedContainer,
       BeatmapObjectContainer conflictingObject, string comment) : base(new [] { placedContainer }, comment)
    {
        removedConflictObjects.Add(conflictingObject);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in containers) param.collections.ForEach(x => x.DeleteObject(obj, false));
        removedConflictObjects.Clear();
        foreach (BeatmapObject data in removedConflictObjectsData)
            removedConflictObjects.Add(param.collections.FirstOrDefault(x => x.ContainerType == data.beatmapType)?.SpawnObject(
                BeatmapObject.GenerateCopy(data), out _));
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in removedConflictObjects) param.collections.ForEach(x => x.DeleteObject(obj, false));
        containers.Clear();
        foreach (BeatmapObject con in data)
            containers.Add(param.collections.FirstOrDefault(x => x.ContainerType == con.beatmapType)?.SpawnObject(
                BeatmapObject.GenerateCopy(con), out _));
        param.tracksManager.RefreshTracks();
    }
}
