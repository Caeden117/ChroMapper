using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventCommon
{
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;
    // Partition color-transition timelines by GLS group ID so unrelated light groups never share cache work.
    private static readonly Dictionary<int, ColorTransitionGroupCache> colorTransitionCaches = new();
    // Index only sources with active transition intervals so viewport retention never scans all color sequences.
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorTransitionIntervals = new();
    // Reuse the cross-group query buffer because color-pool refresh runs every viewport update.
    private static readonly List<BaseLightColorBase> colorTransitionQueryResults = new();
    // Reuse the event ordering comparer while inserting edited nodes into their cached timelines.
    private static readonly Comparer<BaseLightColorBase> colorEventComparer =
        Comparer<BaseLightColorBase>.Create(CompareColorEvents);
    private static BaseDifficulty cachedColorTransitionMap;

    // GLS edits replace groups and child objects with clones, so diagnostics must expose reference identity as well as serialized values.
    public static string DescribeEvent(BaseGLSEvent evt)
    {
        if (evt == null)
        {
            return "null";
        }

        var color = evt is BaseLightColorBase colorEvent
            ? $", color={colorEvent.Color}, custom={FormatColor(colorEvent.CustomColor)}, usePrevious={colorEvent.UsePrevious}, easing={colorEvent.Easing}"
            : string.Empty;
        return $"{evt.GetType().Name}@{ReferenceId(evt)} abs={evt.JsonTime:R} rel={evt.RelativeJsonTime:R} " +
               $"box={evt.BoxIndex}/@{ReferenceId(evt.EventBoxData)} group={DescribeGroup(evt.EventBoxGroupData)}{color}";
    }

    // Group identity is logged separately because equal serialized GLS groups are routinely replaced by new instances.
    public static string DescribeGroup(BaseEventBoxGroup group) => group == null
        ? "null"
        : $"{group.GetType().Name}@{ReferenceId(group)} id={group.ID} beat={group.JsonTime:R} boxes={group.ReadOnlyBoxes.Count}";

    // Reference IDs remain null-safe while exposing identity independently of mutable beatmap equality.
    public static int ReferenceId(object value) => value == null
        ? 0
        : RuntimeHelpers.GetHashCode(value);

    // Nullable Chroma values need an invariant compact representation so cloned-event logs can be compared directly.
    public static string FormatColor(Color? color) => color.HasValue
        ? $"({color.Value.r:R},{color.Value.g:R},{color.Value.b:R},{color.Value.a:R})"
        : "null";

    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return ApplyBrightness(GetBaseColor(evt, boost, eventAppearance), evt.Brightness, eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        // Strobe corners use their own brightness, falling back to the main event color when no override exists.
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return ApplyBrightness(color, evt.StrobeBrightness, eventAppearance);
    }

    // Keep the strobe-band predicate tied to actual strobe timing, not its independently configurable dark brightness.
    public static bool IsStrobing(BaseLightColorBase evt)
        => evt.Frequency > 0 
            || (evt.ChromaStrobeInterval is { } interval && interval > 0f);

    // Keep the existing GLS dimness curve shared by main and strobe sections.
    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

    private static Color GetBaseColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        if (evt.CustomColor.HasValue) return evt.CustomColor.Value;

        return evt.Color == (int)LightColor.Red
            ? boost ? eventAppearance.RedBoostColor : eventAppearance.RedColor
            : evt.Color == (int)LightColor.Blue
                ? boost ? eventAppearance.BlueBoostColor : eventAppearance.BlueColor
                : boost
                    ? eventAppearance.WhiteBoostColor
                    : eventAppearance.WhiteColor;
    }

    public static string GetColorInfo(BaseLightColorBase evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Brightness * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));
        var hasStrobe = IsStrobing(evt);
        if (evt.ChromaStrobeInterval is { } strobeInterval && strobeInterval > 0f)
            sb.Append(FormatStrobeInterval(strobeInterval));
        else if (evt.Frequency > 0)
            sb.Append($"1/{evt.Frequency}");
        if (hasStrobe && evt.StrobeFade == 1)
            sb.Append(' ');
        if (evt.StrobeFade == 1)
            sb.Append('L');
        if ((hasStrobe || evt.StrobeFade == 1) && evt.StrobeBrightness > 0f)
            sb.Append(' ');
        if (evt.StrobeBrightness > 0f)
            sb.Append((evt.StrobeBrightness * 100f).ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    private static string FormatStrobeInterval(float strobeInterval)
    {
        // Force the requested number of decimal places so values like 2.0 render as 2.00 and .333 as .333.
        var format = strobeInterval >= 10f
            ? "0.0"
            : "0.00";
        return strobeInterval.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string GetRotationInfo(BaseLightRotationBase evt)
    {
        var sb = new StringBuilder();

        var direction = evt.Direction switch
        {
            (int)LightRotationDirection.Clockwise => "CW",
            (int)LightRotationDirection.CounterClockwise => "CCW",
            _ => "A"
        };

        sb.AppendLine(evt.Rotation.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));
        sb.AppendLine($"{direction} <{evt.Loop}>");

        return sb.ToString();
    }

    public static Color GetAxisColor(BaseGLSEvent evt, EventAppearanceSO eventAppearance)
    {
        // GLS X remains the neutral ring gray, while Y/Z reuse Basic Event CW/CCW's light and dark grays.
        return evt.EventBoxData?.GetAxis() switch
        {
            Axis.Y => eventAppearance.RingEventsClockwiseColor,
            Axis.Z => eventAppearance.RingEventsCounterClockwiseColor,
            _ => eventAppearance.RingEventsColor,
        };
    }

    public static string GetTranslationInfo(BaseLightTranslationBase evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine(GLSEventTranslationCommand.IsYeet(evt.Translation)
            ? "YEET"
            : (evt.Translation * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));

        return sb.ToString();
    }

    public static string GetFloatFXInfo(BaseFxEventFloat evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Value * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));

        return sb.ToString();
    }

    // Render each transition ribbon forward from its preceding matching-filter node to the transition target.
    public static void UpdateColorTransitionRibbon(
        LightGradientController controller,
        BaseLightColorBase source,
        EventAppearanceSO eventAppearance,
        Func<float, bool> isBoostAt)
    {
        if (!TryGetFollowingColorTransition(source, out var transition, out var followingEvent))
        {
            // LogRibbonDiagnostic(source, followingEvent, null, null);
            controller.SetVisible(false);
            return;
        }

        var startColor = GetColor(source, isBoostAt(source.JsonTime), eventAppearance);
        var endColor = GetColor(transition, isBoostAt(transition.JsonTime), eventAppearance);
        // LogRibbonDiagnostic(source, followingEvent, startColor, endColor);
        var gradient = new ChromaLightGradient(
            startColor,
            endColor,
            transition.SongBpmTime - source.SongBpmTime);
        controller.SetVisible(true);
        controller.UpdateGradientData(gradient);
        controller.UpdateDuration(gradient.Duration);
    }

    private static bool TryGetFollowingColorTransition(
        BaseLightColorBase source,
        out BaseLightColorBase transition,
        out BaseLightColorBase followingEvent)
    {
        transition = null;
        followingEvent = null;
        var filter = source.EventBoxData?.IndexFilter;
        var sourceGroup = source.EventBoxGroupData as BaseLightColorEventBoxGroup;
        // Unity singletons need explicit null checks before reaching map-owned GLS groups.
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (filter == null || sourceGroup == null || map == null)
        {
            return false;
        }

        EnsureColorTransitionCache(map);
        if (colorTransitionCaches.TryGetValue(sourceGroup.ID, out var groupCache))
        {
            groupCache.TryGetFollowingEvent(source, out followingEvent);
        }

        transition = followingEvent is { UsePrevious: 0 } && followingEvent.Easing != (int)EaseType.None
            ? followingEvent
            : null;
        return transition != null;
    }

    // Add only the inserted group's nodes and rewire the neighboring matching-filter events.
    public static void AddColorTransitionGroup(BaseLightColorEventBoxGroup group)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        if (!ReferenceEquals(cachedColorTransitionMap, map))
        {
            EnsureColorTransitionCache(map);
            return;
        }

        if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
        {
            groupCache = new ColorTransitionGroupCache();
            colorTransitionCaches.Add(group.ID, groupCache);
        }

        groupCache.AddGroup(group);
    }

    // Remove only the deleted group's nodes and reconnect the closest matching-filter neighbors.
    public static void RemoveColorTransitionGroup(BaseLightColorEventBoxGroup group)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        if (!ReferenceEquals(cachedColorTransitionMap, map))
        {
            EnsureColorTransitionCache(map);
            return;
        }

        if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
        {
            return;
        }

        groupCache.RemoveGroup(group);
        if (groupCache.IsEmpty)
        {
            colorTransitionCaches.Remove(group.ID);
        }
    }

    // Expose the matched transition endpoint so inner pooling can retain an offscreen ribbon source.
    public static bool TryGetColorTransitionEndTime(BaseLightColorBase source, out float endTime)
    {
        endTime = 0f;
        if (!TryGetFollowingColorTransition(source, out var transition, out _))
            return false;

        endTime = transition.SongBpmTime;
        return true;
    }

    // Find offscreen source groups whose ribbons cross a pool boundary in either scroll direction.
    public static void GetColorTransitionSourceGroupsAt(
        float boundary,
        string trackFilter,
        ISet<BaseLightColorEventBoxGroup> sourceGroups)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        EnsureColorTransitionCache(map);
        colorTransitionQueryResults.Clear();
        colorTransitionIntervals.GetSourcesAt(boundary, colorTransitionQueryResults);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var sourceGroup = colorTransitionQueryResults[sourceIndex].EventBoxGroupData as BaseLightColorEventBoxGroup;
            if (sourceGroup != null && sourceGroup.HasMatchingTrack(trackFilter))
            {
                sourceGroups.Add(sourceGroup);
            }
        }
    }

    // Query indexed source intervals for the inner GLS editor without walking its complete event list.
    public static void GetColorTransitionSourcesAt(
        float boundary,
        BaseEventBoxGroup group,
        string trackFilter,
        List<BaseLightColorBase> sources)
    {
        sources.Clear();
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        EnsureColorTransitionCache(map);
        colorTransitionIntervals.GetSourcesAt(boundary, sources);
        for (var sourceIndex = sources.Count - 1; sourceIndex >= 0; sourceIndex--)
        {
            var source = sources[sourceIndex];
            if (!ReferenceEquals(source.EventBoxGroupData, group) || !source.HasMatchingTrack(trackFilter))
            {
                sources.Remove(source);
            }
        }
    }

    // Keep GLS transition qualification with its timeline owner while sharing the allocation-free interval tree.
    private static void ReplaceColorTransitionSequence(
        IList<BaseLightColorBase> events,
        IDictionary<BaseLightColorBase, BaseLightColorBase> followingEvents)
    {
        for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
        {
            colorTransitionIntervals.Remove(events[eventIndex]);
        }

        foreach (var followingEvent in followingEvents)
        {
            var transition = followingEvent.Value;
            if (transition.UsePrevious == 0 && transition.Easing != (int)EaseType.None)
            {
                colorTransitionIntervals.AddOrReplace(
                    followingEvent.Key,
                    followingEvent.Key.SongBpmTime,
                    transition.SongBpmTime);
            }
        }
    }

    // Build one chronological sequence per group ID and equivalent filter, including overlapping group ranges.
    private static void EnsureColorTransitionCache(BaseDifficulty map)
    {
        if (ReferenceEquals(cachedColorTransitionMap, map))
        {
            return;
        }

        colorTransitionCaches.Clear();
        colorTransitionIntervals.Clear();
        foreach (var group in map.LightColorEventBoxGroups)
        {
            if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
            {
                groupCache = new ColorTransitionGroupCache();
                colorTransitionCaches.Add(group.ID, groupCache);
            }

            groupCache.AddGroupWithoutRewiring(group);
        }

        foreach (var groupCache in colorTransitionCaches.Values)
        {
            groupCache.RebuildAllTransitions();
        }

        cachedColorTransitionMap = map;
    }

    private static int CompareColorEvents(BaseLightColorBase left, BaseLightColorBase right)
    {
        var comparison = left.JsonTime.CompareTo(right.JsonTime);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.BoxIndex.CompareTo(right.BoxIndex);
        return comparison != 0
            ? comparison
            : left.EventBoxGroupData.JsonTime.CompareTo(right.EventBoxGroupData.JsonTime);
    }

    private sealed class ColorTransitionGroupCache
    {
        private readonly List<ColorFilterSequence> sequences = new();
        // Resolve an event's filter timeline without scanning its sibling filters during ribbon rendering.
        private readonly Dictionary<BaseLightColorBase, ColorFilterSequence> sequenceByEvent = new();
        // A cache miss may occur during repeated appearance refreshes, so compare equivalent identities only once per requested clone.
        private readonly HashSet<BaseLightColorBase> identityMissDiagnosticSources = new();

        // Drop an ID cache once it contains no event identities, even if an empty authored box remains.
        public bool IsEmpty => sequenceByEvent.Count == 0;

        public void AddGroupWithoutRewiring(BaseLightColorEventBoxGroup group)
        {
            foreach (var box in group.Boxes)
            {
                var sequence = FindOrCreateSequence(box.IndexFilter);
                sequence.Events.AddRange(box.Events);
                foreach (var evt in box.Events)
                {
                    sequenceByEvent.Add(evt, sequence);
                }
            }
        }

        public void AddGroup(BaseLightColorEventBoxGroup group)
        {
            var modifiedSequences = new Dictionary<ColorFilterSequence, TimeRange>();
            foreach (var box in group.Boxes)
            {
                var sequence = FindOrCreateSequence(box.IndexFilter);
                foreach (var evt in box.Events)
                {
                    var index = sequence.Events.BinarySearch(evt, colorEventComparer);
                    sequence.Events.Insert(index >= 0 ? index : ~index, evt);
                    sequenceByEvent.Add(evt, sequence);
                    AddModifiedTime(modifiedSequences, sequence, evt.JsonTime);
                }
            }

            RewireModifiedSequences(modifiedSequences);
        }

        public void RemoveGroup(BaseLightColorEventBoxGroup group)
        {
            var modifiedSequences = new Dictionary<ColorFilterSequence, TimeRange>();
            foreach (var box in group.Boxes)
            {
                var sequence = FindSequence(box.IndexFilter);
                if (sequence == null)
                {
                    continue;
                }

                foreach (var evt in box.Events)
                {
                    // Removed nodes are absent from the rewired sequence, so clear their old viewport-retention intervals first.
                    colorTransitionIntervals.Remove(evt);
                    if (!sequence.Events.Remove(evt))
                    {
                        continue;
                    }

                    sequence.FollowingEvents.Remove(evt);
                    sequenceByEvent.Remove(evt);
                    AddModifiedTime(modifiedSequences, sequence, evt.JsonTime);
                }

                if (sequence.Events.Count == 0)
                {
                    sequences.Remove(sequence);
                }
            }

            RewireModifiedSequences(modifiedSequences);
        }

        public void RebuildAllTransitions()
        {
            foreach (var sequence in sequences)
            {
                sequence.Events.Sort(CompareColorEvents);
                sequence.RewireAll();
                ReplaceColorTransitionSequence(sequence.Events, sequence.FollowingEvents);
            }
        }

        public bool TryGetFollowingEvent(BaseLightColorBase source, out BaseLightColorBase followingEvent)
        {
            var sourceFound = sequenceByEvent.TryGetValue(source, out var sequence);
            if (sourceFound)
            {
                if (sequence.FollowingEvents.TryGetValue(source, out followingEvent))
                {
                    return true;
                }
            }

            // An equivalent event is only an identity miss when the requested source itself is absent, not merely the final node in a sequence.
            if (sourceFound)
            {
                followingEvent = null;
                return false;
            }

            if (!identityMissDiagnosticSources.Add(source))
            {
                followingEvent = null;
                return false;
            }

            followingEvent = null;
            return false;
        }

        private static void AddModifiedTime(
            Dictionary<ColorFilterSequence, TimeRange> modifiedSequences,
            ColorFilterSequence sequence,
            float time)
        {
            if (modifiedSequences.TryGetValue(sequence, out var range))
            {
                range.Include(time);
                modifiedSequences[sequence] = range;
            }
            else
            {
                modifiedSequences.Add(sequence, new TimeRange(time));
            }
        }

        private static void RewireModifiedSequences(Dictionary<ColorFilterSequence, TimeRange> modifiedSequences)
        {
            foreach (var modifiedSequence in modifiedSequences)
            {
                // Only the edited range and its immediately preceding matching-filter timestamp can change successor links.
                modifiedSequence.Key.RewireRange(modifiedSequence.Value.Minimum, modifiedSequence.Value.Maximum);
                // Reindex only changed filter timelines so scrolling never pays for edit-time cache maintenance.
                ReplaceColorTransitionSequence(
                    modifiedSequence.Key.Events,
                    modifiedSequence.Key.FollowingEvents);
            }
        }

        private ColorFilterSequence FindOrCreateSequence(BaseIndexFilter filter)
        {
            var sequence = FindSequence(filter);
            if (sequence != null)
            {
                return sequence;
            }

            sequence = new ColorFilterSequence(filter);
            sequences.Add(sequence);
            return sequence;
        }

        private ColorFilterSequence FindSequence(BaseIndexFilter filter)
        {
            foreach (var sequence in sequences)
            {
                if (IndexFiltersMatch(sequence.Filter, filter))
                {
                    return sequence;
                }
            }

            return null;
        }
    }

    private sealed class ColorFilterSequence
    {
        public ColorFilterSequence(BaseIndexFilter filter)
        {
            Filter = filter;
        }

        public BaseIndexFilter Filter { get; }
        public List<BaseLightColorBase> Events { get; } = new();
        public Dictionary<BaseLightColorBase, BaseLightColorBase> FollowingEvents { get; } = new();

        public void RewireAll()
        {
            FollowingEvents.Clear();
            RewireRange(float.NegativeInfinity, float.PositiveInfinity);
        }

        public void RewireRange(float minimumTime, float maximumTime)
        {
            if (Events.Count == 0)
            {
                return;
            }

            var firstChangedIndex = LowerBound(minimumTime);
            var startIndex = firstChangedIndex > 0
                ? LowerBound(Events[firstChangedIndex - 1].JsonTime)
                : firstChangedIndex;
            var endIndex = UpperBound(maximumTime);
            var following = endIndex < Events.Count ? Events[endIndex] : null;

            for (var eventIndex = endIndex - 1; eventIndex >= startIndex; eventIndex--)
            {
                var current = Events[eventIndex];
                FollowingEvents.Remove(current);
                if (following != null && following.JsonTime > current.JsonTime)
                {
                    FollowingEvents[current] = following;
                }

                if (following == null || current.JsonTime <= following.JsonTime)
                {
                    following = current;
                }
            }
        }

        private int LowerBound(float time)
        {
            var low = 0;
            var high = Events.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (Events[middle].JsonTime < time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private int UpperBound(float time)
        {
            var low = 0;
            var high = Events.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (Events[middle].JsonTime <= time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }
    }

    private struct TimeRange
    {
        public TimeRange(float time)
        {
            Minimum = time;
            Maximum = time;
        }

        public float Minimum;
        public float Maximum;

        public void Include(float time)
        {
            Minimum = Mathf.Min(Minimum, time);
            Maximum = Mathf.Max(Maximum, time);
        }
    }

    // Index filters are cloned per GLS group, so compare their serialized matching parameters instead of references.
    private static bool IndexFiltersMatch(BaseIndexFilter left, BaseIndexFilter right) =>
        right != null
        && left.Type == right.Type
        && left.Param0 == right.Param0
        && left.Param1 == right.Param1
        && left.Reverse == right.Reverse
        && left.Chunks == right.Chunks
        && left.Random == right.Random
        && left.Seed == right.Seed
        && Mathf.Approximately(left.Limit, right.Limit)
        && left.LimitAffectsType == right.LimitAffectsType;
}
