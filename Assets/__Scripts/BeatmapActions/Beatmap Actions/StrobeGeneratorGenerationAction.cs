using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Enums;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    private IEnumerable<BaseObject> conflictingData;

    public StrobeGeneratorGenerationAction() : base() { }

    public StrobeGeneratorGenerationAction(IEnumerable<BaseObject> generated, IEnumerable<BaseObject> conflicting)
        : base(generated) { affectsSeveralObjects = true; conflictingData = conflicting; }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            DeleteObject(obj, false);
        foreach (var obj in conflictingData)
            SpawnObject(obj);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in conflictingData)
            DeleteObject(obj, false);
        foreach (var obj in Data)
            SpawnObject(obj);
        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, conflictingData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        conflictingData = DeserializeBeatmapObjectList(reader);
    }
}
