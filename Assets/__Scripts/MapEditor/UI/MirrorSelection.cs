using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CreateEventTypeLabels labels;

    private readonly Dictionary<int, int> cutDirectionToMirrored = new Dictionary<int, int>
    {
        {(int)NoteCutDirection.DownLeft, (int)NoteCutDirection.DownRight},
        {(int)NoteCutDirection.DownRight, (int)NoteCutDirection.DownLeft},
        {(int)NoteCutDirection.UpLeft, (int)NoteCutDirection.UpRight},
        {(int)NoteCutDirection.UpRight, (int)NoteCutDirection.UpLeft},
        {(int)NoteCutDirection.Right, (int)NoteCutDirection.Left},
        {(int)NoteCutDirection.Left, (int)NoteCutDirection.Right}
    };

    public void MirrorTime()
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var ordered = SelectionController.SelectedObjects.OrderByDescending(x => x.JsonTime);
        var orderedSliders = ordered.Where(x => x is BaseSlider);
        var maxTailJsonTime = orderedSliders.Any()
            ? orderedSliders.Max(x => (x as BaseSlider).TailJsonTime)
            : float.MinValue;

        var end = Mathf.Max(ordered.First().JsonTime, maxTailJsonTime);
        var start = ordered.Last().JsonTime;
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var edited = BeatmapFactory.Clone(con);
            edited.JsonTime = start + (end - con.JsonTime);

            if (edited is BaseSlider edittedSlider && con is BaseSlider slider)
            {
                edittedSlider.TailJsonTime = start + (end - slider.TailJsonTime);
                edittedSlider.SwapHeadAndTail();
            }

            allActions.Add(new BeatmapObjectModifiedAction(edited, con, con, "e", true));
        }

        var actionCollection =
            new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects in time.");
        BeatmapActionContainer.AddAction(actionCollection, true);
    }

    public void Mirror(bool moveNotes = true)
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        var allActions = new List<BeatmapAction>();
        foreach (var con in SelectionController.SelectedObjects)
        {
            var original = BeatmapFactory.Clone(con);
            if (con is BaseObstacle obstacle && moveNotes)
            {
                var precisionWidth = obstacle.Width >= 1000;
                var state = obstacle.PosX;

                if (obstacle.CustomCoordinate != null && obstacle.CustomCoordinate.IsArray)
                {
                    var oldPosition = obstacle.CustomCoordinate.ReadVector2();

                    var flipped = new Vector2(oldPosition.x * -1, oldPosition.y);

                    var customSize = obstacle.CustomSize;
                    if (customSize != null && customSize.IsArray && customSize[0].IsNumber)
                    {
                        flipped.x -= customSize[0];
                    }
                    else
                    {
                        flipped.x -= obstacle.Width;
                    }

                    obstacle.CustomCoordinate = flipped;
                }

                if (obstacle.CustomLocalRotation != null)
                {
                    if (obstacle.CustomLocalRotation.IsNumber)
                    {
                        obstacle.CustomLocalRotation = -obstacle.CustomLocalRotation;
                    }
                    else if (obstacle.CustomLocalRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1];
                        }
                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2];
                        }
                    }
                }

                if (obstacle.CustomWorldRotation != null)
                {
                    if (obstacle.CustomWorldRotation.IsNumber)
                    {
                        obstacle.CustomWorldRotation = -obstacle.CustomWorldRotation;
                    }
                    else if (obstacle.CustomWorldRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1];
                        }
                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2];
                        }
                    }
                }

                if (state >= 1000 || state <= -1000 || precisionWidth) // precision lineIndex
                {
                    var newIndex = state;
                    if (newIndex <= -1000) // normalize index values, we'll fix them later
                        newIndex += 1000;
                    else if (newIndex >= 1000)
                        newIndex -= 1000;
                    else
                        newIndex *= 1000; //convert lineIndex to precision if not already
                    newIndex = ((newIndex - 2000) * -1) + 2000; //flip lineIndex

                    var newWidth = obstacle.Width; //normalize wall width
                    if (newWidth < 1000)
                        newWidth *= 1000;
                    else
                        newWidth -= 1000;
                    newIndex -= newWidth;

                    if (newIndex < 0)
                        //this is where we fix them
                        newIndex -= 1000;
                    else
                        newIndex += 1000;
                    obstacle.PosX = newIndex;
                }
                else // state > -1000 || state < 1000 assumes no precision width
                {
                    var mirrorLane = ((state - 2) * -1) + 2; //flip lineIndex
                    obstacle.PosX = mirrorLane - obstacle.Width; //adjust for wall width
                }
            }
            else if (con is BaseNote note)
            {
                if (moveNotes)
                {
                    note.AngleOffset *= -1;

                    // NE Precision rotation
                    if (note.CustomCoordinate != null && note.CustomCoordinate.IsArray)
                    {
                        var oldPosition = note.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        note.CustomCoordinate = flipped;
                    }

                    // NE precision cut direction
                    if (note.CustomDirection != null)
                    {
                        var cutDirection = note.CustomDirection;
                        note.CustomDirection = cutDirection * -1;
                    }

                    if (note.CustomLocalRotation != null)
                    {
                        if (note.CustomLocalRotation.IsNumber)
                        {
                            note.CustomLocalRotation = -note.CustomLocalRotation;
                        }
                        else if (note.CustomLocalRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1];
                            }
                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2];
                            }
                        }
                    }

                    if (note.CustomWorldRotation != null)
                    {
                        if (note.CustomWorldRotation.IsNumber)
                        {
                            note.CustomWorldRotation = -note.CustomWorldRotation;
                        }
                        else if (note.CustomWorldRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1];
                            }
                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2];
                            }
                        }
                    }

                    var state = note.PosX; // flip line index
                    if (state > 3 || state < 0) // precision case
                    {
                        var newIndex = state;
                        if (newIndex <= -1000) // normalize index values, we'll fix them later
                            newIndex += 1000;
                        else if (newIndex >= 1000) newIndex -= 1000;

                        newIndex = ((newIndex - 1500) * -1) + 1500; //flip lineIndex

                        if (newIndex < 0) //this is where we fix them
                            newIndex -= 1000;
                        else
                            newIndex += 1000;

                        note.PosX = newIndex;
                    }
                    else
                    {
                        var mirrorLane = (int)(((state - 1.5f) * -1) + 1.5f);
                        note.PosX = mirrorLane;
                    }
                }

                //flip colors
                if (note.Type != (int)NoteType.Bomb)
                {
                    note.Type = note.Type == (int)NoteType.Red
                        ? (int)NoteType.Blue
                        : (int)NoteType.Red;

                    //flip cut direction horizontally
                    if (moveNotes && cutDirectionToMirrored.ContainsKey(note.CutDirection))
                        note.CutDirection = cutDirectionToMirrored[note.CutDirection];
                }
            }
            else if (con is BaseEvent e)
            {
                if (e.IsLaneRotationEvent())
                {
                    if (e is BaseRotationEvent re)
                    {
                        re.Rotation *= -1;
                    }
                    else
                    {
                        if (e.CustomLaneRotation != null)
                            e.CustomLaneRotation *= -1;

                        var rotation = e.GetRotationDegreeFromValue();
                        if (rotation != null)
                        {
                            if (e.Value >= 0 && e.Value < BaseEvent.LightValueToRotationDegrees.Length)
                                e.Value = BaseEvent.LightValueToRotationDegrees.ToList()
                                    .IndexOf((int)(rotation ?? 0) * -1);
                            else if (e.Value >= 1000 && e.Value <= 1720) //Invert Mapping Extensions rotation
                                e.Value = 1720 - (e.Value - 1000);
                        }
                    }

                    tracksManager.RefreshTracks();
                }
                else
                {
                    if (e.CustomLightGradient != null)
                    {
                        (e.CustomLightGradient.StartColor, e.CustomLightGradient.EndColor) = (e.CustomLightGradient.EndColor, e.CustomLightGradient.StartColor);
                    }

                    if (!e.IsLightEvent()) continue;
                    if (moveNotes && (e.IsPropagation && events.EventTypeToPropagate == e.Type &&
                                      events.PropagationEditing == EventGridContainer.PropMode.Prop))
                    {
                        var mirroredIdx = events.EventTypePropagationSize - (int)e.CustomPropID - 1;
                        e.CustomLightID = labels.PropIdToLightIds(e.Type, mirroredIdx);
                    }
                    else if (moveNotes && (e.CustomLightID != null && events.EventTypeToPropagate == e.Type &&
                                           events.PropagationEditing == EventGridContainer.PropMode.Light))
                    {
                        var idx = labels.LightIDToEditor(e.Type, e.CustomLightID[0]);
                        var mirroredIdx = events.EventTypePropagationSize - idx - 1;
                        e.CustomLightID = new[] { labels.EditorToLightID(e.Type, mirroredIdx) };
                    }

                    // (M) swaps red and blue
                    // (Shift + M) cycles red, blue, and white 
                    if (moveNotes)
                    {
                        if (e.Value > 0 && e.Value <= 4) e.Value += 4; // blue to red
                        else if (e.Value > 4 && e.Value <= 8) e.Value -= 4; // red to blue
                    }
                    else
                    {
                        if (e.Value > 0 && e.Value <= 4) e.Value += 4; // blue to red
                        else if (e.Value > 4 && e.Value <= 8) e.Value += 4; // red to white
                        else if (e.Value > 8 && e.Value <= 12) e.Value -= 8; // white to blue
                    }
                }
            }
            else if (con is BaseArc arc)
            {
                if (moveNotes)
                {
                    if (arc.CustomCoordinate != null && arc.CustomCoordinate.IsArray)
                    {
                        var oldPosition = arc.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        arc.CustomCoordinate = flipped;
                    }

                    if (arc.CustomTailCoordinate != null && arc.CustomTailCoordinate.IsArray)
                    {
                        var oldPosition = arc.CustomTailCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        arc.CustomTailCoordinate = flipped;
                    }

                    arc.PosX = Mathf.RoundToInt(((arc.PosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(arc.CutDirection))
                        arc.CutDirection = cutDirectionToMirrored[arc.CutDirection];

                    arc.TailPosX = Mathf.RoundToInt(((arc.TailPosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(arc.TailCutDirection))
                        arc.TailCutDirection = cutDirectionToMirrored[arc.TailCutDirection];

                    if (arc.MidAnchorMode > 0 && arc.MidAnchorMode < 3)
                    {
                        arc.MidAnchorMode = arc.MidAnchorMode == (int)SliderMidAnchorMode.Clockwise ? (int)SliderMidAnchorMode.CounterClockwise : (int)SliderMidAnchorMode.Clockwise;
                    }
                }
                arc.Color = arc.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;

            }
            else if (con is BaseChain chain)
            {
                if (moveNotes)
                {
                    // NE Precision rotation
                    if (chain.CustomCoordinate != null && chain.CustomCoordinate.IsArray)
                    {
                        var oldPosition = chain.CustomCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        chain.CustomCoordinate = flipped;
                    }

                    if (chain.CustomTailCoordinate != null && chain.CustomTailCoordinate.IsArray)
                    {
                        var oldPosition = chain.CustomTailCoordinate.ReadVector2();
                        var flipped = new Vector2(((oldPosition.x + 0.5f) * -1) - 0.5f, oldPosition.y);
                        chain.CustomTailCoordinate = flipped;
                    }

                    chain.PosX = Mathf.RoundToInt(((chain.PosX - 1.5f) * -1) + 1.5f);
                    if (cutDirectionToMirrored.ContainsKey(chain.CutDirection))
                        chain.CutDirection = cutDirectionToMirrored[chain.CutDirection];

                    chain.TailPosX = Mathf.RoundToInt(((chain.TailPosX - 1.5f) * -1) + 1.5f);
                }
                chain.Color = chain.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;
            }

            allActions.Add(new BeatmapObjectModifiedAction(con, con, original, "e", true));
        }

        foreach (var unique in SelectionController.SelectedObjects.DistinctBy(x => x.ObjectType))
            BeatmapObjectContainerCollection.GetCollectionForType(unique.ObjectType).RefreshPool(true);
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true,
            "Mirrored a selection of objects."));
    }
}
