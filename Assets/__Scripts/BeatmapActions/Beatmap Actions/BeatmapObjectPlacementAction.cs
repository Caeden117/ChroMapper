using System.Collections.Generic;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    private readonly IEnumerable<BeatmapObject> removedConflictObjects;

    public BeatmapObjectPlacementAction(IEnumerable<BeatmapObject> placedContainers,
        IEnumerable<BeatmapObject> conflictingObjects, string comment) : base(placedContainers, comment) =>
        removedConflictObjects = conflictingObjects;

    public BeatmapObjectPlacementAction(BeatmapObject placedObject,
        IEnumerable<BeatmapObject> conflictingObject, string comment) : base(new[] { placedObject }, comment) =>
        removedConflictObjects = conflictingObject;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);

        foreach (var data in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType).SpawnObject(data, refreshesPool: false);
        }

        RefreshPools(removedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);

        foreach (var con in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(con.BeatmapType).SpawnObject(con, refreshesPool: false);
        }

        RefreshPools(Data);
    }
}
