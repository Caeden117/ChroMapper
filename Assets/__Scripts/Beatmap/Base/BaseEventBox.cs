using System;
using System.Collections.Generic;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseEventBox : BaseItem
    {
        protected BaseEventBox()
        {
            IndexFilter = new BaseIndexFilter();
            BeatDistributionType = (int)DistributionType.Wave;
        }

        protected BaseEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
            Easing = 0;
        }

        protected BaseEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            int easing)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
            Easing = easing;
        }

        public BaseIndexFilter IndexFilter { get; set; }
        public float BeatDistribution { get; set; }
        public int BeatDistributionType { get; set; }

        public int Easing { get; set; }

        /// <summary>
        /// Is it a temp ghost lane for ease of placement for rotation / translation XYZ? 
        /// We don't serialize these, they only exist once used.
        /// </summary>
        public bool IsAutomaticAxisLane { get; set; }
        
        public abstract IReadOnlyList<BaseGLSEvent> ReadOnlyEvents { get; }
        public abstract void ClearEvents();
        public abstract void SetEvents(BaseGLSEvent[] data);

        // Editor mutations must replace an existing node at the same relative beat instead of creating overlapping, ambiguous GLS nodes.
        protected static BaseGLSEvent[] ResolveSameBeatConflicts(BaseGLSEvent[] data)
        {
            if (data == null || data.Length < 2)
            {
                return data ?? Array.Empty<BaseGLSEvent>();
            }

            // SetEvents callers normally supply chronological unique nodes, so keep that hot mutation path allocation-free here.
            var requiresResolution = false;
            for (var index = 1; index < data.Length; index++)
            {
                var previousTime = data[index - 1].RelativeJsonTime;
                var currentTime = data[index].RelativeJsonTime;
                if (currentTime < previousTime
                    || Math.Abs(currentTime - previousTime) < BeatmapObjectContainerCollection.Epsilon)
                {
                    requiresResolution = true;
                    break;
                }
            }

            if (!requiresResolution)
            {
                return data;
            }

            var indexedEvents = new IndexedEvent[data.Length];
            for (var index = 0; index < data.Length; index++)
            {
                indexedEvents[index] = new IndexedEvent(data[index], index);
            }

            Array.Sort(indexedEvents, static (left, right) =>
            {
                var comparison = left.Event.RelativeJsonTime.CompareTo(right.Event.RelativeJsonTime);
                return comparison != 0
                    ? comparison
                    : left.OriginalIndex.CompareTo(right.OriginalIndex);
            });

            var resolved = new List<BaseGLSEvent>(indexedEvents.Length);
            for (var index = 0; index < indexedEvents.Length; index++)
            {
                var evt = indexedEvents[index].Event;
                if (resolved.Count > 0
                    && Math.Abs(resolved[resolved.Count - 1].RelativeJsonTime - evt.RelativeJsonTime)
                    < BeatmapObjectContainerCollection.Epsilon)
                {
                    // Preserve normal replacement semantics and identify every discarded node precisely enough to repair the source JSON.
                    var deletedEvent = resolved[resolved.Count - 1];
                    var outerBeat = evt.EventBoxGroupData != null
                        ? evt.EventBoxGroupData.JsonTime
                        : evt.JsonTime - evt.RelativeJsonTime;
                    Debug.LogWarning(
                        $"[GLSEventConflict] Deleted duplicate GLS node outerGroupBeat={outerBeat:R} " +
                        $"innerBeatOffset={deletedEvent.RelativeJsonTime:R} filterLane={deletedEvent.BoxIndex} " +
                        $"deleted={GLSEventCommon.DescribeEvent(deletedEvent)} kept={GLSEventCommon.DescribeEvent(evt)}.");
                    resolved[resolved.Count - 1] = evt;
                }
                else
                {
                    resolved.Add(evt);
                }
            }

            return resolved.ToArray();
        }

        // Stable source indexes make the last supplied node win even though Array.Sort itself is unstable on equal keys.
        private readonly struct IndexedEvent
        {
            public IndexedEvent(BaseGLSEvent evt, int originalIndex)
            {
                Event = evt;
                OriginalIndex = originalIndex;
            }

            public BaseGLSEvent Event { get; }
            public int OriginalIndex { get; }
        }

        public virtual Axis GetAxis() => Axis.X;

    }
}
