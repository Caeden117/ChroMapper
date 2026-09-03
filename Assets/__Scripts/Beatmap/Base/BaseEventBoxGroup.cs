using System;
using System.Collections.Generic;
using SimpleJSON;
using ZLinq;

namespace Beatmap.Base
{
    public abstract class BaseEventBoxGroup : BaseObject
    {
        // Notify data-only preview indexes when this logical group's event ordering changes.
        public event Action<BaseEventBoxGroup> OnOrderedEventsResorted;

        protected BaseEventBoxGroup()
        {
        }

        protected BaseEventBoxGroup(float time, int id, JSONNode customData = null) : base(
            time,
            customData) =>
            ID = id;

        public int ID;

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseEventBoxGroup eventBoxGroup && other.GetType() == GetType()) return ID == eventBoxGroup.ID;
            return false;
        }

        // Distinguish an initialized empty authored group from a cache that has not been built yet.
        public bool OrderedEventsInitialized { get; set; }

        public abstract IReadOnlyList<BaseEventBox> ReadOnlyBoxes { get; }

        // Base-type viewport code needs the cached List so it can use the shared allocation-free binary-search helper.
        public abstract List<BaseGLSEvent> OrderedEvents { get; protected set; }

        // Shared GLS mutation code receives the non-generic group base, so expose its required ordering refresh polymorphically.
        public abstract void ResortOrderedEvents();

        public abstract void PruneEmptyAutomaticAxisLanes();

        // Keep event invocation in the declaring base type so generic groups can invalidate their data-only indexes.
        protected void NotifyOrderedEventsResorted() => OnOrderedEventsResorted?.Invoke(this);
    }

    public abstract class BaseEventBoxGroup<TBox> : BaseEventBoxGroup where TBox : BaseEventBox
    {
        protected BaseEventBoxGroup()
        {
        }

        protected BaseEventBoxGroup(float time, int id, JSONNode customData = null) : base(
            time,
            id,
            customData)
        {
        }

        public List<TBox> Boxes = new();

        // Cached node ordering supports deterministic outer previews and future ghost-node rendering.
        public override List<BaseGLSEvent> OrderedEvents { get; protected set; } = new();

        public override void ResortOrderedEvents()
        {
            // Preserve each event's array/JSON index as the final tie-breaker because sort stability is not guaranteed.
            // Without it, stacked events with identical time and BoxIndex can randomly alternate as the outer preview.
            var indexedEvents = new List<(BaseGLSEvent Event, int EventIndex)>();
            foreach (var box in Boxes)
            {
                for (var eventIndex = 0; eventIndex < box.ReadOnlyEvents.Count; eventIndex++)
                    indexedEvents.Add((box.ReadOnlyEvents[eventIndex], eventIndex));
            }

            indexedEvents.Sort(static (left, right) =>
            {
                var comparison = left.Event.RelativeJsonTime.CompareTo(right.Event.RelativeJsonTime);
                if (comparison == 0)
                    comparison = left.Event.BoxIndex.CompareTo(right.Event.BoxIndex);
                if (comparison == 0)
                    comparison = left.EventIndex.CompareTo(right.EventIndex);
                return comparison;
            });

            OrderedEvents = new List<BaseGLSEvent>(indexedEvents.Count);
            foreach (var indexedEvent in indexedEvents)
                OrderedEvents.Add(indexedEvent.Event);

            // Record initialization separately so empty groups do not sort again for every preview query.
            OrderedEventsInitialized = true;
            // Refresh only indexes that own this group instead of coupling selection to rendered containers.
            NotifyOrderedEventsResorted();
        }

        public override void PruneEmptyAutomaticAxisLanes()
        {
            var hasPermanentOrPopulatedBox = false;
            for (var boxIndex = 0; boxIndex < Boxes.Count; boxIndex++)
            {
                var box = Boxes[boxIndex];
                if (!box.IsAutomaticAxisLane || box.ReadOnlyEvents.Count > 0)
                {
                    hasPermanentOrPopulatedBox = true;
                    break;
                }
            }

            var retainedAutomaticIndex = hasPermanentOrPopulatedBox
                ? -1
                : Boxes.Count - 1;
            // Most GLS mutations do not remove a lane, so remember whether indexes actually shifted before touching every child.
            var removedBox = false;
            for (var boxIndex = Boxes.Count - 1; boxIndex >= 0; boxIndex--)
            {
                var box = Boxes[boxIndex];
                if (box.IsAutomaticAxisLane
                    && box.ReadOnlyEvents.Count == 0
                    && boxIndex != retainedAutomaticIndex)
                {
                    Boxes.RemoveAt(boxIndex);
                    removedBox = true;
                }
            }

            if (removedBox)
            {
                for (var boxIndex = 0; boxIndex < Boxes.Count; boxIndex++)
                {
                    var box = Boxes[boxIndex];
                    foreach (var evt in box.ReadOnlyEvents)
                    {
                        evt.EventBoxData = box;
                        evt.EventBoxGroupData = this;
                        evt.BoxIndex = boxIndex;
                        evt.JsonTime = JsonTime + evt.RelativeJsonTime;
                    }
                }
            }

            ResortOrderedEvents();
        }

        // Deserialization has complete group ownership here, so normalize each filter lane once and report accurate outer/inner beats.
        public void NormalizeLoadedEventConflicts()
        {
            for (var boxIndex = 0; boxIndex < Boxes.Count; boxIndex++)
            {
                var box = Boxes[boxIndex];
                box.SetEvents(box.ReadOnlyEvents.AsValueEnumerable().ToArray());
                foreach (var evt in box.ReadOnlyEvents)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = this;
                    evt.BoxIndex = boxIndex;
                    evt.JsonTime = JsonTime + evt.RelativeJsonTime;
                }
            }

            ResortOrderedEvents();
        }

        public override int CompareTo(BaseObject other)
        {
            var comparison = base.CompareTo(other);

            // Early return if we're comparing against a different object type
            if (other is not BaseEventBoxGroup<TBox> group) return comparison;

            // Is not the same group type
            if (other.GetType() != GetType()) return comparison;

            // Compare by type if ID match
            if (comparison == 0) comparison = ID.CompareTo(group.ID);

            // TODO: I realise it is not possible and is unadvisable to sort based on event boxes,
            //  first in last out type of deal, we might have to prevent 2 GLS group in same time

            // All matching vanilla properties so compare custom data as a final check
            if (comparison == 0)
                comparison = string.Compare(
                    CustomData?.ToString(),
                    group.CustomData?.ToString(),
                    StringComparison.Ordinal);

            return comparison;
        }

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseEventBoxGroup<TBox> group)
                return;

            ID = group.ID;
            Boxes = group.Boxes
                .AsValueEnumerable()
                .Select(x => (TBox)x.Clone())
                .ToList();

            for (var i = 0; i < Boxes.Count; i++)
            {
                var box = Boxes[i];
                foreach (var evt in box.ReadOnlyEvents)
                {
                    evt.EventBoxData = box;
                    evt.EventBoxGroupData = this;
                    evt.BoxIndex = i;
                    evt.JsonTime = evt.RelativeJsonTime + JsonTime;
                }
            }

            ResortOrderedEvents();
        }

        public override IReadOnlyList<BaseEventBox> ReadOnlyBoxes => Boxes;
    }
}
