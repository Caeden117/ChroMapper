using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer>
{
    public override BeatmapAction GenerateAction(BeatmapNoteContainer spawned)
    {
        return new BeatmapNotePlacementAction(spawned);
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_BOMB, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.position.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.position.y - 0.5f);
    }
}
