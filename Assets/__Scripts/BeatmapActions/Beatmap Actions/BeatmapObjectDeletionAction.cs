using System.Collections.Generic;
using LiteNetLib.Utils;

public class BeatmapObjectDeletionAction : BeatmapAction
{
    public BeatmapObjectDeletionAction() : base() { }

    public BeatmapObjectDeletionAction(IEnumerable<BeatmapObject> objs, string comment) : base(objs, comment) { }

    public BeatmapObjectDeletionAction(BeatmapObject obj, string comment) : base(new[] { obj }, comment) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            SpawnObject(obj, true);
        }

        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
        {
            DeleteObject(obj, false);
        }

        RefreshPools(Data);
    }

    public override void Serialize(NetDataWriter writer) => SerializeBeatmapObjectList(writer, Data);

    public override void Deserialize(NetDataReader reader) => Data = DeserializeBeatmapObjectList(reader);
}
