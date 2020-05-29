using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>, CMInput.INotePlacementActions
{
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private NotePlacementUI notePlacementUI;
    private bool upNote = false;
    private bool leftNote = false;
    private bool downNote = false;
    private bool rightNote = false;

    public override bool IsValid
    {
        get => base.IsValid || isDraggingObject;
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
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
        instantiatedContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapNote dragged, BeatmapNote queued)
    {
        dragged._time = queued._time;
        dragged._lineIndex = queued._lineIndex;
        dragged._lineLayer = queued._lineLayer;
        dragged._cutDirection = queued._cutDirection;
        if (draggedObjectContainer != null)
        {
            draggedObjectContainer.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(dragged);
        }
        noteAppearanceSO?.SetNoteAppearance(draggedObjectContainer);
    }

    private void DisableDeleteTool()
    {
        notePlacementUI.UpdateValue(queuedData._type);
    }

    public void OnDownNote(InputAction.CallbackContext context)
    {
        downNote = context.performed;
        if (!downNote) return;
        if (!leftNote && !rightNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN);
        else if (leftNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
        else if (rightNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
    }

    public void OnLeftNote(InputAction.CallbackContext context)
    {
        leftNote = context.performed;
        if (!leftNote) return;
        if (!upNote && !downNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_LEFT);
        else if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
        else if (downNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT);
    }

    public void OnUpNote(InputAction.CallbackContext context)
    {
        upNote = context.performed;
        if (!upNote) return;
        if (!leftNote && !rightNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP);
        else if (leftNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT);
        else if (rightNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
    }

    public void OnRightNote(InputAction.CallbackContext context)
    {
        rightNote = context.performed;
        if (!rightNote) return;
        if (!upNote && !downNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_RIGHT);
        else if (upNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT);
        else if (downNote) UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT);
    }

    public void OnDotNote(InputAction.CallbackContext context)
    {
        UpdateCut(BeatmapNote.NOTE_CUT_DIRECTION_ANY);
    }
}
