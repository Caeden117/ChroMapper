using System;
using System.Collections.Generic;
using Beatmap.Enums;
using Beatmap.Shared;
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

        public void ApplyDefaultAffectFirst()
        {
            var value = Settings.Instance.DefaultGLSAffectFirst ? 1 : 0;
            switch (this)
            {
                case BaseLightColorEventBox color:
                    color.BrightnessAffectFirst = value;
                    break;
                case BaseLightTransformEventBox transform:
                    transform.AffectFirst = value;
                    break;
                case BaseVfxEventEventBox fx:
                    fx.VfxAffectFirst = value;
                    break;
            }
        }

        public bool ShouldSerializeAffectFirst(int value, float distribution, int distributionType)
        {
            // Exact zero checks retain even tiny offsets. Unknown custom data may also change distribution behavior.
            if (value != 1
                || BeatDistribution != 0f
                || BeatDistributionType != (int)DistributionType.Wave
                || distribution != 0f
                || distributionType != (int)DistributionType.Wave
                || Easing != 0
                || IndexFilter.LimitAffectsType != 0)
            {
                return true;
            }

            if (this is BaseLightColorEventBox color
                && (color.ColorDistributions.Length > 0
                    || color.StrobeColorDistributions.Length > 0
                    || color.CustomData.Count > 0))
            {
                return true;
            }

            var events = ReadOnlyEvents;
            for (var index = 0; index < events.Count; index++)
            {
                var evt = events[index];
                if ((evt.CustomData != null && evt.CustomData.Count > 0)
                    || evt.CustomColor.HasValue
                    || evt.CustomTrack != null)
                {
                    return true;
                }
                // Color controls can hold changes that SaveCustom has not written into the JSON payload yet.
                if (evt is BaseLightColorBase colorEvent
                    && (colorEvent.ColorDistributions.Length > 0
                        || colorEvent.StrobeColorDistributions.Length > 0
                        || colorEvent.StrobeColor.HasValue
                        || colorEvent.ChromaColorEasing.HasValue
                        || colorEvent.ChromaStrobeColorEasing.HasValue
                        || colorEvent.ChromaStrobeEasing.HasValue
                        || colorEvent.ChromaStrobeInterval.HasValue
                        || colorEvent.ChromaStrobePulseWidth != 0.5f
                        || colorEvent.CustomLerpType != BasicEventColorLerpType.RGB))
                {
                    return true;
                }
            }
            return false;
        }

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
                    var deletedEvent = resolved[resolved.Count - 1];
                    var outerBeat = evt.EventBoxGroupData != null
                        ? evt.EventBoxGroupData.JsonTime
                        : evt.JsonTime - evt.RelativeJsonTime;
                    Debug.LogWarning(
                        $"[GLSEventConflict] Deleted duplicate GLS node outerGroupBeat={outerBeat:R} " +
                        $"innerBeatOffset={deletedEvent.RelativeJsonTime:R} filterLane={deletedEvent.BoxIndex}.");
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

    // Rotation and translation boxes share XYZ lane behavior while retaining their typed event arrays and JSON properties.
    public abstract class BaseLightTransformEventBox : BaseEventBox
    {
        protected BaseLightTransformEventBox()
        {
        }

        protected BaseLightTransformEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            int axis,
            int flip) : base(indexFilter, beatDistribution, beatDistributionType)
        {
            Axis = axis;
            Flip = flip;
        }

        protected BaseLightTransformEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            int easing,
            int axis,
            int flip) : base(indexFilter, beatDistribution, beatDistributionType, easing)
        {
            Axis = axis;
            Flip = flip;
        }

        protected BaseLightTransformEventBox(BaseLightTransformEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            Axis = other.Axis;
            Flip = other.Flip;
            IsAutomaticAxisLane = other.IsAutomaticAxisLane;
        }

        public int Axis { get; set; }
        public int Flip { get; set; }
        public abstract float ValueDistribution { get; set; }
        public abstract int ValueDistributionType { get; set; }
        public abstract int AffectFirst { get; set; }
        public abstract float ValueDistributionDisplayScale { get; }
        public abstract bool AcceptsEvent(BaseGLSEvent evt);

        public override Axis GetAxis() => (Axis)Axis;
    }
}
