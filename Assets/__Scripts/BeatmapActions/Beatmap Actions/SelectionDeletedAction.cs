using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public class SelectionDeletedAction : BeatmapAction
{
    public SelectionDeletedAction(IEnumerable<IObject> deletedData) : base(deletedData)
    {
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data.ToArray())
        {
            BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType).SpawnObject(data, false, false);
            SelectionController.Select(data, true, false, false);
        }

        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (var data in Data.ToArray())
            BeatmapObjectContainerCollection.GetCollectionForType(data.ObjectType).DeleteObject(data, false, false);

        RefreshPools(Data);
    }
}
