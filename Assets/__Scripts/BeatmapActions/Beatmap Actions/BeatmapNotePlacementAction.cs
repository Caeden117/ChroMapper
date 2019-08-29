using UnityEngine;

public class BeatmapNotePlacementAction : BeatmapAction
{
    public BeatmapNotePlacementAction(BeatmapNoteContainer note) : base(note) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.notes.DeleteObject(container);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.notes.SpawnObject(data);
    }
}
