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
        collection.DeleteObject(editedData, false);
        SelectionController.Deselect(editedData);

        collection.SpawnObject(originalData, true);
        SelectionController.Select(originalData, false, true, false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(originalData, false);
        SelectionController.Deselect(originalData);

        collection.SpawnObject(editedData, true);
        SelectionController.Select(editedData, false, true, false);
    }
}
