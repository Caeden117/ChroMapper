using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction() : base() { }

    public BeatmapObjectDeletionAction(IEnumerable<BaseObject> objs, string comment) : base(objs, comment) { }

    public BeatmapObjectDeletionAction(BaseObject obj, string comment) : base(new[] { obj }, comment) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            SpawnObject(obj, true);
            // BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, refreshesPool: false);
        }

        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            DeleteObject(obj, false);
            // BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        }

        RefreshPools(Data);
    }

    public override void Serialize(NetDataWriter writer) => SerializeBeatmapObjectList(writer, Data);

    public override void Deserialize(NetDataReader reader) => Data = DeserializeBeatmapObjectList(reader);
}
