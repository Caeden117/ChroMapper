using System.Collections.Generic;
using System.Linq;

public class SelectionPastedAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> removed;

    public SelectionPastedAction(IEnumerable<BeatmapObject> pasteData, IEnumerable<BeatmapObject> removed) : base(pasteData)
    {
        this.removed = removed;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false, false);
        }
        foreach (BeatmapObject obj in removed)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).SpawnObject(obj, false);
        }
        RefreshPools(removed);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapObject obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).SpawnObject(obj, false, false);
        }
        foreach (BeatmapObject obj in removed)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).DeleteObject(obj, false);
        }
        RefreshPools(Data);
    }
}
