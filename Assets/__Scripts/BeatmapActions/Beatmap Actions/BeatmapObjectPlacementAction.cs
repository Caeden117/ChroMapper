using System.Collections.Generic;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> removedConflictObjects;

    public BeatmapObjectPlacementAction(IEnumerable<BeatmapObject> placedContainers,
        IEnumerable<BeatmapObject> conflictingObjects, string comment) : base(placedContainers, comment) {
        removedConflictObjects = conflictingObjects;
    }

    public BeatmapObjectPlacementAction(BeatmapObject placedObject,
       IEnumerable<BeatmapObject> conflictingObject, string comment) : base(new [] { placedObject }, comment)
    {
        removedConflictObjects = conflictingObject;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        RefreshPools(Data);
        foreach (BeatmapObject data in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType).SpawnObject(data, refreshesPool: false);
        }
        RefreshPools(removedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        RefreshPools(Data);
        foreach (BeatmapObject con in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(con.beatmapType)?.SpawnObject(con, refreshesPool: false);
        }
        RefreshPools(Data);
    }
}
