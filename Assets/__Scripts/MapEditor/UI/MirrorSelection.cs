using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CreateEventTypeLabels labels;

    private Dictionary<int, int> CutDirectionToMirrored = new Dictionary<int, int>
    {
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_DOWN_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_DOWN_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT },
        { BeatmapNote.NOTE_CUT_DIRECTION_UP_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_UP_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_RIGHT, BeatmapNote.NOTE_CUT_DIRECTION_LEFT },
        { BeatmapNote.NOTE_CUT_DIRECTION_LEFT, BeatmapNote.NOTE_CUT_DIRECTION_RIGHT },
    };

    public void MirrorTime()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.BOTTOM);
            return;
        }

        var ordered = SelectionController.SelectedObjects.OrderByDescending(x => x._time);
        var end = ordered.First()._time;
        var start = ordered.Last()._time;
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var edited = BeatmapObject.GenerateCopy(con);
            edited._time = start + (end - con._time);
            allActions.Add(new BeatmapObjectModifiedAction(edited, con, con, "e", true));
        }

        var actionCollection = new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects in time.");
        BeatmapActionContainer.AddAction(actionCollection, true);
    }

    public void Mirror(bool moveNotes = true)
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.BOTTOM);
            return;
        }
        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventsContainer>(BeatmapObject.Type.EVENT);
        List<BeatmapAction> allActions = new List<BeatmapAction>();
        foreach (BeatmapObject con in SelectionController.SelectedObjects)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(con);
            if (con is BeatmapObstacle obstacle && moveNotes)
            {
                bool precisionWidth = obstacle._width >= 1000;
                int __state = obstacle._lineIndex;
                if (obstacle._customData != null) //Noodle Extensions
                {
                    if (obstacle._customData.HasKey("_position"))
                    {
                        Vector2 oldPosition = obstacle._customData["_position"];
                        Vector2 flipped = new Vector2(oldPosition.x * -1, oldPosition.y);
                        if (obstacle._customData.HasKey("_scale"))
                        {
                            Vector2 scale = obstacle._customData["_scale"];
                            flipped.x -= scale.x;
                        }
                        else
                        {
                            flipped.x -= obstacle._width;
                        }
                        obstacle._customData["_position"] = flipped;
                    }
                }

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

                    int newWidth = obstacle._width; //normalize wall width
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
                    obstacle._lineIndex = newIndex;
                }
                else // state > -1000 || state < 1000 assumes no precision width
                {
                    int mirrorLane = (((__state - 2) * -1) + 2); //flip lineIndex
                    obstacle._lineIndex = mirrorLane - obstacle._width; //adjust for wall width
                }
            }
            else if (con is BeatmapNote note)
            {
                if (moveNotes)
                {
                    if (note._customData != null) //Noodle Extensions
                    {
                        if (note._customData.HasKey("_position"))
                        {
                            Vector2 oldPosition = note._customData["_position"];
                            Vector2 flipped = new Vector2(oldPosition.x * -1, oldPosition.y);
                            note._customData["_position"] = flipped;
                        }
                    }
                    else
                    {
                        int __state = note._lineIndex; // flip line index
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

                            note._lineIndex = newIndex;
                        }
                        else
                        {
                            int mirrorLane = (int) (((__state - 1.5f) * -1) + 1.5f);
                            note._lineIndex = mirrorLane;
                        }
                    }
                }

                //flip colors
                if (note._type != BeatmapNote.NOTE_TYPE_BOMB)
                {
                    note._type = note._type == BeatmapNote.NOTE_TYPE_A ? BeatmapNote.NOTE_TYPE_B : BeatmapNote.NOTE_TYPE_A;

                    //flip cut direction horizontally
                    if (moveNotes && CutDirectionToMirrored.ContainsKey(note._cutDirection))
                    {
                        note._cutDirection = CutDirectionToMirrored[note._cutDirection];
                    }
                }
            }
            else if (con is MapEvent e)
            {
                if (e.IsRotationEvent)
                {
                    if (e._customData != null && e._customData.HasKey("_rotation"))
                    {
                        e._customData["_rotation"] = e._customData["_rotation"].AsFloat * -1;
                    }

                    int? rotation = e.GetRotationDegreeFromValue();
                    if (rotation != null)
                    {
                        if (e._value >= 0 && e._value < MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.Length)
                            e._value = MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf((rotation ?? 0) * -1);
                        else if (e._value >= 1000 && e._value <= 1720) //Invert Mapping Extensions rotation
                            e._value = 1720 - (e._value - 1000);
                    }

                    tracksManager?.RefreshTracks();
                } else {
                    if (e._lightGradient != null)
                    {
                        var startColor = e._lightGradient.StartColor;
                        e._lightGradient.StartColor = e._lightGradient.EndColor;
                        e._lightGradient.EndColor = startColor;
                    }
                    if (e.IsUtilityEvent) continue;
                    if (moveNotes && e.IsPropogationEvent && events.EventTypeToPropagate == e._type && events.PropagationEditing == EventsContainer.PropMode.Prop)
                    {
                        var mirroredIdx = events.EventTypePropagationSize - e.PropId - 1;
                        e._customData["_lightID"] = labels.PropIdToLightIdsJ(e._type, mirroredIdx);
                    }
                    else if (moveNotes && e.IsLightIdEvent && events.EventTypeToPropagate == e._type && events.PropagationEditing == EventsContainer.PropMode.Light)
                    {
                        var idx = labels.LightIDToEditor(e._type, e.LightId[0]);
                        var mirroredIdx = events.EventTypePropagationSize - idx - 1;
                        e._customData["_lightID"] = labels.EditorToLightID(e._type, mirroredIdx);
                    }
                    if (e._value > 4 && e._value < 8) e._value -= 4;
                    else if (e._value > 0 && e._value <= 4) e._value += 4;
                }
            }
            allActions.Add(new BeatmapObjectModifiedAction(con, con, original, "e", true));
        }
        foreach (BeatmapObject unique in SelectionController.SelectedObjects.DistinctBy(x => x.beatmapType))
        {
            BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
        }
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects."));
    }
}
