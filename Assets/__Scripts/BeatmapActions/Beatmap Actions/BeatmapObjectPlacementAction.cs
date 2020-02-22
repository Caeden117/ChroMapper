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
        foreach (BeatmapObjectContainer obj in containers)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false);
        }
        removedConflictObjects.Clear();
        foreach (BeatmapObject data in removedConflictObjectsData)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(data);
            BeatmapObjectContainer conflicting = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType)?.SpawnObject(copy);
            removedConflictObjects.Add(conflicting);
        }
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false);
        }
        containers.Clear();
        foreach (BeatmapObject con in data)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(con);
            BeatmapObjectContainer conflicting = BeatmapObjectContainerCollection.GetCollectionForType(con.beatmapType)?.SpawnObject(copy);
            containers.Add(conflicting);
        }
        param.tracksManager.RefreshTracks();
    }
}
