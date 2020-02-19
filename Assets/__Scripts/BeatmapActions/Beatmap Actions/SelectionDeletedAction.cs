using System.Collections.Generic;
using System.Linq;

public class SelectionDeletedAction : BeatmapAction
{
    private List<BeatmapObject> deletedData = new List<BeatmapObject>();

    public SelectionDeletedAction(List<BeatmapObjectContainer> selection) : base(null)
    {
        foreach (BeatmapObjectContainer container in selection) deletedData.Add(container.objectData);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach(BeatmapObject data in deletedData)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(data);
            BeatmapObjectContainer recovered = BeatmapObjectContainerCollection.GetCollectionForType(copy.beatmapType)?.SpawnObject(copy);
            SelectionController.Select(recovered, true, false);
        }
        SelectionController.RefreshSelectionMaterial(false);
        param.tracksManager.RefreshTracks();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.selection.Delete(false);
        param.tracksManager.RefreshTracks();
    }
}
