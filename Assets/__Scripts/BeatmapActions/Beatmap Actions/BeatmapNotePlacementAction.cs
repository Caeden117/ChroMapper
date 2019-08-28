using UnityEngine;

public class BeatmapNotePlacementAction : BeatmapAction<BeatmapNoteContainer>
{
    public BeatmapNotePlacementAction(BeatmapNoteContainer note) : base(note) { }

    public override void Undo(ref GameObject objectContainerPrefab)
    {
        Object.Destroy(container);
    }

    public override void Redo(ref GameObject objectContainerPrefab)
    {
        BeatmapNoteContainer note = objectContainerPrefab.AddComponent<BeatmapNoteContainer>();
        note.mapNoteData = (data as BeatmapNote);
    }
}
