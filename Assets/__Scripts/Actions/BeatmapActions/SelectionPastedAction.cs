using System.Collections.Generic;
using LiteNetLib.Utils;
using Beatmap.Base;

public class SelectionPastedAction : BeatmapAction
{
    public IEnumerable<BaseObject> Removed;

    // This constructor is needed for United Mapping
    public SelectionPastedAction() : base() { }

    public SelectionPastedAction(IEnumerable<BaseObject> pasteData, IEnumerable<BaseObject> removed) :
        base(pasteData)
    {
        affectsSeveralObjects = true;
        Removed = removed;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data) DeleteObject(obj, false);
        SelectionController.NotifySelectionChanged();

        // Consume boost and pooled-container invalidations from the removed paste before restoring any conflicts.
        RefreshPools(Data);

        foreach (var obj in Removed) SpawnObject(obj);
        RefreshPools(Removed);
        RefreshEventAppearance();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (!Networked) SelectionController.DeselectAll();
        foreach (var obj in Removed) DeleteObject(obj, false);
        foreach (var obj in Data)
        {
            SpawnObject(obj);
            if (!Networked) SelectionController.Select(obj, true, false, false);
        }

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
