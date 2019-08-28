using UnityEngine;

public class BeatmapNotePlacementAction : BeatmapAction
{
    public BeatmapNotePlacementAction(BeatmapNoteContainer note) : base(note) { }

    public override void Undo(ref GameObject objectContainerPrefab, params object[] others)
    {
        Object.Destroy(container);
    }

    public override void Redo(ref GameObject objectContainerPrefab, params object[] others)
    {
        BeatmapNoteContainer note = objectContainerPrefab.AddComponent<BeatmapNoteContainer>();
        note.mapNoteData = (data as BeatmapNote);
    }
}
