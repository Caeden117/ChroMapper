using System.Collections.Generic;
using LiteNetLib.Utils;
using System.Linq;
using Beatmap.Base;

public class SelectionDeletedAction : BeatmapAction
{
    // This constructor is needed for United Mapping
    public SelectionDeletedAction() : base() { }

    public SelectionDeletedAction(IEnumerable<BaseObject> deletedData) : base(deletedData)
    {
        this.affectsSeveralObjects = true;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data)
        {
            SpawnObject(data);

            if (!Networked)
            {
                SelectionController.Select(data, true, false, false);
            }
        }

        SelectionController.SelectionChangedEvent?.Invoke();
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data)
            DeleteObject(data, false);

        SelectionController.SelectionChangedEvent?.Invoke();
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer) => SerializeBeatmapObjectList(writer, Data);

    public override void Deserialize(NetDataReader reader) => Data = DeserializeBeatmapObjectList(reader);
}
