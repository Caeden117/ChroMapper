using System.Collections.Generic;
using System.Linq;

public class SelectionPastedAction : BeatmapAction
{
    private float time = 0;

    public SelectionPastedAction(IEnumerable<BeatmapObject> pasteData, float time) : base(pasteData)
    {
        this.time = time;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false);
        }
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var atsc = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE).AudioTimeSyncController;
        SelectionController.CopiedObjects = new HashSet<BeatmapObject>(Data);
        float beatTime = atsc.CurrentBeat;
        atsc.MoveToTimeInBeats(time);
        param.selection.Paste(false);
        atsc.MoveToTimeInBeats(beatTime);
        RefreshPools(Data);
    }
}
