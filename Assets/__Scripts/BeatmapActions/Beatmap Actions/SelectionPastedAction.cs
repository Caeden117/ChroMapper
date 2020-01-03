using System.Collections.Generic;
using System.Linq;

public class SelectionPastedAction : BeatmapAction
{
    private List<BeatmapObjectContainer> pastedObjects;
    private List<BeatmapObject> pastedData;
    private float time = 0;

    public SelectionPastedAction(List<BeatmapObjectContainer> pasted, List<BeatmapObject> pasteData, float time) : base(null)
    {
        pastedObjects = new List<BeatmapObjectContainer>(pasted);
        pastedData = new List<BeatmapObject>(pasteData);
        this.time = time;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObjectContainer obj in pastedObjects) param.collections.ForEach(x => x.DeleteObject(obj, false));
        SelectionController.CopiedObjects = pastedData;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        float beatTime = param.collections.First().AudioTimeSyncController.CurrentBeat;
        param.collections.First().AudioTimeSyncController.MoveToTimeInBeats(time);
        param.selection.Paste(false);
        param.collections.First().AudioTimeSyncController.MoveToTimeInBeats(beatTime);
        param.tracksManager.RefreshTracks();
    }
}
