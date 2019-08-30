using System.Collections.Generic;
public class SelectionChangedAction : BeatmapAction
{
    private List<BeatmapObjectContainer> selected = new List<BeatmapObjectContainer>();
    private List<BeatmapObjectContainer> oldSelected = new List<BeatmapObjectContainer>();

    public SelectionChangedAction(List<BeatmapObjectContainer> selection) : base(null)
    {
        oldSelected = selection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        selected = new List<BeatmapObjectContainer>(SelectionController.SelectedObjects);
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects.AddRange(oldSelected);
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        oldSelected = new List<BeatmapObjectContainer>(SelectionController.SelectedObjects);
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects.AddRange(selected);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
