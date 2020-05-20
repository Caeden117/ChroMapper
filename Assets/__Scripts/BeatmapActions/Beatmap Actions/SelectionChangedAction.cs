using System.Collections.Generic;
public class SelectionChangedAction : BeatmapAction
{
    private IEnumerable<BeatmapObject> oldSelection;

    public SelectionChangedAction(HashSet<BeatmapObject> selection, IEnumerable<BeatmapObject> oldSelection) : base(selection)
    {
        this.oldSelection = oldSelection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new SortedSet<BeatmapObject>(Data, new BeatmapObjectComparer());
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new SortedSet<BeatmapObject>(Data, new BeatmapObjectComparer());
        SelectionController.RefreshSelectionMaterial(false);
    }
}
