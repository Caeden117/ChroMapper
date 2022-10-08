using System.Collections.Generic;
using Beatmap.Base;

public class SelectionPastedAction : BeatmapAction
{
    private readonly IEnumerable<IObject> removed;

    public SelectionPastedAction(IEnumerable<IObject> pasteData, IEnumerable<IObject> removed) :
        base(pasteData) => this.removed = removed;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var obj in Data)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false, false);
        foreach (var obj in removed)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, false);
        RefreshPools(removed);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        foreach (var obj in Data)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).SpawnObject(obj, false, false);
            SelectionController.Select(obj, true, false, false);
        }

        foreach (var obj in removed)
            BeatmapObjectContainerCollection.GetCollectionForType(obj.ObjectType).DeleteObject(obj, false);
        RefreshPools(Data);
    }
}
