using System.Collections.Generic;
using System.Linq;
using ZLinq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;

public class MirrorSelection : MonoBehaviour
{
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private PlacementLaneController placementLaneController;

    private readonly Dictionary<int, int> cutDirectionToMirrored = new()
    {
        { (int)NoteCutDirection.DownLeft, (int)NoteCutDirection.DownRight },
        { (int)NoteCutDirection.DownRight, (int)NoteCutDirection.DownLeft },
        { (int)NoteCutDirection.UpLeft, (int)NoteCutDirection.UpRight },
        { (int)NoteCutDirection.UpRight, (int)NoteCutDirection.UpLeft },
        { (int)NoteCutDirection.Right, (int)NoteCutDirection.Left },
        { (int)NoteCutDirection.Left, (int)NoteCutDirection.Right }
    };

    // Read the active physical grid once per mirror operation; gameplay positions mirror arithmetically across it.
    private int GetLaneCount()
    {
        var laneCount = placementLaneController.LaneCount;
        return laneCount > 0 ? laneCount : 4;
    }

    // Use one ordered mapping algorithm for every physical lane domain.
    private static Dictionary<int, int> BuildMirrorMap(IEnumerable<int> lanes)
    {
        var selectedLanes = lanes.AsValueEnumerable().Distinct().OrderBy(lane => lane).ToList();
        return BuildOrderedMirrorMap(selectedLanes);
    }

    // Preserve caller-defined lane ordering when a domain's IDs do not match its visual order.
    private static Dictionary<int, int> BuildOrderedMirrorMap(IReadOnlyList<int> selectedLanes)
    {
        var laneMirrorMap = new Dictionary<int, int>(selectedLanes.Count);
        for (var index = 0; index < selectedLanes.Count; index++)
        {
            laneMirrorMap[selectedLanes[index]] = selectedLanes[selectedLanes.Count - 1 - index];
        }

        return laneMirrorMap;
    }

    // Gather every standard grid lane touched by the selected object so sparse selections can mirror in-place.
    private static IEnumerable<int> GetSelectedLaneIndices(BaseObject obj, int laneCount)
    {
        switch (obj)
        {
            case BaseNote note when note.PosX >= 0 && note.PosX < laneCount:
                yield return note.PosX;
                yield break;
            case BaseArc arc:
                if (arc.PosX >= 0 && arc.PosX < laneCount)
                {
                    yield return arc.PosX;
                }

                if (arc.TailPosX >= 0 && arc.TailPosX < laneCount)
                {
                    yield return arc.TailPosX;
                }

                yield break;
            case BaseChain chain:
                if (chain.PosX >= 0 && chain.PosX < laneCount)
                {
                    yield return chain.PosX;
                }

                if (chain.TailPosX >= 0 && chain.TailPosX < laneCount)
                {
                    yield return chain.TailPosX;
                }

                yield break;
            case BaseGLSEvent glsEvent when glsEvent.BoxIndex >= 0:
                yield return glsEvent.BoxIndex;
                yield break;
            case BaseObstacle obstacle when obstacle.PosX >= 0 && obstacle.PosX < laneCount && obstacle.Width > 0:
                for (var lane = obstacle.PosX; lane < obstacle.PosX + obstacle.Width && lane < laneCount; lane++)
                {
                    yield return lane;
                }

                yield break;
        }
    }

    // Do not move a lane outside the selected mirror domain.
    private static int MirrorLane(int lane, IReadOnlyDictionary<int, int> selectedLaneMirrorMap) =>
        selectedLaneMirrorMap.TryGetValue(lane, out var mirroredLane)
            ? mirroredLane : lane;

    // Standard lane indices are zero-based, so their physical reflection is LaneCount - 1 - currentPosition.
    private static int MirrorGameplayLane(int lane, int laneCount) => laneCount - 1 - lane;

    // Preserve a wall's width while reflecting its left edge across the loaded physical grid.
    private static int MirrorObstacleLane(BaseObstacle obstacle, int laneCount) =>
        laneCount - obstacle.PosX - obstacle.Width;

