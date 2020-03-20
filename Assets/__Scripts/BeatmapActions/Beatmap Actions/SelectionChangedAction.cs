using System.Collections.Generic;
public class SelectionChangedAction : BeatmapAction
{
    private HashSet<BeatmapObjectContainer> selected = new HashSet<BeatmapObjectContainer>();
    private HashSet<BeatmapObjectContainer> oldSelected;

    public SelectionChangedAction(HashSet<BeatmapObjectContainer> selection) : base(null)
    {
        oldSelected = selection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        selected = new HashSet<BeatmapObjectContainer>(SelectionController.SelectedObjects);
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObjectContainer>(oldSelected);
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        oldSelected = new HashSet<BeatmapObjectContainer>(SelectionController.SelectedObjects);
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObjectContainer>(selected);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
