using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotePlacement : PlacementController<BeatmapNote, BeatmapNoteContainer, NotesContainer>
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

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        queuedData._lineIndex = Mathf.RoundToInt(instantiatedContainer.transform.position.x + 1.5f);
        queuedData._lineLayer = Mathf.RoundToInt(instantiatedContainer.transform.position.y - 0.5f);
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
        instantiatedContainer.Directionalize(queuedData._cutDirection);
    }
}
