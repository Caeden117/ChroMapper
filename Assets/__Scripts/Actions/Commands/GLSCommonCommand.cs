using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSCommonCommand
{
    // Apply the dedicated authored axis action without inspecting or overlapping physical modifier state.
    public static void CycleEventAxis(InputAction.CallbackContext context, BaseGLSEvent evt)
    {
        if (!context.performed || evt == null)
            return;

        var direction = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (direction == 0)
            return;

        switch (evt)
        {
            case BaseLightRotationBase rotation:
                CycleRotationEventAxis(rotation, direction);
                break;
            case BaseLightTranslationBase translation:
                CycleTranslationEventAxis(translation, direction);
                break;
        }
    }

    // Move a rotation event between axis tracks while preserving every sibling on the source track.
    private static void CycleRotationEventAxis(BaseLightRotationBase evt, int direction)
    {
        if (evt.EventBoxGroupData is not BaseLightRotationEventBoxGroup originalGroup
            || evt.EventBoxData is not BaseLightRotationEventBox
            || !TryFindEventIndex(evt, out var eventIndex))
        {
            Debug.LogError("[GLSAxisScroll] Rotation event has invalid group or box ownership.");
            return;
        }

        // Don't shift to an axis with something already at this beat.
        if (!TryFindOpenAxis(originalGroup.Boxes, evt.BoxIndex, evt.RelativeJsonTime, direction, static box => box.Axis, out var targetAxis))
        {
            WarnNoOpenAxis();
            return;
        }

        var editedGroup = BeatmapFactory.Clone(originalGroup);
        MoveEventToAxisTrack(
            editedGroup.Boxes,
            evt.BoxIndex,
            eventIndex,
            targetAxis,
            static box => box.Axis,
            static (box, axis) => box.Axis = axis);

        RebindGroup(editedGroup);
        TriggerModifyEventBoxAction(originalGroup, editedGroup, ActionMergeType.ModifyGLSEventAxis);
    }

    // Move a translation event between axis tracks while preserving every sibling on the source track.
    private static void CycleTranslationEventAxis(BaseLightTranslationBase evt, int direction)
    {
        if (evt.EventBoxGroupData is not BaseLightTranslationEventBoxGroup originalGroup
            || evt.EventBoxData is not BaseLightTranslationEventBox
            || !TryFindEventIndex(evt, out var eventIndex))
        {
            Debug.LogError("[GLSAxisScroll] Translation event has invalid group or box ownership.");
            return;
        }

        // Translation uses the same same-beat occupancy search as rotation, including reverse wraparound.
        if (!TryFindOpenAxis(originalGroup.Boxes, evt.BoxIndex, evt.RelativeJsonTime, direction, static box => box.Axis, out var targetAxis))
        {
            WarnNoOpenAxis();
            return;
        }

        var editedGroup = BeatmapFactory.Clone(originalGroup);
        MoveEventToAxisTrack(
            editedGroup.Boxes,
            evt.BoxIndex,
            eventIndex,
            targetAxis,
            static box => box.Axis,
            static (box, axis) => box.Axis = axis);

        RebindGroup(editedGroup);
        TriggerModifyEventBoxAction(originalGroup, editedGroup, ActionMergeType.ModifyGLSEventAxis);
    }

    // Treat an axis as occupied when any of its filter lanes already owns a node at the moving node's relative beat.
    private static bool TryFindOpenAxis<TBox>(
        IReadOnlyList<TBox> boxes,
        int sourceBoxIndex,
        float relativeJsonTime,
        int direction,
        Func<TBox, int> getAxis,
        out int targetAxis)
        where TBox : BaseEventBox
    {
        var sourceAxis = getAxis(boxes[sourceBoxIndex]);
        var axisDirection = Math.Sign(direction);
        for (var axisOffset = 1; axisOffset < 3; axisOffset++)
        {
            var candidateAxis = (sourceAxis + (axisDirection * axisOffset) + 6) % 3;
            var occupied = false;
            for (var boxIndex = 0; boxIndex < boxes.Count && !occupied; boxIndex++)
            {
                if (getAxis(boxes[boxIndex]) != candidateAxis)
                {
                    continue;
                }

                // AxisScrollSearchesSortedDestinationEventsWithoutLinearEnumeration covers this wheel-input hot path.
                // SetEvents keeps each lane chronological, so binary search avoids walking every node on every pulse.
                occupied = ContainsEventAtRelativeTime(boxes[boxIndex].ReadOnlyEvents, relativeJsonTime);
            }

            if (!occupied)
            {
                targetAxis = candidateAxis;
                return true;
            }
        }

        targetAxis = sourceAxis;
        return false;
    }

    // AxisScrollSearchesSortedDestinationEventsWithoutLinearEnumeration protects the wheel-input lookup from linear scans.
    // Comparing at each midpoint against the existing epsilon preserves same-beat occupancy semantics on sorted lane data.
    private static bool ContainsEventAtRelativeTime(IReadOnlyList<BaseGLSEvent> events, float relativeJsonTime)
    {
        var lowerIndex = 0;
        var upperIndex = events.Count - 1;
        while (lowerIndex <= upperIndex)
        {
            var middleIndex = lowerIndex + ((upperIndex - lowerIndex) / 2);
            var difference = events[middleIndex].RelativeJsonTime - relativeJsonTime;
            if (Math.Abs(difference) < BeatmapObjectContainerCollection.Epsilon)
            {
                return true;
            }

            if (difference < 0)
            {
                lowerIndex = middleIndex + 1;
            }
            else
            {
                upperIndex = middleIndex - 1;
            }
        }

        return false;
    }

    // Use the existing fading bottom notification and retain a diagnostic warning when every other axis is occupied.
    private static void WarnNoOpenAxis()
    {
        const string message = "No open axis to shift to on this beat.";
        Debug.LogWarning($"[GLSAxisScroll] {message}");
        PersistentUI.Instance.DisplayMessage(message, PersistentUI.DisplayMessageType.Bottom);
    }

    // Reuse an existing destination-axis track, creating one only when the destination axis does not exist.
    private static (bool createdDestination, bool removedSource) MoveEventToAxisTrack<TBox>(
        List<TBox> boxes,
        int sourceBoxIndex,
        int eventIndex,
        int targetAxis,
        Func<TBox, int> getAxis,
        Action<TBox, int> setAxis)
        where TBox : BaseEventBox
    {
        var sourceBox = boxes[sourceBoxIndex];
        var movedEvent = sourceBox.ReadOnlyEvents[eventIndex];
        TBox targetBox = null;
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            if (boxIndex != sourceBoxIndex && getAxis(boxes[boxIndex]) == targetAxis)
            {
                targetBox = boxes[boxIndex];
                break;
            }
        }

        var createdDestination = targetBox == null;
        if (createdDestination)
        {
            // Clone track configuration only when the destination axis does not exist yet.
            targetBox = BeatmapFactory.Clone(sourceBox);
            setAxis(targetBox, targetAxis);
            targetBox.SetEvents(new BaseGLSEvent[] { movedEvent });
        }
        else
        {
            // Insert after existing equal-time events so their JSON order remains deterministic; Array.Sort is unstable on ties.
            var insertionIndex = 0;
            while (insertionIndex < targetBox.ReadOnlyEvents.Count
                   && targetBox.ReadOnlyEvents[insertionIndex].RelativeJsonTime <= movedEvent.RelativeJsonTime)
            {
                insertionIndex++;
            }

            var targetEvents = new BaseGLSEvent[targetBox.ReadOnlyEvents.Count + 1];
            for (var i = 0; i < insertionIndex; i++)
                targetEvents[i] = targetBox.ReadOnlyEvents[i];
            targetEvents[insertionIndex] = movedEvent;
            for (var i = insertionIndex; i < targetBox.ReadOnlyEvents.Count; i++)
                targetEvents[i + 1] = targetBox.ReadOnlyEvents[i];
            targetBox.SetEvents(targetEvents);
        }

        var remainingEvents = new BaseGLSEvent[sourceBox.ReadOnlyEvents.Count - 1];
        var destination = 0;
        for (var i = 0; i < sourceBox.ReadOnlyEvents.Count; i++)
        {
            if (i != eventIndex)
                remainingEvents[destination++] = sourceBox.ReadOnlyEvents[i];
        }

        sourceBox.SetEvents(remainingEvents);
        var removedSource = sourceBox.ReadOnlyEvents.Count == 0;
        // Vacated source tracks disappear before the newly-created destination track is appended.
        if (removedSource)
            boxes.RemoveAt(sourceBoxIndex);
        if (createdDestination)
            boxes.Add(targetBox);
        // Keep event-box tracks in stable X/Y/Z order regardless of which direction created the destination.
        SortAxisTracks(boxes, getAxis);
        return (createdDestination, removedSource);
    }

    // Preserve relative order within each axis while normalizing the three axis groups.
    internal static void SortAxisTracks<TBox>(List<TBox> boxes, Func<TBox, int> getAxis)
    {
        var orderedBoxes = new List<TBox>(boxes.Count);
        for (var axis = (int)Axis.X; axis <= (int)Axis.Z; axis++)
        {
            for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                if (getAxis(boxes[boxIndex]) == axis)
                    orderedBoxes.Add(boxes[boxIndex]);
            }
        }

        // Preserve malformed or future axis values after the supported X/Y/Z tracks instead of dropping data.
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var axis = getAxis(boxes[boxIndex]);
            if (axis < (int)Axis.X || axis > (int)Axis.Z)
                orderedBoxes.Add(boxes[boxIndex]);
        }

        boxes.Clear();
        boxes.AddRange(orderedBoxes);
    }

    internal static void SortAxisTracks(BaseEventBoxGroup group)
    {
        switch (group)
        {
            case BaseLightRotationEventBoxGroup rotationGroup:
                SortAxisTracks(rotationGroup.Boxes, static box => box.Axis);
                break;
            case BaseLightTranslationEventBoxGroup translationGroup:
                SortAxisTracks(translationGroup.Boxes, static box => box.Axis);
                break;
        }
    }

    // Resolve the cloned child by stable box and event indexes without scanning the beatmap.
    private static bool TryFindEventIndex(BaseGLSEvent evt, out int eventIndex)
    {
        eventIndex = -1;
        if (evt.BoxIndex < 0
            || evt.EventBoxGroupData == null
            || evt.BoxIndex >= evt.EventBoxGroupData.ReadOnlyBoxes.Count
            || !ReferenceEquals(evt.EventBoxGroupData.ReadOnlyBoxes[evt.BoxIndex], evt.EventBoxData))
        {
            return false;
        }

        var events = evt.EventBoxData.ReadOnlyEvents;
        for (var i = 0; i < events.Count; i++)
        {
            if (ReferenceEquals(events[i], evt))
            {
                eventIndex = i;
                return true;
            }
        }

        return false;
    }

    // Rebind every edited child after a box split so inner lanes and outer previews share valid ownership.
    internal static void RebindGroup<TBox>(BaseEventBoxGroup<TBox> group)
        where TBox : BaseEventBox
    {
        for (var boxIndex = 0; boxIndex < group.Boxes.Count; boxIndex++)
        {
            var box = group.Boxes[boxIndex];
            foreach (var evt in box.ReadOnlyEvents)
            {
                evt.EventBoxData = box;
                evt.EventBoxGroupData = group;
                evt.BoxIndex = boxIndex;
                evt.JsonTime = group.JsonTime + evt.RelativeJsonTime;
            }
        }

        group.ResortOrderedEvents();
        group.SaveCustom();
    }

    // Ctrl+Arrow and other base-typed group edits share one ownership-finalization dispatch across every GLS node type.
    internal static void RebindGroup(BaseEventBoxGroup group)
    {
        switch (group)
        {
            case BaseLightColorEventBoxGroup colorGroup:
                RebindGroup(colorGroup);
                break;
            case BaseLightRotationEventBoxGroup rotationGroup:
                RebindGroup(rotationGroup);
                break;
            case BaseLightTranslationEventBoxGroup translationGroup:
                RebindGroup(translationGroup);
                break;
            case BaseVfxEventEventBoxGroup floatFxGroup:
                RebindGroup(floatFxGroup);
                break;
        }
    }

    internal static bool TryMaterializeAutomaticAxisLane(
        BaseEventBoxGroup group,
        BaseEventBox displayBox,
        out BaseEventBox materializedBox,
        out int materializedBoxIndex)
    {
        switch (group)
        {
            case BaseLightRotationEventBoxGroup rotationGroup when displayBox is BaseLightRotationEventBox rotationBox:
                materializedBox = (BaseEventBox)rotationBox.Clone();
                rotationGroup.Boxes.Add((BaseLightRotationEventBox)materializedBox);
                SortAxisTracks(rotationGroup.Boxes, static box => box.Axis);
                break;
            case BaseLightTranslationEventBoxGroup translationGroup when displayBox is BaseLightTranslationEventBox translationBox:
                materializedBox = (BaseEventBox)translationBox.Clone();
                translationGroup.Boxes.Add((BaseLightTranslationEventBox)materializedBox);
                SortAxisTracks(translationGroup.Boxes, static box => box.Axis);
                break;
            default:
                materializedBox = null;
                materializedBoxIndex = -1;
                return false;
        }

        // Resolve the stable index once after sorting so callers never assume a materialized lane was appended.
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            if (ReferenceEquals(group.ReadOnlyBoxes[boxIndex], materializedBox))
            {
                materializedBoxIndex = boxIndex;
                return true;
            }
        }

        materializedBox = null;
        materializedBoxIndex = -1;
        return false;
    }

    public static (BaseEventBoxGroup group, TEvent evt) CopyGroupFrom<TEvent>(TEvent evt)
        where TEvent : BaseGLSEvent
    {
        var newEvtIdx = Array.IndexOf((TEvent[])evt.EventBoxData.ReadOnlyEvents, evt);
        var newGroup = BeatmapFactory.Clone(evt.EventBoxGroupData);
        var newEvt = newGroup.ReadOnlyBoxes[evt.BoxIndex].ReadOnlyEvents[newEvtIdx] as TEvent;
        return (newGroup, newEvt);
    }

    public static BaseEventBoxGroup TriggerPlaceAction(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, oldGroup, "Modified event box group.");
        BeatmapActionContainer.AddAction(action, true);
        return newGroup;
    }

    public static BaseEventBoxGroup TriggerModifyEventBoxAction(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup,
        ActionMergeType actionMergeType)
    {
        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            oldGroup,
            "Modified event box group.",
            actionMergeType);
        BeatmapActionContainer.AddAction(action, true);
        return newGroup;
    }

    public static TEvent TriggerModifyEventAction<TEvent>(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup,
        TEvent newEvt,
        ActionMergeType actionMergeType)
        where TEvent : BaseGLSEvent
    {
        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            oldGroup,
            "Modified event box.",
            actionMergeType);
        BeatmapActionContainer.AddAction(action, true);
        return newEvt;
    }
}