    // Mirror basic-event lanes within the selected light types instead of across every environment light type.
    private Dictionary<int, int> BuildSelectedBasicEventTypeMirrorMap()
    {
        var selectedTypes = SelectionController.SelectedObjects.AsValueEnumerable()
            .OfType<BaseEvent>()
            .Where(evt => beatmapRuntimeContext.TracksDefinition.GetBasicOrDefault(evt.Type).Kind == BasicEventKind.Lights)
            .Select(evt => evt.Type)
            .Distinct()
            .OrderBy(labels.EventTypeToLaneId)
            .ToList();

        return BuildOrderedMirrorMap(selectedTypes);
    }

    // Build an in-place mirror map from the visible light-ID lanes selected for one event type.
    private Dictionary<int, int> BuildSelectedLightIdLaneMirrorMap(int eventType)
    {
        var selectedLanes = SelectionController.SelectedObjects.AsValueEnumerable()
            .OfType<BaseEvent>()
            .Where(evt => evt.Type == eventType && evt.CustomLightID != null)
            .Select(evt => labels.LightIDsToVisibleLane(eventType, evt.CustomLightID))
            .Where(lane => lane >= 0)
            .Distinct()
            .OrderBy(lane => lane)
            .ToList();

        return BuildMirrorMap(selectedLanes);
    }

    // Build an in-place mirror map from the selected propagation groups for one event type.
    private Dictionary<int, int> BuildSelectedPropagationMirrorMap(int eventType)
    {
        var selectedGroups = SelectionController.SelectedObjects.AsValueEnumerable()
            .OfType<BaseEvent>()
            .Where(evt => evt.Type == eventType && evt.CustomLightID != null)
            .Select(evt => labels.LightIDsToPropID(eventType, evt.CustomLightID))
            .Where(group => group >= 0)
            .Distinct()
            .OrderBy(group => group)
            .ToList();

        return BuildMirrorMap(selectedGroups);
    }

    private void MirrorEventBoxGroupPositions(BaseEventBoxGroup group) => MirrorEventBoxGroupPositions(group.ReadOnlyBoxes);

    // Change filters instead of swapping boxes because events retain their BoxIndex and EventBoxData references.
    private void MirrorEventBoxGroupPositions(IReadOnlyList<BaseEventBox> boxes)
    {
        if (boxes.Count <= 1) return;

        // Every filter in a selected outer GLS group is part of its selected physical lane domain.
        var selectedLaneMirrorMap = BuildMirrorMap(
            boxes.AsValueEnumerable().Select(box => GetIndexFilterLane(box.IndexFilter)).Where(lane => lane >= 0).ToList());
        foreach (var box in boxes)
        {
            MirrorIndexFilter(box.IndexFilter, selectedLaneMirrorMap);
        }
    }

    // Extract the lane-bearing filter parameter used by the two supported GLS filter modes.
    private static int GetIndexFilterLane(BaseIndexFilter filter) => filter?.Type switch
    {
        (int)IndexFilterType.Division => filter.Param1,
        (int)IndexFilterType.StepAndOffset => filter.Param0,
        _ => -1
    };

    private static void MirrorIndexFilter(BaseIndexFilter filter, IReadOnlyDictionary<int, int> selectedLaneMirrorMap)
    {
        if (filter == null) return;

        // Mirror the ID based on the filter type
        // For Division: Param1 is the ID (0-indexed)
        // For StepAndOffset: Param0 is the ID (0-indexed)
        int id;
        if (filter.Type == (int)IndexFilterType.Division)
        {
            id = filter.Param1;
            filter.Param1 = MirrorLane(id, selectedLaneMirrorMap);
        }
        else if (filter.Type == (int)IndexFilterType.StepAndOffset)
        {
            id = filter.Param0;
            filter.Param0 = MirrorLane(id, selectedLaneMirrorMap);
        }
    }

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

