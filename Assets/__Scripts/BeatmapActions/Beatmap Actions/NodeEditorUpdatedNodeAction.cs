using System.Collections.Generic;
using SimpleJSON;

public class NodeEditorUpdatedNodeAction : BeatmapAction
{
    private JSONNode originalData;
    private JSONNode editedData;

    public NodeEditorUpdatedNodeAction(BeatmapObjectContainer obj, JSONNode edited, JSONNode original)
        : base(new List<BeatmapObjectContainer>() { obj })
    {
        editedData = edited;
        originalData = original;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        containers[0].objectData = BeatmapObject.GenerateCopy(containers[0].objectData);
        param.nodeEditor.ObjectWasSelected(containers[0]);
        param.nodeEditor.UpdateAppearance(containers[0]);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        containers[0].objectData = BeatmapObject.GenerateCopy(containers[0].objectData);
        param.nodeEditor.ObjectWasSelected(containers[0]);
        param.nodeEditor.UpdateAppearance(containers[0]);
    }
}
