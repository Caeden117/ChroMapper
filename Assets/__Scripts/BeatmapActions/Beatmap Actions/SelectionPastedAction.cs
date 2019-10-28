using System.Collections.Generic;
using UnityEngine;

public class SelectionPastedAction : BeatmapAction
{
    private List<BeatmapObjectContainer> pastedObjects = new List<BeatmapObjectContainer>();
    private List<BeatmapObject> pastedData = new List<BeatmapObject>();
    private float time = 0;

    public SelectionPastedAction(List<BeatmapObjectContainer> pasted, List<BeatmapObject> pasteData, float time) : base(null)
    {
        pastedObjects = new List<BeatmapObjectContainer>(pasted);
        pastedData = new List<BeatmapObject>(pasteData);
        this.time = time;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in pastedObjects)
        {
            param.bpm.DeleteObject(obj);
            param.notes.DeleteObject(obj);
            param.events.DeleteObject(obj);
            param.obstacles.DeleteObject(obj);
            param.customEvents.DeleteObject(obj);
        }
        SelectionController.CopiedObjects = pastedData;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        float beatTime = param.notes.AudioTimeSyncController.CurrentBeat;
        param.notes.AudioTimeSyncController.MoveToTimeInBeats(time);
        param.selection.Paste(false);
        param.notes.AudioTimeSyncController.MoveToTimeInBeats(beatTime);
    }
}
