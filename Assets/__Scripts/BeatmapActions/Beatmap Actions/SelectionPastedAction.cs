using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;
using System.Linq;

public class SelectionPastedAction : BeatmapAction
{
    public IEnumerable<BaseObject> Removed;

    // This constructor is needed for United Mapping
    public SelectionPastedAction() : base() { }

    public SelectionPastedAction(IEnumerable<BaseObject> pasteData, IEnumerable<BaseObject> removed) :
        base(pasteData)
    {
        this.affectsSeveralObjects = true;
        this.Removed = removed;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data) DeleteObject(obj, false);

        SelectionController.SelectionChangedEvent?.Invoke();

        foreach (var obj in Removed) SpawnObject(obj);

        RefreshPools(Removed);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (!Networked)
        {
            SelectionController.DeselectAll();
        }

        foreach (var obj in Data)
        {
            SpawnObject(obj);

            if (!Networked)
            {
                SelectionController.Select(obj, true, false, false);
            }
        }

        foreach (var obj in Removed) DeleteObject(obj, false);
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, Removed);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        Removed = DeserializeBeatmapObjectList(reader);
    }
}
