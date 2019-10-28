using SimpleJSON;
public class NodeEditorUpdatedNodeAction : BeatmapAction
{
    private JSONNode originalData;
    private JSONNode editedData;

    public NodeEditorUpdatedNodeAction(BeatmapObjectContainer obj, JSONNode edited, JSONNode original) : base(obj)
    {
        editedData = edited;
        originalData = original;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        switch (container.objectData.beatmapType)
        {
            case BeatmapObject.Type.NOTE:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(originalData);
                break;
            case BeatmapObject.Type.BOMB:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(originalData);
                break;
            case BeatmapObject.Type.CUSTOM_NOTE:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(originalData);
                break;
            case BeatmapObject.Type.OBSTACLE:
                (container as BeatmapObstacleContainer).obstacleData = new BeatmapObstacle(originalData);
                break;
            case BeatmapObject.Type.EVENT:
                (container as BeatmapEventContainer).eventData = new MapEvent(originalData);
                break;
            case BeatmapObject.Type.CUSTOM_EVENT:
                (container as BeatmapCustomEventContainer).customEventData = new BeatmapCustomEvent(originalData);
                break;
        }
        param.nodeEditor.ObjectWasSelected(container);
        param.nodeEditor.UpdateAppearance(container);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        switch (container.objectData.beatmapType)
        {
            case BeatmapObject.Type.NOTE:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(editedData);
                break;
            case BeatmapObject.Type.BOMB:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(editedData);
                break;
            case BeatmapObject.Type.CUSTOM_NOTE:
                (container as BeatmapNoteContainer).mapNoteData = new BeatmapNote(editedData);
                break;
            case BeatmapObject.Type.OBSTACLE:
                (container as BeatmapObstacleContainer).obstacleData = new BeatmapObstacle(editedData);
                break;
            case BeatmapObject.Type.EVENT:
                (container as BeatmapEventContainer).eventData = new MapEvent(editedData);
                break;
            case BeatmapObject.Type.CUSTOM_EVENT:
                (container as BeatmapCustomEventContainer).customEventData = new BeatmapCustomEvent(editedData);
                break;
        }
        param.nodeEditor.ObjectWasSelected(container);
        param.nodeEditor.UpdateAppearance(container);
    }
}
