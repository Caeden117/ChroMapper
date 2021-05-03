using System;
using System.Collections.Generic;

[Obsolete("We don't track selection changes anymore")]
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
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(Data);
        SelectionController.RefreshSelectionMaterial(false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(Data);
        SelectionController.RefreshSelectionMaterial(false);
    }
}
