using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;
using System.Linq;

public class SelectionPastedAction : BeatmapAction
{
    private IEnumerable<BaseObject> removed;

    // This constructor is needed for United Mapping
    public SelectionPastedAction() : base() { }

    public SelectionPastedAction(IEnumerable<BaseObject> pasteData, IEnumerable<BaseObject> removed) :
        base(pasteData)
    {
        this.affectsSeveralObjects = true;
        this.removed = removed;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            DeleteObject(obj, false);

        SelectionController.SelectionChangedEvent?.Invoke();

        foreach (var obj in removed)
            SpawnObject(obj);

        RefreshPools(removed);
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

        foreach (var obj in removed)
            DeleteObject(obj, false);
        RefreshPools(Data);
        RefreshEventAppearance();
    }

    public override void Serialize(NetDataWriter writer)
    {
        SerializeBeatmapObjectList(writer, Data);
        SerializeBeatmapObjectList(writer, removed);
    }

    public override void Deserialize(NetDataReader reader)
    {
        Data = DeserializeBeatmapObjectList(reader);
        removed = DeserializeBeatmapObjectList(reader);
    }
}
