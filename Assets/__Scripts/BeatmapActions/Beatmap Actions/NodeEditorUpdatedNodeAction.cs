using System.Collections.Generic;
using SimpleJSON;

public class NodeEditorUpdatedNodeAction : BeatmapAction
{
    private BeatmapObjectContainer container;
    private BeatmapObject originalData;
    private BeatmapObject editedData;

    public NodeEditorUpdatedNodeAction(BeatmapObjectContainer obj, BeatmapObject edited, BeatmapObject original)
        : base(null, $"Edited a {obj.objectData.beatmapType} with Node Editor.")
    {
        container = obj;
        editedData = edited;
        originalData = original;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.nodeEditor.ObjectWasSelected(originalData);
        param.nodeEditor.UpdateAppearance(container);
        RefreshPools(new[] { originalData });
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container.objectData = editedData;
        param.nodeEditor.ObjectWasSelected(editedData);
        param.nodeEditor.UpdateAppearance(container);
        RefreshPools(new[] { editedData });
    }
}
