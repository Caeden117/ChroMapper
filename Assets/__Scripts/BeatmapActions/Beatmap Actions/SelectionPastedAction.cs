using System.Collections.Generic;

public class SelectionPastedAction : BeatmapAction
{
    private readonly IEnumerable<BeatmapObject> removed;

    public SelectionPastedAction(IEnumerable<BeatmapObject> pasteData, IEnumerable<BeatmapObject> removed) :
        base(pasteData) => this.removed = removed;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false, false);
        foreach (var obj in removed)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).SpawnObject(obj, false);
        RefreshPools(removed);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).SpawnObject(obj, false, false);
            SelectionController.Select(obj, true, false, false);
        }

        foreach (var obj in removed)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).DeleteObject(obj, false);
        RefreshPools(Data);
    }
}
