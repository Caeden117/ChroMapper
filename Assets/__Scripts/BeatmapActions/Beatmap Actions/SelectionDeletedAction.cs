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
            BeatmapObjectContainer recovered = null;
            BeatmapObject copy = BeatmapObject.GenerateCopy(data);
            param.collections.Where(x => x.ContainerType == copy.beatmapType).FirstOrDefault()?.SpawnObject(copy, out _);
            SelectionController.SelectedObjects.Add(recovered);
        }
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.selection.Delete(false);
    }
}
