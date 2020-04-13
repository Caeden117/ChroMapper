using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

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

    //Do some shit later lmao
    public void OnInvertNoteColors(InputAction.CallbackContext context)
    {
        if (!KeybindsController.AnyCriticalKeys)
        {
            RaycastFirstObject(out BeatmapNoteContainer note);
            if (note != null && note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB)
            {
                int newType = note.mapNoteData._type == BeatmapNote.NOTE_TYPE_A ? BeatmapNote.NOTE_TYPE_B : BeatmapNote.NOTE_TYPE_A;
                note.mapNoteData._type = newType;
                noteAppearanceSO.SetNoteAppearance(note);
            }
        }
    }

    public void OnUpdateNoteDirection(InputAction.CallbackContext context)
    {
        if (KeybindsController.AltHeld)
        {
            bool shiftForward = context.ReadValue<float>() > 0;
            RaycastFirstObject(out BeatmapNoteContainer note);
            if (note != null)
            {
                if (shiftForward)
                    note.mapNoteData._cutDirection = CutDirectionMovedForward[note.mapNoteData._cutDirection];
                else note.mapNoteData._cutDirection = CutDirectionMovedBackward[note.mapNoteData._cutDirection];
                note.Directionalize(note.mapNoteData._cutDirection);
            }
        }
    }
}
