using System;
using System.Collections.Generic;
using Beatmap.Base;

internal static class GLSSelectionAxisMutation
{
    public static bool Apply(BaseEventBoxGroup groupData, List<BaseGLSEvent> events, int direction)
    {
        var group = (ILightTransformEventBoxGroup)groupData;
        var occupiedTimes = new AxisTimeIndex(group.TransformBoxes);
        var boxesByAxis = new BaseLightTransformEventBox[3];
        foreach (var box in group.TransformBoxes)
        {
            if (boxesByAxis[box.Axis] == null)
                boxesByAxis[box.Axis] = box;
        }

        var destinations = new Dictionary<BaseGLSEvent, BaseLightTransformEventBox>(events.Count);
        var sourceBoxes = new HashSet<BaseLightTransformEventBox>();
        var affectedBoxes = new HashSet<BaseLightTransformEventBox>();
        var rejected = false;
        foreach (var evt in events)
        {
            var sourceBox = (BaseLightTransformEventBox)evt.EventBoxData;
            occupiedTimes.Remove(sourceBox.Axis, evt.RelativeJsonTime);
            var targetAxis = sourceBox.Axis;
            for (var offset = 1; offset < 3; offset++)
            {
                var candidate = (sourceBox.Axis + (Math.Sign(direction) * offset) + 6) % 3;
                if (!occupiedTimes.Contains(candidate, evt.RelativeJsonTime))
                {
                    targetAxis = candidate;
                    break;
                }
            }

            occupiedTimes.Add(targetAxis, evt.RelativeJsonTime);
            if (targetAxis == sourceBox.Axis)
            {
                rejected = true;
                continue;
            }

            var targetBox = boxesByAxis[targetAxis];
            if (targetBox == null)
            {
                targetBox = (BaseLightTransformEventBox)sourceBox.Clone();
                targetBox.Axis = targetAxis;
                targetBox.SetEvents(Array.Empty<BaseGLSEvent>());
                group.TryAddTransformBox(targetBox);
                boxesByAxis[targetAxis] = targetBox;
            }

            destinations.Add(evt, targetBox);
            sourceBoxes.Add(sourceBox);
            affectedBoxes.Add(sourceBox);
            affectedBoxes.Add(targetBox);
        }

        if (rejected)
            GLSCommonCommand.WarnNoOpenAxis();

        if (destinations.Count == 0)
            return false;

        // Keep lane arrays unchanged while planning moves, then write each affected lane once.
        var eventsByBox = new Dictionary<BaseLightTransformEventBox, List<BaseGLSEvent>>(affectedBoxes.Count);
        foreach (var box in affectedBoxes)
            eventsByBox.Add(box, new List<BaseGLSEvent>(box.ReadOnlyEvents.Count));

        foreach (var box in affectedBoxes)
        {
            foreach (var evt in box.ReadOnlyEvents)
            {
                var destination = destinations.TryGetValue(evt, out var targetBox)
                    ? targetBox
                    : box;
                eventsByBox[destination].Add(evt);
            }
        }

        foreach (var (box, boxEvents) in eventsByBox)
        {
            boxEvents.Sort(static (left, right) => left.RelativeJsonTime.CompareTo(right.RelativeJsonTime));
            box.SetEvents(boxEvents.ToArray());
        }

        for (var index = group.TransformBoxes.Count - 1; index >= 0; index--)
        {
            var box = group.TransformBoxes[index];
            if (sourceBoxes.Contains(box) && box.ReadOnlyEvents.Count == 0)
                group.RemoveTransformBoxAt(index);
        }

        group.SortAxisTracks();
        GLSCommonCommand.RebindGroup(groupData);
        return true;
    }

    private sealed class AxisTimeIndex
    {
        private readonly SortedSet<float>[] times = { new(), new(), new() };
        // Filter lanes can share an axis and beat, so removing one node must keep that beat occupied for the others.
        private readonly Dictionary<(int Axis, float Time), int> counts = new();

        public AxisTimeIndex(IReadOnlyList<BaseLightTransformEventBox> boxes)
        {
            foreach (var box in boxes)
            {
                foreach (var evt in box.ReadOnlyEvents)
                    Add(box.Axis, evt.RelativeJsonTime);
            }
        }

        public void Add(int axis, float time)
        {
            counts.TryGetValue((axis, time), out var count);
            counts[(axis, time)] = count + 1;
            times[axis].Add(time);
        }

        public void Remove(int axis, float time)
        {
            var count = counts[(axis, time)] - 1;
            if (count == 0)
            {
                counts.Remove((axis, time));
                times[axis].Remove(time);
            }
            else
            {
                counts[(axis, time)] = count;
            }
        }

        public bool Contains(int axis, float time)
        {
            var epsilon = BeatmapObjectContainerCollection.Epsilon;
            foreach (var candidate in times[axis].GetViewBetween(time - epsilon, time + epsilon))
            {
                if (Math.Abs(candidate - time) < epsilon)
                    return true;
            }

            return false;
        }
    }
}