            // Use the current update action so moving selected objects cannot leave stale state or ghost entries.
            allActions.Add(new BeatmapObjectUpdatedAction(edited, con, "e", true));
        }

        var actionCollection =
            new ActionCollectionAction(allActions, true, true, "Mirrored a selection of objects in time.");
        BeatmapActionContainer.AddAction(actionCollection, true);
    }

    // Rebuild each affected GLS group once so replay does not spawn individual nodes through ReplaceGroup.
    private List<BeatmapAction> CreateMirroredGlsActions(
        bool moveNotes,
        List<BaseGLSEvent> mirroredSelectedGlsEvents,
        Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>> selectedGroups)
    {
        var actions = new List<BeatmapAction>();
        // Group and index source nodes once so mirroring keeps selected lanes aligned with cloned event arrays.
        foreach (var groupEntry in selectedGroups)
        {
            var originalGroup = groupEntry.Key;
            var groupEvents = groupEntry.Value;
            var editedGroup = BeatmapFactory.Clone(originalGroup);
            var sourceIndex = new GLSEventLookupIndex(originalGroup);
            var editedEventsByBox = new List<BaseGLSEvent>[editedGroup.ReadOnlyBoxes.Count];
            for (var boxIndex = 0; boxIndex < editedGroup.ReadOnlyBoxes.Count; boxIndex++)
            {
                var events = editedGroup.ReadOnlyBoxes[boxIndex].ReadOnlyEvents;
                var copiedEvents = new List<BaseGLSEvent>(events.Count);
                for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    copiedEvents.Add(events[eventIndex]);
                }

                editedEventsByBox[boxIndex] = copiedEvents;
            }

            int laneCount = editedEventsByBox.Length;
            var selectedEvents = new List<(int SourceBox, BaseGLSEvent EditedEvent)>(groupEvents.Count);
            foreach (var originalEvent in groupEvents)
            {
                if (!sourceIndex.TryGetCloneEvent(
                        originalEvent,
                        editedGroup,
                        out var location,
                        out var editedEvent)
                    || location.BoxIndex >= laneCount)
                {
                    continue;
                }

                selectedEvents.Add((location.BoxIndex, editedEvent));
            }

            // Mirror GLS inner nodes only among selected box indices in their own parent group.
            var selectedLanes = new List<int>(selectedEvents.Count);
            foreach (var selectedEvent in selectedEvents)
            {
                selectedLanes.Add(selectedEvent.SourceBox);
            }

            var selectedLaneMirrorMap = BuildMirrorMap(selectedLanes);

            foreach (var (sourceBox, editedEvent) in selectedEvents)
            {
                editedEventsByBox[sourceBox].Remove(editedEvent);
                int destinationIndex = moveNotes ? MirrorLane(sourceBox, selectedLaneMirrorMap) : sourceBox;
                editedEventsByBox[destinationIndex].Add(editedEvent);

                if (editedEvent is BaseLightColorBase colorEvent)
                {
                    colorEvent.Color = (colorEvent.Color + 1) % 3;
                }

                if (editedEvent is BaseLightRotationBase rotationEvent)
                {
                    // Physical lane mirroring already supplies the reflection; do not invert GLS rotation as well.
                    if (!moveNotes) rotationEvent.Rotation *= -1f;
                }

                mirroredSelectedGlsEvents.Add(editedEvent);
            }

            for (var boxIndex = 0; boxIndex < editedGroup.ReadOnlyBoxes.Count; boxIndex++)
            {
                var box = editedGroup.ReadOnlyBoxes[boxIndex];
                box.SetEvents(editedEventsByBox[boxIndex].ToArray());
                foreach (var evt in box.ReadOnlyEvents)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = editedGroup;
                    evt.BoxIndex = boxIndex;
                    evt.JsonTime = editedGroup.JsonTime + evt.RelativeJsonTime;
                }
            }

            // Mirroring rewrites box event arrays, so refresh ordering once here instead of forcing every preview repaint to scan them.
            editedGroup.ResortOrderedEvents();
            editedGroup.SaveCustom();
            actions.Add(new BeatmapGLSEventBoxModifiedAction(
                editedGroup,
                originalGroup,
                "Mirrored GLS events."));
        }

        return actions;
    }

    public void Mirror(bool moveNotes = true)
    {
        if (!SelectionController.HasSelectedObjects())
        {
            PersistentUI.Instance.DisplayMessage("Mapper", "mirror.error", PersistentUI.DisplayMessageType.Bottom);
            return;
        }

        // Reuse one loaded-grid map so every standard note and wall mirrors across the same physical domain.
        var laneCount = GetLaneCount();
        var noteMirrorCenter = (laneCount - 1) * 500;
        var obstacleMirrorCenter = laneCount * 500;
        var selectedLaneMirrorMap = BuildMirrorMap(
            SelectionController.SelectedObjects.AsValueEnumerable().SelectMany(obj => GetSelectedLaneIndices(obj, laneCount)).ToList());
        // Keep the basic-event lane maps separate because their lanes are defined by the active event-grid mode.
        var selectedBasicEventTypeMirrorMap = BuildSelectedBasicEventTypeMirrorMap();
        var mirroredSelectedGlsEvents = new List<BaseGLSEvent>();
        // Inner GLS nodes own their parent replacement, so do not also mirror a parent that is selected beside them.
        var selectedGlsGroups = GLSEventLookupIndex.GroupSelectedEvents(SelectionController.SelectedObjects);
        var glsActions = CreateMirroredGlsActions(moveNotes, mirroredSelectedGlsEvents, selectedGlsGroups);
        var events = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        var originalObjects = new List<BaseObject>();
        var editedObjects = new List<BaseObject>();
        foreach (var original in SelectionController.SelectedObjects.Where(obj =>
                     obj is not BaseGLSEvent
                     && (obj is not BaseEventBoxGroup eventBoxGroup || !selectedGlsGroups.ContainsKey(eventBoxGroup))))
        {
            var edited = BeatmapFactory.Clone(original);
            if (edited is BaseObstacle obstacle && moveNotes)
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
                        flipped.x -= customSize[0].AsFloat;
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
                        obstacle.CustomLocalRotation = -obstacle.CustomLocalRotation.AsFloat;
                    }
                    else if (obstacle.CustomLocalRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1].AsFloat;
                        }

                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2].AsFloat;
                        }
                    }
                }

                if (obstacle.CustomWorldRotation != null)
                {
                    if (obstacle.CustomWorldRotation.IsNumber)
                    {
                        obstacle.CustomWorldRotation = -obstacle.CustomWorldRotation.AsFloat;
                    }
                    else if (obstacle.CustomWorldRotation is JSONArray rot)
                    {
                        if (rot.Count > 1)
                        {
                            rot[1] = -rot[1].AsFloat;
                        }

                        if (rot.Count > 2)
                        {
                            rot[2] = -rot[2].AsFloat;
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
                    newIndex = ((newIndex - obstacleMirrorCenter) * -1) + obstacleMirrorCenter; //flip lineIndex

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
                    obstacle.PosX = MirrorObstacleLane(obstacle, laneCount);
                }
            }
            else if (edited is BaseNote note)
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
                            note.CustomLocalRotation = -note.CustomLocalRotation.AsFloat;
                        }
                        else if (note.CustomLocalRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1].AsFloat;
                            }

                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2].AsFloat;
                            }
                        }
                    }

                    if (note.CustomWorldRotation != null)
                    {
                        if (note.CustomWorldRotation.IsNumber)
                        {
                            note.CustomWorldRotation = -note.CustomWorldRotation.AsFloat;
                        }
                        else if (note.CustomWorldRotation is JSONArray rot)
                        {
                            if (rot.Count > 1)
                            {
                                rot[1] = -rot[1].AsFloat;
                            }

                            if (rot.Count > 2)
                            {
                                rot[2] = -rot[2].AsFloat;
                            }
                        }
                    }

                    var state = note.PosX; // flip line index
                    if (state >= 1000 || state <= -1000) // precision case; ordinary out-of-grid lane indices still use physical arithmetic
                    {
                        var newIndex = state;
                        if (newIndex <= -1000) // normalize index values, we'll fix them later
                            newIndex += 1000;
                        else if (newIndex >= 1000) newIndex -= 1000;

                        newIndex = ((newIndex - noteMirrorCenter) * -1) + noteMirrorCenter; //flip lineIndex

                        if (newIndex < 0) //this is where we fix them
                            newIndex -= 1000;
                        else
                            newIndex += 1000;

                        note.PosX = newIndex;
                    }
                    else
                    {
                        // Notes always use the loaded physical grid; selection-scoped maps are only for lane-ID domains.
                        var mirrorLane = MirrorGameplayLane(state, laneCount);
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
            else if (edited is BaseEvent e)
            {
                // Ring rotation and zoom use value inversion only when no physical lane mirror is requested.
                // Read current environment metadata directly so mirroring cannot retain stale track capabilities.
                var components = beatmapRuntimeContext.TracksDefinition.GetBasicOrDefault(e.Type).Components;
                var isRingRotation = components.HasFlag(BasicEventComponent.RingRotation);
                // SmoothStepRingZoom only applies to The Second's legacy ring right now.
                var isRingZoom = components.HasFlag(BasicEventComponent.RingZoom)
                    || components.HasFlag(BasicEventComponent.SmoothStepRingZoom);
                if (isRingRotation || isRingZoom)
                {
                    if (!moveNotes)
                    {
                        if (isRingRotation && e.CustomRingRotation.HasValue)
                            e.CustomRingRotation = -e.CustomRingRotation.Value;
                        else if (isRingZoom && e.CustomStep.HasValue)
                            e.CustomStep = -e.CustomStep.Value;
                    }

                    continue;
                }

                // Track whether this event has a physical lane counterpart before deciding whether color inversion applies.
                var physicallyMirroringEvent = false;
                // In the normal basic-event view, mirror the event's visible lane by changing its event type.
                if (moveNotes && events.PropagationEditing == EventGridContainer.PropMode.Off)
                {
                    physicallyMirroringEvent = selectedBasicEventTypeMirrorMap.Count > 1;
                    if (selectedBasicEventTypeMirrorMap.TryGetValue(e.Type, out var mirroredType))
                    {
                        e.Type = mirroredType;
                    }
                }

                if (beatmapRuntimeContext.TracksDefinition.GetBasicOrDefault(e.Type).Kind != BasicEventKind.Lights)
                {
                    continue;
                }
                if (moveNotes
                    && e.IsPropagation
                    && e.CustomLightID != null
                    && events.EventTypeToPropagate == e.Type
                    && events.PropagationEditing == EventGridContainer.PropMode.Prop)
                {
                    var idx = labels.LightIDsToPropID(e.Type, e.CustomLightID);
                    var selectedPropagationMirrorMap = BuildSelectedPropagationMirrorMap(e.Type);
                    physicallyMirroringEvent = selectedPropagationMirrorMap.Count > 1;
                    // Preserve the authored propagation IDs when the selected domain has no physical counterpart.
                    if (selectedPropagationMirrorMap.Count > 1
                        && selectedPropagationMirrorMap.TryGetValue(idx, out var mirroredIdx))
                    {
                        e.CustomLightID = labels.PropIdToLightIds(e.Type, mirroredIdx);
                    }
                }
                // Keep event color behavior independent of lane movement so single-lane and physical mirrors agree.
                if (moveNotes && e.CustomLightID != null && events.PropagationEditing == EventGridContainer.PropMode.Light)
                {
                    var idx = labels.LightIDsToVisibleLane(e.Type, e.CustomLightID);
                    var selectedLightIdLaneMirrorMap = BuildSelectedLightIdLaneMirrorMap(e.Type);
                    physicallyMirroringEvent = selectedLightIdLaneMirrorMap.Count > 1;
                    // Preserve multi-ID light selections when their single visible lane mirrors to itself.
                    if (selectedLightIdLaneMirrorMap.Count > 1
                        && selectedLightIdLaneMirrorMap.TryGetValue(idx, out var mirroredIdx))
                    {
                        var mirroredId = labels.LaneToLightID(e.Type, mirroredIdx);
                        if (mirroredId >= 0)
                        {
                            e.CustomLightID = new[] { mirroredId };
                        }
                    }
                }
                // Only an invert operation swaps gradient colors; physical lane mirroring preserves the authored color.
                if (!physicallyMirroringEvent && e.CustomLightGradient != null)
                {
                    (e.CustomLightGradient.StartColor, e.CustomLightGradient.EndColor) =
                        (e.CustomLightGradient.EndColor, e.CustomLightGradient.StartColor);
                }

                // A single-node Mirror uses the legacy red/blue inversion; physical mirrors preserve color, while explicit invert cycles all three colors.
                if (moveNotes && !physicallyMirroringEvent)
                {
                    if (e.Value > 0 && e.Value <= 4) e.Value += 4;
                    else if (e.Value > 4 && e.Value <= 8) e.Value -= 4;
                }
                else if (!moveNotes)
                {
                    if (e.Value > 0 && e.Value <= 4) e.Value += 4;
                    else if (e.Value > 4 && e.Value <= 8) e.Value += 4;
                    else if (e.Value > 8 && e.Value <= 12) e.Value -= 8;
                }
            }
            else if (edited is BaseRotationEvent r)
            {
                r.Rotation *= -1;
                tracksManager.RefreshTracks();
            }
            else if (edited is BaseArc arc)
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

                    arc.PosX = MirrorGameplayLane(arc.PosX, laneCount);
                    if (cutDirectionToMirrored.ContainsKey(arc.CutDirection))
                        arc.CutDirection = cutDirectionToMirrored[arc.CutDirection];

                    arc.TailPosX = MirrorGameplayLane(arc.TailPosX, laneCount);
                    if (cutDirectionToMirrored.ContainsKey(arc.TailCutDirection))
                        arc.TailCutDirection = cutDirectionToMirrored[arc.TailCutDirection];

                    if (arc.MidAnchorMode > 0 && arc.MidAnchorMode < 3)
                    {
                        arc.MidAnchorMode = arc.MidAnchorMode == (int)SliderMidAnchorMode.Clockwise
                            ? (int)SliderMidAnchorMode.CounterClockwise
                            : (int)SliderMidAnchorMode.Clockwise;
                    }
                }

                arc.Color = arc.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;
            }
            else if (edited is BaseChain chain)
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

                    chain.PosX = MirrorGameplayLane(chain.PosX, laneCount);
                    if (cutDirectionToMirrored.ContainsKey(chain.CutDirection))
                        chain.CutDirection = cutDirectionToMirrored[chain.CutDirection];

                    chain.TailPosX = MirrorGameplayLane(chain.TailPosX, laneCount);
                }

                chain.Color = chain.Color == (int)NoteType.Red
                    ? (int)NoteType.Blue
                    : (int)NoteType.Red;
            }
            // Mirror selected GLS inner nodes by changing their lane index instead of moving box objects.
            else if (edited is BaseGLSEvent glsEvent && original is BaseGLSEvent originalGlsEvent && moveNotes)
            {
                // Keep the GLS group width distinct from the loaded gameplay lane count in the enclosing mirror operation.
                int glsLaneCount = originalGlsEvent.EventBoxGroupData?.ReadOnlyBoxes.Count ?? 0;
                if (glsLaneCount > 1 && originalGlsEvent.BoxIndex >= 0 && originalGlsEvent.BoxIndex < glsLaneCount)
                {
                    glsEvent.BoxIndex = MirrorLane(originalGlsEvent.BoxIndex, selectedLaneMirrorMap);
                }
            }
            else if (edited is BaseLightColorEventBoxGroup lcebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(lcebg);
                // Cycle colors (red/blue/white)
                foreach (var evt in lcebg.Boxes.SelectMany(box => box.Events)) evt.Color = (evt.Color + 1) % 3;
            }
            else if (edited is BaseLightRotationEventBoxGroup lrebg)
            {
                // Mirror the box positions within the group and invert every rotation node for horizontal mirroring.
                MirrorEventBoxGroupPositions(lrebg);
                foreach (var evt in lrebg.Boxes.SelectMany(box => box.Events))
                {
                    evt.Rotation *= -1f;
                }
            }
            else if (edited is BaseLightTranslationEventBoxGroup ltebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(ltebg);
            }
            else if (edited is BaseVfxEventEventBoxGroup ffebg)
            {
                // Mirror the box positions within the group (swap lane indices)
                MirrorEventBoxGroupPositions(ffebg);
            }

            edited.SaveCustom();

            editedObjects.Add(edited);
            originalObjects.Add(original);
        }

        // Keep GLS group actions separate from ordinary object collection replacement to avoid nested GLS spawns.
        var actions = new List<BeatmapAction>(glsActions);
        if (editedObjects.Count > 0)
        {
            actions.Add(new BeatmapObjectModifiedCollectionAction(
                editedObjects,
                originalObjects,
                "Mirrored a selection of objects."));
        }

        if (actions.Count > 0)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(actions, true, true, "Mirrored a selection of objects."),
                true);
        }

        // Parent replacement recreates child identities, so rebind mirror selection to the authoritative inner nodes.
        if (mirroredSelectedGlsEvents.Count > 0)
        {
            BeatmapObjectContainerCollection
                .GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent)
                .RebindSelectionAfterBatch(mirroredSelectedGlsEvents);
        }
    }
}
