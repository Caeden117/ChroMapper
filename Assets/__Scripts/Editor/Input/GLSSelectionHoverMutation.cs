using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Helper;

public static class GLSSelectionHoverMutation
{
    public static bool TryApply<TEvent>(
        TEvent hoveredEvent,
        Func<TEvent, bool> mutation,
        out bool changed)
        where TEvent : BaseGLSEvent =>
        TryApply(
            hoveredEvent,
            (_, events) =>
            {
                var modified = false;
                foreach (var evt in events)
                    modified |= mutation(evt);

                return modified;
            },
            out changed);

    public static bool TryApply<TEvent>(
        TEvent hoveredEvent,
        Func<BaseEventBoxGroup, List<TEvent>, bool> mutation,
        out bool changed)
        where TEvent : BaseGLSEvent
    {
        changed = false;
        // An outer preview represents one child. Only explicit parent selection authorizes editing every child in that group.
        if (!SelectionController.IsObjectSelected(hoveredEvent)
            && !SelectionController.IsObjectSelected(hoveredEvent.EventBoxGroupData))
        {
            return false;
        }

        var groups = new HashSet<BaseEventBoxGroup>();
        var objectType = hoveredEvent.EventBoxGroupData.ObjectType;
        foreach (var selected in SelectionController.SelectedObjects)
        {
            var group = selected is BaseGLSEvent evt
                ? evt.EventBoxGroupData
                : selected as BaseEventBoxGroup;
            if (group != null && group.ObjectType == objectType)
                groups.Add(group);
        }

        var actions = new List<BeatmapAction>(groups.Count);
        foreach (var group in groups)
        {
            var editedGroup = BeatmapFactory.Clone(group);
            var selectedGroup = SelectionController.IsObjectSelected(group);
            var eventsToModify = new List<TEvent>();
            var originalSelection = new List<BaseGLSEvent>();
            var editedSelection = new List<BaseGLSEvent>();
            // A group clone already visits every child, so collect its selected counterparts in one pass rather than searching per node.
            for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
            {
                var originalEvents = group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents;
                var editedEvents = editedGroup.ReadOnlyBoxes[boxIndex].ReadOnlyEvents;
                for (var eventIndex = 0; eventIndex < originalEvents.Count; eventIndex++)
                {
                    var selectedEvent = SelectionController.IsObjectSelected(originalEvents[eventIndex]);
                    if (selectedEvent)
                    {
                        originalSelection.Add(originalEvents[eventIndex]);
                        editedSelection.Add(editedEvents[eventIndex]);
                    }

                    if ((selectedGroup || selectedEvent) && editedEvents[eventIndex] is TEvent target)
                        eventsToModify.Add(target);
                }
            }

            if (!mutation(editedGroup, eventsToModify))
                continue;

            actions.Add(new BeatmapGLSEventBoxModifiedAction(
                editedGroup,
                group,
                "Modified selected GLS events.",
                originalSelectedEvents: originalSelection,
                editedSelectedEvents: editedSelection));
        }

        changed = actions.Count > 0;
        if (actions.Count == 1)
            BeatmapActionContainer.AddAction(actions[0], true);
        else if (actions.Count > 1)
        {
            BeatmapActionContainer.AddAction(
                new ActionCollectionAction(actions, clearsSelection: false, comment: "Modified selected GLS events."),
                true);
        }

        return true;
    }
}
