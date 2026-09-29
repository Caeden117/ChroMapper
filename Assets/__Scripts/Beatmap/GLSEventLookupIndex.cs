using System.Collections.Generic;
using Beatmap.Base;

/// <summary>
///     Maps authoritative GLS event references to their box and event indexes.
/// </summary>
/// <remarks>
///     Used by paint-properties, mirror, time-shift, and lane-shift operations after they clone an owning GLS group.
///     It is most valuable when a large group has many nodes in a box and the mapper edits many selected nodes,
///     including stacked duplicates. Previously each selected node copied and searched its source box:
///     <c>O(N + S * E)</c>, where <c>N</c> is group clone/setup work, <c>S</c> selected nodes, and <c>E</c> nodes per
///     box. Building this data-only reference index once makes source resolution <c>O(N + S)</c>.
/// </remarks>
internal sealed class GLSEventLookupIndex
{
    private readonly Dictionary<BaseGLSEvent, EventLocation> eventLocations;

    // Build one reference index per affected source group so selected nodes retain their exact authored array position.
    public GLSEventLookupIndex(BaseEventBoxGroup group)
    {
        var capacity = 0;
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            capacity += group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents.Count;
        }

        eventLocations = new Dictionary<BaseGLSEvent, EventLocation>(capacity, ReferenceComparer.Instance);
        for (var boxIndex = 0; boxIndex < group.ReadOnlyBoxes.Count; boxIndex++)
        {
            var events = group.ReadOnlyBoxes[boxIndex].ReadOnlyEvents;
            for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                var evt = events[eventIndex];
                if (!eventLocations.ContainsKey(evt))
                {
                    eventLocations.Add(evt, new EventLocation(boxIndex, eventIndex));
                }
            }
        }
    }

    // Resolve the counterpart in a cloned group by its authoritative source-array position.
    public bool TryGetCloneEvent(
        BaseGLSEvent sourceEvent,
        BaseEventBoxGroup clonedGroup,
        out EventLocation location,
        out BaseGLSEvent clonedEvent)
    {
        if (!eventLocations.TryGetValue(sourceEvent, out location)
            || location.BoxIndex >= clonedGroup.ReadOnlyBoxes.Count)
        {
            clonedEvent = null;
            return false;
        }

        var clonedEvents = clonedGroup.ReadOnlyBoxes[location.BoxIndex].ReadOnlyEvents;
        if (location.EventIndex >= clonedEvents.Count)
        {
            clonedEvent = null;
            return false;
        }

        clonedEvent = clonedEvents[location.EventIndex];
        return true;
    }

    // Materialize selected GLS nodes by owning group once for callers that will clone each parent group.
    public static Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>> GroupSelectedEvents(
        IEnumerable<BaseObject> selectedObjects)
    {
        var groups = new Dictionary<BaseEventBoxGroup, List<BaseGLSEvent>>();
        foreach (var selectedObject in selectedObjects)
        {
            if (selectedObject is not BaseGLSEvent selectedEvent
                || selectedEvent.EventBoxGroupData == null)
            {
                continue;
            }

            var group = selectedEvent.EventBoxGroupData;
            if (!groups.TryGetValue(group, out var groupEvents))
            {
                groupEvents = new List<BaseGLSEvent>();
                groups.Add(group, groupEvents);
            }

            groupEvents.Add(selectedEvent);
        }

        return groups;
    }

    public readonly struct EventLocation
    {
        public EventLocation(int boxIndex, int eventIndex)
        {
            BoxIndex = boxIndex;
            EventIndex = eventIndex;
        }

        public int BoxIndex { get; }

        public int EventIndex { get; }
    }

    // Reference equality prevents equal-looking stacked nodes from overwriting each other's source-array location.
    private sealed class ReferenceComparer : IEqualityComparer<BaseGLSEvent>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(BaseGLSEvent left, BaseGLSEvent right) => ReferenceEquals(left, right);

        public int GetHashCode(BaseGLSEvent obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

/// <summary>
///     Resolves replacement GLS nodes by stable identity while a parent group is replaced.
/// </summary>
internal sealed class GLSEventReplacementLookup
{
    private readonly Dictionary<(int boxIndex, float time), BaseGLSEvent> replacements = new();
    private readonly Dictionary<float, (Queue<BaseGLSEvent> entries, int remainingCount)>
        replacementsIgnoringLane = new();

    public GLSEventReplacementLookup(IReadOnlyList<BaseGLSEvent> events)
    {
        for (var index = 0; index < events.Count; index++)
        {
            var replacement = events[index];
            // SetEvents resolves same-lane/same-beat conflicts, so an exact identity has one replacement.
            replacements.Add(ExactIdentity(replacement), replacement);

            var time = replacement.RelativeJsonTime;
            if (!replacementsIgnoringLane.TryGetValue(time, out var bucket))
                bucket = (new Queue<BaseGLSEvent>(), 0);

            bucket.entries.Enqueue(replacement);
            bucket.remainingCount++;
            replacementsIgnoringLane[time] = bucket;
        }
    }

    public bool TryTake(BaseGLSEvent selectedEvent, out BaseGLSEvent replacement)
    {
        if (replacements.TryGetValue(ExactIdentity(selectedEvent), out replacement)
            && TryConsume(replacement))
        {
            return true;
        }

        replacement = null;
        return false;
    }

    public bool TryTakeUniqueIgnoringLane(BaseGLSEvent selectedEvent, out BaseGLSEvent replacement)
    {
        if (replacementsIgnoringLane.TryGetValue(selectedEvent.RelativeJsonTime, out var bucket)
            && bucket.remainingCount == 1)
        {
            while (bucket.entries.Count > 0)
            {
                replacement = bucket.entries.Dequeue();
                if (TryConsume(replacement)) return true;
            }
        }

        replacement = null;
        return false;
    }

    private bool TryConsume(BaseGLSEvent replacement)
    {
        if (!replacements.Remove(ExactIdentity(replacement)))
            return false;

        var time = replacement.RelativeJsonTime;
        var bucket = replacementsIgnoringLane[time];
        bucket.remainingCount--;
        replacementsIgnoringLane[time] = bucket;
        return true;
    }

    private static (int boxIndex, float time) ExactIdentity(BaseGLSEvent evt)
        => (evt.BoxIndex, evt.RelativeJsonTime);
}
