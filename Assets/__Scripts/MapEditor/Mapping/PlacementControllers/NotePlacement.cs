using UnityEngine;
using UnityEngine.InputSystem;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>, CMInput.INotePlacementActions
{
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;

    public override bool IsValid
    {
        get => base.IsValid || isDraggingObject;
    }

    public override BeatmapAction GenerateAction(BeatmapNoteContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed a note.");
    }

    public override BeatmapNote GenerateOriginalData()
    {
        return new BeatmapNote(0, 0, 0, BeatmapNote.NOTE_TYPE_A, BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.y - 0.5f);
        UpdateAppearance();
    }

    public void UpdateCut(int value)
    {
        queuedData._cutDirection = value;
        UpdateAppearance();
    }

    public void UpdateType(int type)
    {
        queuedData._type = type;
        UpdateAppearance();
    }

    public void ChangeChromaToggle(bool isChromaToggleNote)
    {
        if (isChromaToggleNote)
        {
            BeatmapChromaNote data = new BeatmapChromaNote(queuedData);
            data.BombRotation = BeatmapChromaNote.ALTERNATE;
            queuedData = data;
        }
        else if (queuedData is BeatmapChromaNote data) queuedData = data.ConvertToNote();
        UpdateAppearance();
    }

    public void UpdateChromaValue(int chromaNoteValue)
    {
        if (queuedData is BeatmapChromaNote chroma)
        {
            chroma.BombRotation = chromaNoteValue;
            UpdateAppearance();
        }
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) return;
        instantiatedContainer.mapNoteData = queuedData;
        noteAppearanceSO.SetNoteAppearance(instantiatedContainer);
        foreach (MeshRenderer renderer in instantiatedContainer.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.material.HasProperty("_AlwaysTranslucent") && renderer.material.GetFloat("_AlwaysTranslucent") == 1)
                continue; //Dont want to do this shit almost every frame.
            renderer.material.SetFloat("_AlwaysTranslucent", 1);
        }
        instantiatedContainer.Directionalize(queuedData._cutDirection);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
        dragged._cutDirection = queued._cutDirection;
        draggedObjectContainer?.Directionalize(dragged._cutDirection);
        noteAppearanceSO?.SetNoteAppearance(draggedObjectContainer);
    }

    public override bool IsObjectOverlapping(BeatmapNote draggedData, BeatmapNote overlappingData)
    {
        return draggedData._lineIndex == overlappingData._lineIndex && draggedData._lineLayer == overlappingData._lineLayer;
    }

    public void OnDownNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
    }

    public void OnLeftNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
    }

    public void OnUpNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
    }

    public void OnRightNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.started) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
    }
}
