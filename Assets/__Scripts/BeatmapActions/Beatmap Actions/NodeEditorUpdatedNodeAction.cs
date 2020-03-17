using System.Collections.Generic;
using SimpleJSON;

public class NodeEditorUpdatedNodeAction : BeatmapAction
{
    private BeatmapObject originalData;
    private BeatmapObject editedData;

    public NodeEditorUpdatedNodeAction(BeatmapObjectContainer obj, BeatmapObject edited, BeatmapObject original)
        : base(new List<BeatmapObjectContainer>() { obj }, $"Edited a {obj.objectData.beatmapType} with Node Editor.")
    {
        editedData = edited;
        originalData = original;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        containers[0].objectData = BeatmapObject.GenerateCopy(originalData);
        param.nodeEditor.ObjectWasSelected(containers[0]);
        param.nodeEditor.UpdateAppearance(containers[0]);
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        containers[0].objectData = BeatmapObject.GenerateCopy(editedData);
        param.nodeEditor.ObjectWasSelected(containers[0]);
        param.nodeEditor.UpdateAppearance(containers[0]);
        param.tracksManager.RefreshTracks();
    }
}
