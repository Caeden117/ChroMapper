using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSCommonCommand
{
    public static void CycleEventAxis(InputAction.CallbackContext context, BaseGLSEvent evt)
    {
        if (!context.performed
            || evt is not (BaseLightRotationBase or BaseLightTranslationBase))
        {
            return;
        }

        var direction = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (direction == 0)
        {
            return;
        }

        CycleTransformEventAxis(evt, direction);
    }

    private static void CycleTransformEventAxis(BaseGLSEvent evt, int direction)
    {
        if (evt.EventBoxGroupData is not ILightTransformEventBoxGroup originalGroup
            || evt.EventBoxData is not BaseLightTransformEventBox sourceBox
            || !sourceBox.AcceptsEvent(evt)
            || !TryFindEventIndex(evt, out var eventIndex))
        {
            var eventType = evt is BaseLightRotationBase ? "Rotation" : "Translation";
            Debug.LogError($"[GLSAxisScroll] {eventType} event has invalid group or box ownership.");
            return;
        }

        if (!TryFindOpenAxis(
                originalGroup.TransformBoxes,
                evt.BoxIndex,
                evt.RelativeJsonTime,
                direction,
                out var targetAxis))
        {
            WarnNoOpenAxis();
            return;
        }

        var originalGroupData = evt.EventBoxGroupData;
        var editedGroupData = BeatmapFactory.Clone(originalGroupData);
        var editedGroup = (ILightTransformEventBoxGroup)editedGroupData;
        MoveEventToAxisTrack(editedGroup, evt.BoxIndex, eventIndex, targetAxis);

        RebindGroup(editedGroupData);
        TriggerModifyEventBoxAction(originalGroupData, editedGroupData, ActionMergeType.ModifyGLSEventAxis);
    }

    // Treat an axis as occupied when any of its filter lanes already owns a node at the moving node's relative beat.
    private static bool TryFindOpenAxis(
        IReadOnlyList<BaseLightTransformEventBox> boxes,
        int sourceBoxIndex,
        float relativeJsonTime,
        int direction,
        out int targetAxis)
    {
        var sourceAxis = boxes[sourceBoxIndex].Axis;
        var axisDirection = Math.Sign(direction);
        for (var axisOffset = 1; axisOffset < 3; axisOffset++)
        {
            var candidateAxis = (sourceAxis + (axisDirection * axisOffset) + 6) % 3;
            var occupied = false;
            for (var boxIndex = 0; boxIndex < boxes.Count && !occupied; boxIndex++)
            {
                if (boxes[boxIndex].Axis != candidateAxis)
                {
                    continue;
                }

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

    private static bool ContainsEventAtRelativeTime(IReadOnlyList<BaseGLSEvent> events, float relativeJsonTime)
    {
        var eventIndex = events.BinarySearchBy(relativeJsonTime, static evt => evt.RelativeJsonTime);
        if (eventIndex >= 0)
        {
            return true;
        }

        var insertionIndex = ~eventIndex;
        return (insertionIndex < events.Count
                && Math.Abs(events[insertionIndex].RelativeJsonTime - relativeJsonTime)
                < BeatmapObjectContainerCollection.Epsilon)
            || (insertionIndex > 0
                && Math.Abs(events[insertionIndex - 1].RelativeJsonTime - relativeJsonTime)
                < BeatmapObjectContainerCollection.Epsilon);
    }

    private static void WarnNoOpenAxis()
    {
        const string message = "No open axis to shift to on this beat.";
        PersistentUI.Instance.DisplayMessage(message, PersistentUI.DisplayMessageType.Bottom);
    }

    // Reuse an existing destination-axis track through the common transform-group contract without changing concrete box types.
    private static (bool createdDestination, bool removedSource) MoveEventToAxisTrack(
        ILightTransformEventBoxGroup group,
        int sourceBoxIndex,
        int eventIndex,
        int targetAxis)
    {
        var boxes = group.TransformBoxes;
        var sourceBox = boxes[sourceBoxIndex];
        var movedEvent = sourceBox.ReadOnlyEvents[eventIndex];
        BaseLightTransformEventBox targetBox = null;
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            if (boxIndex != sourceBoxIndex && boxes[boxIndex].Axis == targetAxis)
            {
                targetBox = boxes[boxIndex];
                break;
            }
        }

        var createdDestination = targetBox == null;
        if (createdDestination)
        {
            targetBox = (BaseLightTransformEventBox)sourceBox.Clone();
            targetBox.Axis = targetAxis;
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
        if (removedSource)
            group.RemoveTransformBoxAt(sourceBoxIndex);
        if (createdDestination)
            group.TryAddTransformBox(targetBox);
        group.SortAxisTracks();
        return (createdDestination, removedSource);
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

    // Every GLS group already exposes base-typed boxes, so ownership finalization needs no concrete group dispatch.
    internal static void RebindGroup(BaseEventBoxGroup group)
    {
        // Preserve the previous supported-group boundary while collapsing rotation and translation through their common contract.
        if (group is not (BaseLightColorEventBoxGroup or ILightTransformEventBoxGroup or BaseVfxEventEventBoxGroup))
        {
            return;
        }

        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            var box = group.ReadOnlyBoxes[boxIndex];
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

    internal static bool TryMaterializeAutomaticAxisLane(
        BaseEventBoxGroup group,
        BaseEventBox displayBox,
        out BaseEventBox materializedBox,
        out int materializedBoxIndex)
    {
        // The common transform contract validates concrete compatibility before adding and sorting a cloned display lane.
        if (group is not ILightTransformEventBoxGroup transformGroup
            || displayBox is not BaseLightTransformEventBox transformBox)
        {
            materializedBox = null;
            materializedBoxIndex = -1;
            return false;
        }

        materializedBox = (BaseEventBox)transformBox.Clone();
        if (!transformGroup.TryAddTransformBox((BaseLightTransformEventBox)materializedBox))
        {
            materializedBox = null;
            materializedBoxIndex = -1;
            return false;
        }
        transformGroup.SortAxisTracks();

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
