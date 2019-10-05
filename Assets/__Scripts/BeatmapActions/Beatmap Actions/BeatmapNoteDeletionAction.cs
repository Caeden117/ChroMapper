public class BeatmapNoteDeletionAction : BeatmapAction
{
    public BeatmapNoteDeletionAction(BeatmapNoteContainer note) : base(note) { }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = param.notes.SpawnObject(BeatmapObject.GenerateCopy(data), out _);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        param.notes.DeleteObject(container);
    }
}
