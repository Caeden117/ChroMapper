using System.Collections.Generic;

public class SelectionDeletedAction : BeatmapAction
{
    public SelectionDeletedAction(IEnumerable<BeatmapObject> deletedData) : base(deletedData)
    {
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObject data in Data)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(data);
            BeatmapObjectContainerCollection.GetCollectionForType(copy.beatmapType)?.SpawnObject(copy);
            SelectionController.Select(data, true, false);
        }
        SelectionController.RefreshSelectionMaterial(false);
        RefreshPools(Data);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.selection.Delete(false);
        RefreshPools(Data);
    }
}
