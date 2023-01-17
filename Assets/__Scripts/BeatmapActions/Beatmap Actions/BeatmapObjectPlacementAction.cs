using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    private IEnumerable<BaseObject> removedConflictObjects;

    public BeatmapObjectPlacementAction() : base() { }

    public BeatmapObjectPlacementAction(IEnumerable<BaseObject> placedContainers,
        IEnumerable<BaseObject> conflictingObjects, string comment) : base(placedContainers, comment) =>
        removedConflictObjects = conflictingObjects;

    public BeatmapObjectPlacementAction(BaseObject placedObject,
        IEnumerable<BaseObject> conflictingObject, string comment) : base(new[] { placedObject }, comment) =>
        removedConflictObjects = conflictingObject;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            DeleteObject(obj, false);
            // BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);

        foreach (var data in removedConflictObjects)
        {
            SpawnObject(data, true);
            // BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType).SpawnObject(data, refreshesPool: false);
        }

        RefreshPools(removedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in removedConflictObjects)
        {
            DeleteObject(obj, false);
            // BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(removedConflictObjects);

        foreach (var obj in Data)
        {
            SpawnObject(obj, true);
            // BeatmapObjectContainerCollection.GetCollectionForType(con.ObjectType).SpawnObject(con, refreshesPool: false);
        }

        RefreshPools(Data);
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, removedConflictObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        removedConflictObjects = DeserializeBeatmapObjectList(reader);
    }
}
