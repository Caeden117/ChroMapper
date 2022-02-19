using System.Collections.Generic;
using System.Linq;

public class SelectionDeletedAction : BeatmapAction
{
    public SelectionDeletedAction(IEnumerable<BeatmapObject> deletedData) : base(deletedData)
    {
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data.ToArray())
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType).SpawnObject(data, false, false);
            SelectionController.Select(data, true, false, false);
        }

        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data.ToArray())
            BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType).DeleteObject(data, false, false);

        RefreshPools(Data);
    }
}
