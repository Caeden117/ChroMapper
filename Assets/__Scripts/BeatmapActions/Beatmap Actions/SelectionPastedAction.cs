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
        foreach (BeatmapObjectContainer obj in pastedObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.objectData.beatmapType).DeleteObject(obj, false);
        }
        SelectionController.CopiedObjects = pastedData;
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        AudioTimeSyncController atsc = BeatmapObjectContainerCollection.GetAnyCollection().AudioTimeSyncController;
        float beatTime = atsc.CurrentBeat;
        atsc.MoveToTimeInBeats(time);
        param.selection.Paste(false);
        atsc.MoveToTimeInBeats(beatTime);
        param.tracksManager.RefreshTracks();
    }
}
