using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Enums;

public class StrobeGeneratorGenerationAction : BeatmapAction
{
    public IEnumerable<BaseObject> ConflictingData;

    // This constructor is needed for United Mapping
    public StrobeGeneratorGenerationAction() : base() { }

    public StrobeGeneratorGenerationAction(IEnumerable<BaseObject> generated, IEnumerable<BaseObject> conflicting)
        : base(generated) { affectsSeveralObjects = true; ConflictingData = conflicting; }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            DeleteObject(obj, false);

        SelectionController.SelectionChangedEvent?.Invoke();

        foreach (var obj in ConflictingData)
            SpawnObject(obj);

        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in ConflictingData)
            DeleteObject(obj, false);

        SelectionController.SelectionChangedEvent?.Invoke();

        foreach (var obj in Data)
            SpawnObject(obj);

        BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event).RefreshPool(true);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, ConflictingData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        ConflictingData = DeserializeBeatmapObjectList(reader);
    }
}
