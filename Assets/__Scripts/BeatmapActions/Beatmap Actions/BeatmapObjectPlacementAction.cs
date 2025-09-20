using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    public IEnumerable<BaseObject> RemovedConflictObjects;

    // This constructor is needed for United Mapping
    public BeatmapObjectPlacementAction() : base() { }

    public BeatmapObjectPlacementAction(IEnumerable<BaseObject> placedContainers,
        IEnumerable<BaseObject> conflictingObjects, string comment) : base(placedContainers, comment) =>
        RemovedConflictObjects = conflictingObjects;

    public BeatmapObjectPlacementAction(BaseObject placedObject,
        IEnumerable<BaseObject> conflictingObject, string comment) : base(new[] { placedObject }, comment) =>
        RemovedConflictObjects = conflictingObject;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            DeleteObject(obj, false);
        }

        SelectionController.SelectionChangedEvent?.Invoke();
        RefreshPools(Data);

        foreach (var data in RemovedConflictObjects)
        {
            SpawnObject(data);
        }

        RefreshPools(RemovedConflictObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in RemovedConflictObjects)
        {
            DeleteObject(obj, false);
        }

        SelectionController.SelectionChangedEvent?.Invoke();
        RefreshPools(RemovedConflictObjects);

        foreach (var obj in Data)
        {
            SpawnObject(obj);
        }

        RefreshPools(Data);
    }

    public override void Serialize(NetDataWriter writer)
    {
        // Need to ensure customData is up to date before sending
        foreach (var baseObject in Data) baseObject.WriteCustom();

        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, RemovedConflictObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        RemovedConflictObjects = DeserializeBeatmapObjectList(reader);
    }
}
