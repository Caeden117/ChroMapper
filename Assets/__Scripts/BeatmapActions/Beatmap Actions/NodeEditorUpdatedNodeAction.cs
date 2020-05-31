using System;
using System.Collections.Generic;
using SimpleJSON;

public class NodeEditorUpdatedNodeAction : BeatmapAction
{
    private BeatmapObject originalData;
    private BeatmapObject editedData;
    private BeatmapObjectContainerCollection collection;

    public NodeEditorUpdatedNodeAction(BeatmapObject edited, BeatmapObject original)
        : base(null, $"Edited a {original.beatmapType} with Node Editor.")
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType(original.beatmapType);
        editedData = edited;
        originalData = original;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(editedData, false, false);
        JSONNode node = originalData.ConvertToJSON();
        BeatmapObject newObject = Activator.CreateInstance(originalData.GetType(), new object[] { node }) as BeatmapObject;
        collection.SpawnObject(newObject, false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(originalData, false, false);
        JSONNode node = editedData.ConvertToJSON();
        BeatmapObject newObject = Activator.CreateInstance(originalData.GetType(), new object[] { node }) as BeatmapObject;
        collection.SpawnObject(newObject, false);
    }
}
