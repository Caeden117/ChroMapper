using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer>
{
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;

    public override BeatmapAction GenerateAction(BeatmapNoteContainer spawned)
    {
        return new BeatmapNotePlacementAction(spawned);
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_A, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, BeatmapNoteContainer instantiated)
    {
        noteAppearanceSO.SetNoteAppearance(instantiated);
        instantiated.Directionalize(queuedData._cutDirection);
        queuedData._lineIndex = Mathf.RoundToInt(instantiated.transform.position.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiated.transform.position.y - 0.5f);
    }
}
