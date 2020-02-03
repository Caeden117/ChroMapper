using UnityEngine;

public class BombPlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
{
    public override BeatmapAction GenerateAction(BeatmapNoteContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_BOMB, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.material.HasProperty("_AlwaysTranslucent") && renderer.material.GetFloat("_AlwaysTranslucent") == 1)
                continue; //Dont want to do this shit almost every frame.
            renderer.material.SetFloat("_AlwaysTranslucent", 1);
        }
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
    }
    public override bool IsObjectOverlapping(BeatmapNote draggedData, BeatmapNote overlappingData)
    {
        return draggedData._lineIndex == overlappingData._lineIndex && draggedData._lineLayer == overlappingData._lineLayer;
    }
}
