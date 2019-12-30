using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;

    private Dictionary<int, int> CutDirectionToMirrored = new Dictionary<int, int>
    {
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT },
    };

    public void Mirror()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Select stuff first!", PersistentUI.DisplayMessageType.BOTTOM);
            return;
        }
        foreach (BeatmapObjectContainer con in SelectionController.SelectedObjects)
        {
            if (con is BeatmapObstacleContainer obstacle)
            {
                bool precisionWidth = obstacle.obstacleData._width >= 1000;
                int __state = obstacle.obstacleData._lineIndex;
                if (__state >= 1000 || __state <= -1000 || precisionWidth) // precision lineIndex
                {
                    int newIndex = __state;
                    if (newIndex <= -1000) // normalize index values, we'll fix them later
                    {
                        newIndex += 1000;
                    }
                    else if (newIndex >= 1000)
                    {
                        newIndex -= 1000;
                    }
                    else
                    {
                        newIndex = newIndex * 1000; //convert lineIndex to precision if not already
                    }
                    newIndex = (((newIndex - 2000) * -1) + 2000); //flip lineIndex

                    int newWidth = obstacle.obstacleData._width; //normalize wall width
                    if (newWidth < 1000)
                    {
                        newWidth = newWidth * 1000;
                    }
                    else
                    {
                        newWidth -= 1000;
                    }
                    newIndex = newIndex - newWidth;

                    if (newIndex < 0)
                    { //this is where we fix them
                        newIndex -= 1000;
                    }
                    else
                    {
                        newIndex += 1000;
                    }
                    obstacle.obstacleData._lineIndex = newIndex;
                }
                else // state > -1000 || state < 1000 assumes no precision width
                {
                    int mirrorLane = (((__state - 2) * -1) + 2); //flip lineIndex
                    obstacle.obstacleData._lineIndex = mirrorLane - obstacle.obstacleData._width; //adjust for wall width
                }
                con.UpdateGridPosition();
            }
            else if (con is BeatmapNoteContainer note)
            {
                int __state = note.mapNoteData._lineIndex; // flip line index
                if (__state > 3 || __state < 0) // precision case
                {
                    int newIndex = __state;
                    if (newIndex <= -1000) // normalize index values, we'll fix them later
                    {
                        newIndex += 1000;
                    }
                    else if (newIndex >= 1000)
                    {
                        newIndex -= 1000;
                    }
                    newIndex = (((newIndex - 1500) * -1) + 1500); //flip lineIndex

                    if (newIndex < 0) //this is where we fix them
                    { 
                        newIndex -= 1000;
                    }
                    else
                    {
                        newIndex += 1000;
                    }
                    note.mapNoteData._lineIndex = newIndex;
                }
                else
                {
                    int mirrorLane = (int)(((__state - 1.5f) * -1) + 1.5f);
                    note.mapNoteData._lineIndex = mirrorLane;
                }
                con.UpdateGridPosition();

                //flip colors
                if (note.mapNoteData is BeatmapChromaNote chroma) note.mapNoteData = chroma.originalNote; //Revert Chroma status, then invert types
                if (note.mapNoteData._type != BeatmapNote.NOTE_TYPE_BOMB)
                {
                    note.mapNoteData._type = note.mapNoteData._type == BeatmapNote.NOTE_TYPE_A ? BeatmapNote.NOTE_TYPE_B : BeatmapNote.NOTE_TYPE_A;

                    //flip cut direction horizontally
                    if (CutDirectionToMirrored.ContainsKey(note.mapNoteData._cutDirection))
                    {
                        note.mapNoteData._cutDirection = CutDirectionToMirrored[note.mapNoteData._cutDirection];
                        note.Directionalize(note.mapNoteData._cutDirection);
                    }
                }
                noteAppearance.SetNoteAppearance(note);
            }
            else if (con is BeatmapEventContainer e)
            {
                if (e.eventData.IsUtilityEvent()) return;
                if (e.eventData._value > 4 && e.eventData._value < 8) e.eventData._value -= 4;
                else if (e.eventData._value > 0 && e.eventData._value <= 4) e.eventData._value += 4;
                eventAppearance.SetEventAppearance(e);
            }
        }
        SelectionController.RefreshMap();
    }
}
