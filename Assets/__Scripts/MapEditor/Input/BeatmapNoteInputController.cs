using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapNoteInputController : BeatmapInputController<BeatmapNoteContainer>, CMInput.INoteObjectsActions
{
    private Dictionary<int, int> CutDirectionMovedForward = new Dictionary<int, int>
    {
        { BeatmapNote.NOTE_CUT_DIRECTION_ANY, BeatmapNote.NOTE_CUT_DIRECTION_ANY },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_UP },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN },
        { BeatmapNote.NOTE_CUT_DIRECTION_NONE, BeatmapNote.NOTE_CUT_DIRECTION_NONE }
    };

    private Dictionary<int, int> CutDirectionMovedBackward = new Dictionary<int, int>
    {
        { BeatmapNote.NOTE_CUT_DIRECTION_ANY, BeatmapNote.NOTE_CUT_DIRECTION_ANY },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN },
        { BeatmapNote.NOTE_CUT_DIRECTION_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_UP },
        { BeatmapNote.NOTE_CUT_DIRECTION_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_NONE, BeatmapNote.NOTE_CUT_DIRECTION_NONE }
    };

    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    public bool QuickModificationActive;

    //Do some shit later lmao
    public void OnInvertNoteColors(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true) || !KeybindsController.IsMouseInWindow || !context.performed) return;

        RaycastFirstObject(out BeatmapNoteContainer note);
        if (note != null)
        {
            InvertNote(note);
        }
    }

    public void OnQuickDirectionModifier(InputAction.CallbackContext context)
    {
        QuickModificationActive = context.performed;
    }

    public void InvertNote(BeatmapNoteContainer note)
    {
        if (note.mapNoteData._type == BeatmapNote.NOTE_TYPE_BOMB) return;

        var original = BeatmapObject.GenerateCopy(note.objectData);
        var newType = note.mapNoteData._type == BeatmapNote.NOTE_TYPE_A ? BeatmapNote.NOTE_TYPE_B : BeatmapNote.NOTE_TYPE_A;
        note.mapNoteData._type = newType;
        noteAppearanceSO.SetNoteAppearance(note);
        var collection = BeatmapObjectContainerCollection.GetCollectionForType<NotesContainer>(BeatmapObject.Type.NOTE);
        collection.RefreshSpecialAngles(note.objectData, false, false);
        collection.RefreshSpecialAngles(original, false, false);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(note.objectData, note.objectData, original));
    }

    public void OnUpdateNoteDirection(InputAction.CallbackContext context)
    {
        if (customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        if (!context.performed) return;

        bool shiftForward = context.ReadValue<float>() > 0;
        RaycastFirstObject(out BeatmapNoteContainer note);
        if (note != null)
        {
            UpdateNoteDirection(note, shiftForward);
        }
    }

    public void UpdateNoteDirection(BeatmapNoteContainer note, bool shiftForward)
    {
        var original = BeatmapObject.GenerateCopy(note.objectData);
        note.mapNoteData._cutDirection = (shiftForward ? CutDirectionMovedForward : CutDirectionMovedBackward)[note.mapNoteData._cutDirection];
        note.transform.localEulerAngles = BeatmapNoteContainer.Directionalize(note.mapNoteData);
        BeatmapObjectContainerCollection.GetCollectionForType<NotesContainer>(BeatmapObject.Type.NOTE)
            .RefreshSpecialAngles(note.objectData, false, false);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(note.objectData, note.objectData, original));
    }
}
