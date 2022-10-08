using System.Collections.Generic;
using Beatmap.Base;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    private readonly IEnumerable<IObject> removedConflictObjects;

    public BeatmapObjectPlacementAction(IEnumerable<IObject> placedContainers,
        IEnumerable<IObject> conflictingObjects, string comment) : base(placedContainers, comment) =>
        removedConflictObjects = conflictingObjects;

    public BeatmapObjectPlacementAction(IObject placedObject,
        IEnumerable<IObject> conflictingObject, string comment) : base(new[] { placedObject }, comment) =>
        removedConflictObjects = conflictingObject;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);

        foreach (var data in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType).SpawnObject(data, refreshesPool: false);
        }

        RefreshPools(removedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in removedConflictObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);

        foreach (var con in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(con.ObjectType).SpawnObject(con, refreshesPool: false);
        }

        RefreshPools(Data);
    }
}
