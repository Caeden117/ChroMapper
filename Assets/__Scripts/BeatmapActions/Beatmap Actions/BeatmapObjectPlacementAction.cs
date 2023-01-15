using System.Collections.Generic;
using LiteNetLib.Utils;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> removedConflictObjects;

    public BeatmapObjectPlacementAction() : base() { }

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
            DeleteObject(obj, false);
        }

        RefreshPools(Data);

        foreach (var data in removedConflictObjects)
        {
            SpawnObject(data, true);
        }

        RefreshPools(removedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in removedConflictObjects)
        {
            DeleteObject(obj, false);
        }

        RefreshPools(removedConflictObjects);

        foreach (var obj in Data)
        {
            SpawnObject(obj, true);
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
