using System;
using System.Collections.Generic;
using System.Globalization;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventCommon
{
    public const float RotationLayoutAdjustment = 1f / 30f;
    public const float RotationColumnHorizontalOffset = 0.25f - RotationLayoutAdjustment;
    public const float TransformEasingIconHeight = 0.29f - RotationLayoutAdjustment;
    public const float RotationDirectionIconHeight =
        (0.273333f - RotationLayoutAdjustment) -
        ((Beatmap.Containers.GLSEventIconView.RotationDirectionIconSize -
          Beatmap.Containers.GLSEventIconView.PreviousRotationDirectionIconSize) * 0.5f);

    public const float TransformTextVerticalOffset = -0.072f;

    public const float ColorFaceVerticalOffset = -53f / 600f;
    public const float ColorEasingIconHeight = 0.20f + ColorFaceVerticalOffset - (1f / 10f);
    public const float ColorStrobeIconHeight = -0.10f + ColorFaceVerticalOffset + (1f / 15f) - (1f / 25f) + (1f / 50f);
    public const float ColorTertiaryIconHeight = -0.17f + ColorFaceVerticalOffset + (1f / 15f) - (1f / 10f) + (1f / 25f);
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;
    // Partition color-transition timelines by GLS group ID so unrelated light groups never share cache work.
    private static readonly Dictionary<int, ColorTransitionGroupCache> colorTransitionCaches = new();
    private static readonly List<BaseLightColorBase> colorTransitionQueryResults = new();
    private static BaseDifficulty cachedColorTransitionMap;
    private static readonly Dictionary<int, int> colorLightCounts = new();
    private static readonly HashSet<int> dirtyColorTimelines = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorOutgoingIntervals = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorInnerIntervals = new();
    // Collect changed nodes across the whole edit so only affected ribbons refresh.
    private static readonly HashSet<BaseLightColorBase> pendingColorRefreshNodes = new();
    private static readonly HashSet<int> mutatedColorGroupIds = new();
    private static readonly HashSet<int> emptiedColorGroupIds = new();
    private static bool colorRefreshCoversAll;

    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return GetNodePreviewColor(GetLightColor(evt, boost, eventAppearance), eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return GetNodePreviewColor(GetLightStrobeColor(evt, boost, eventAppearance), eventAppearance);
    }

    public static Color GetLightColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance) =>
        BasicEventColorLerp.ApplyBrightness(
            GetBaseColor(evt, boost, eventAppearance),
            evt.Brightness);

    public static Color GetLightStrobeColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance)
    {
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return BasicEventColorLerp.ApplyBrightness(color, evt.StrobeBrightness);
    }

    public static bool PopulateColorTransitionEndpoint(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        Color[] mainColors,
        Color[] strobeColors)
    {
        if (lightCount <= 0
            || mainColors == null
            || strobeColors == null
            || mainColors.Length < lightCount
            || strobeColors.Length < lightCount
            || evt.EventBoxData is not BaseLightColorEventBox box)
        {
            return false;
        }

        for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
        {
            mainColors[lightIndex] = Color.black;
            strobeColors[lightIndex] = Color.black;
        }

        var hasVariation = HasPerLightColorVariation(box, evt);

        var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, lightCount);
        if (indexFilter == null)
        {
            return hasVariation;
        }

        var baseColor = GetBaseColor(evt, boost, eventAppearance);
        var distributionCount = DistributionHelper.GetDistributionCount(indexFilter);
        var affectsFirst = box.BrightnessAffectFirst == 1
            || box.Events.Length == 0
            || !ReferenceEquals(box.Events[0], evt);

        var chunkProgressDenominator = (float)Mathf.Max(indexFilter.VisibleCount - 1, 1);
        var lightProgressDenominator = (float)Mathf.Max(indexFilter.AffectedLightCount - 1, 1);
        foreach (var entry in indexFilter)
        {
            var distributionProgress = entry.AffectedChunkOrder / chunkProgressDenominator;
            var affectedLightProgress = entry.AffectedLightOrder / lightProgressDenominator;
            var brightnessOffset = affectsFirst
                ? DistributionHelper.GetValueStep(
                    entry.DistributionOrder,
                    distributionCount,
                    (DistributionType)box.BrightnessDistributionType,
                    box.BrightnessDistribution,
                    (EaseType)box.Easing)
                : 0f;
            var distributedColor = GLSColorDistribution.ApplyNormal(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            var distributedStrobeColor = GLSColorDistribution.ApplyStrobe(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            distributedColor.a *= evt.Brightness + brightnessOffset;
            distributedStrobeColor.a *= evt.StrobeBrightness;
            mainColors[entry.Element] = distributedColor;
            strobeColors[entry.Element] = distributedStrobeColor;
        }

        return hasVariation;
    }

    private static bool HasPerLightColorVariation(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) =>
        box.IndexFilter.Chunks != 0 || HasColorDistributionOrBrightnessDistribution(box, evt);

    private static bool HasColorDistributionOrBrightnessDistribution(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) 
        => !Mathf.Approximately(box.BrightnessDistribution, 0f)
            || box.ParsedColorDistributions.Count > 0
            || box.ParsedStrobeColorDistributions.Count > 0
            || evt.ParsedColorDistributions.Count > 0
            || evt.ParsedStrobeColorDistributions.Count > 0;

    public static bool IsStrobing(BaseLightColorBase evt)
        => evt.Frequency > 0 
            || (evt.ChromaStrobeInterval is { } interval && interval > 0f);

    public static float GetStrobeFrequency(BaseLightColorBase evt)
    {
        return evt.ChromaStrobeInterval is { } interval && interval > 0f
            ? 1f / interval
            : evt.Frequency;
    }

    internal static float GetStrobeFrequencyScale(BaseDifficulty map, float songBpmTime)
    {
        var baseBpm = map?.SongBpm;
        var localBpm = map?.BpmAtSongBpmTime(Mathf.Max(songBpmTime, 0f));
        return baseBpm is > 0f && localBpm is > 0f
            ? localBpm.Value / baseBpm.Value
            : 1f;
    }

    public static Color GetNodePreviewColor(Color rendererColor, EventAppearanceSO eventAppearance)
    {
        var maximumChannel = Mathf.Max(rendererColor.r, Mathf.Max(rendererColor.g, rendererColor.b));
        var hdrIntensity = Mathf.Max(maximumChannel, 1f);
        if (maximumChannel > 1f)
        {
            rendererColor.r /= maximumChannel;
            rendererColor.g /= maximumChannel;
            rendererColor.b /= maximumChannel;
        }

        var effectiveBrightness = rendererColor.a * hdrIntensity;
        rendererColor.a = 1f;
        return ApplyBrightness(rendererColor, effectiveBrightness, eventAppearance);
    }

    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

    internal static Color GetBaseColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
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

    public static string GetColorInfo(BaseLightColorBase evt) => GetLabelInfo(evt);

    public static string GetRotationInfo(BaseLightRotationBase evt) => GetLabelInfo(evt);

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

    public static string GetTranslationInfo(BaseLightTranslationBase evt) => GetLabelInfo(evt);

    public static string GetFloatFXInfo(BaseFxEventFloat evt) => GetLabelInfo(evt);

    // String callers receive the same plain text as the pooled displays, which write directly into their own buffers.
    private static string GetLabelInfo(BaseGLSEvent evt)
    {
        var buffer = new GLSNodeLabelBuffer();
        buffer.WriteEvent(evt);
        return buffer.ToString();
    }

    public static void ResetColorTransitionLightCounts()
    {
        colorLightCounts.Clear();
        InvalidateColorTransitionTimings();
    }

    public static void InvalidateColorTransitionTimings()
    {
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        // A reset rebuilds every timeline, so the next ribbon refresh must include all nodes.
        colorRefreshCoversAll = true;
        foreach (var entry in colorTransitionCaches)
        {
            entry.Value.InvalidateTimeline();
            dirtyColorTimelines.Add(entry.Key);
        }
    }

    public static void RecomputeColorTransitionTimings(float jsonTime)
    {
        foreach (var entry in colorTransitionCaches)
        {
            if (!entry.Value.HasIncrementalTimeline)
                continue;

            entry.Value.RecomputeSongBpmTimes(jsonTime);
            dirtyColorTimelines.Add(entry.Key);
            mutatedColorGroupIds.Add(entry.Key);
        }

    }

    public static void SetColorTransitionLightCount(int groupId, int lightCount)
    {
        if (lightCount > 0
            && (!colorLightCounts.TryGetValue(groupId, out var previous) || previous != lightCount))
        {
            colorLightCounts[groupId] = lightCount;
            dirtyColorTimelines.Add(groupId);
            mutatedColorGroupIds.Add(groupId);
        }
    }

    private static int ResolveColorTransitionLightCount(BaseLightColorBase node, int lightCount) =>
        lightCount > 0
            ? lightCount
            : node.EventBoxGroupData != null
                && colorLightCounts.TryGetValue(node.EventBoxGroupData.ID, out var registered)
                ? registered
                : 0;

    public static GLSColorTimeline GetColorTimeline(BaseLightColorBase node, int lightCount)
    {
        var song = BeatSaberSongContainer.Instance;
        if (song == null || song.Map == null || node.EventBoxGroupData == null)
            return null;
        lightCount = ResolveColorTransitionLightCount(node, lightCount);
        if (lightCount <= 0)
            return null;
        SetColorTransitionLightCount(node.EventBoxGroupData.ID, lightCount);
        EnsureColorTransitionCache(song.Map);
        return colorTransitionCaches.TryGetValue(node.EventBoxGroupData.ID, out var cache)
            ? cache.GetTimeline(song.Map, lightCount)
            : null;
    }

    public static void UpdateColorTransitionRibbon(
        LightGradientController controller,
        BaseLightColorBase source,
        EventAppearanceSO eventAppearance,
        Func<float, bool> isBoostAt,
        int lightCount,
        bool aggregateSameTimeBoxes = false,
        ColorBoostEventIndex boostEvents = null)
    {
        if (!Settings.Instance.VisualizeGLSLightTransitions)
        {
            controller.SetVisible(false);
            return;
        }

        controller.UpdateColorTimeline(
            GetColorTimeline(source, lightCount),
            source,
            false,
            eventAppearance,
            isBoostAt,
            aggregateSameTimeBoxes,
            boostEvents);
    }

    public static void UpdateIncomingColorTransitionRibbon(
        LightGradientController controller, BaseLightColorBase target, EventAppearanceSO appearance,
        Func<float, bool> isBoostAt, int lightCount, bool aggregateSameTimeBoxes = false,
        ColorBoostEventIndex boostEvents = null)
    {
        if (!Settings.Instance.VisualizeGLSLightTransitions)
        {
            controller.SetVisible(false);
            return;
        }

        controller.UpdateColorTimeline(
            GetColorTimeline(target, lightCount), target, true, appearance, isBoostAt,
            aggregateSameTimeBoxes, boostEvents);
    }

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
        dirtyColorTimelines.Add(group.ID);
        mutatedColorGroupIds.Add(group.ID);
    }

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
        dirtyColorTimelines.Add(group.ID);
        mutatedColorGroupIds.Add(group.ID);
        // Editing a GLS group removes it before adding its replacement. Keep its timeline until the edit
        // finishes so the replacement can reuse it.
        if (groupCache.IsEmpty)
        {
            emptiedColorGroupIds.Add(group.ID);
        }
    }

    // Delete an empty timeline only after the replacement group has had a chance to reuse it.
    private static void CommitEmptiedColorCaches()
    {
        foreach (var groupId in emptiedColorGroupIds)
        {
            if (colorTransitionCaches.TryGetValue(groupId, out var cache) && cache.IsEmpty)
            {
                cache.ClearTimelineIntervals();
                colorTransitionCaches.Remove(groupId);
            }
        }

        emptiedColorGroupIds.Clear();
    }

    // Replacing a timeline invalidates its previous changed-node list, so that batch needs a full ribbon refresh.
    public static bool TryCollectChangedColorTransitions(
        HashSet<BaseLightColorBase> changedNodes,
        Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates)
    {
        var incremental = !colorRefreshCoversAll;
        foreach (var groupId in mutatedColorGroupIds)
        {
            // IDs absent from the environment have no ribbons and must not force other IDs to refresh.
            if (!colorLightCounts.ContainsKey(groupId)
                || !colorTransitionCaches.TryGetValue(groupId, out var cache))
            {
                continue;
            }

            if (!cache.HasIncrementalTimeline || cache.ConsumeRebuiltFlag())
            {
                incremental = false;
                continue;
            }

            cache.CollectChangedNodes(pendingColorRefreshNodes);
        }

        CommitEmptiedColorCaches();
        mutatedColorGroupIds.Clear();
        colorRefreshCoversAll = false;

        if (!incremental)
        {
            pendingColorRefreshNodes.Clear();
            return false;
        }

        changedNodes.UnionWith(pendingColorRefreshNodes);
        pendingColorRefreshNodes.Clear();
        foreach (var node in changedNodes)
        {
            // The outer view combines boxes at the same relative beat into one ribbon.
            var owner = node != null ? node.EventBoxGroupData : null;
            if (owner == null)
            {
                continue;
            }

            if (!changedAggregates.TryGetValue(owner, out var times))
            {
                changedAggregates[owner] = times = new HashSet<float>();
            }

            times.Add(node.RelativeJsonTime);
        }

        return true;
    }

    // Different lights can reach different following nodes. Keep the source group loaded until its last
    // ribbon ends.
    public static bool TryGetColorTransitionEndTime(BaseLightColorBase source, out float endTime)
    {
        endTime = 0f;
        var timeline = GetColorTimeline(source, 0);
        return timeline != null && timeline.TryGetBounds(source, out _, out endTime);
    }

    public static bool TryGetColorRibbonBounds(BaseLightColorBase node, int lightCount, out float start, out float end)
    {
        start = node.SongBpmTime;
        end = start;
        var timeline = GetColorTimeline(node, lightCount);
        return timeline != null && colorTransitionCaches[node.EventBoxGroupData.ID].TryGetInnerBounds(node, out start, out end);
    }

    private static bool IsColorTransitionContainer(ObjectContainer container) =>
        container != null
        && (container is GLSEventContainer { EventData: BaseLightColorBase }
            or GLSGroupContainer { PreviewEventData: BaseLightColorBase });

    public static bool IsColorTransitionRibbonHit(ObjectContainer container) =>
        container != null
        && IsColorTransitionRibbonHit()
        && ReferenceEquals(classifiedRibbonOwner, container);

    // A pooled container may represent a different event on the next hover.
    private static GameObject classifiedRibbonHit;
    private static ObjectContainer classifiedRibbonOwner;
    private static LightGradientController classifiedRibbonController;

    public static bool IsColorTransitionRibbonHit()
    {
        if (!ReferenceEquals(classifiedRibbonHit, BeatmapRaycastCache.FirstHit))
        {
            classifiedRibbonHit = BeatmapRaycastCache.FirstHit;
            var ribbon = classifiedRibbonHit != null
                ? classifiedRibbonHit.GetComponentInParent<LightGradientController>()
                : null;
            classifiedRibbonOwner = ribbon != null ? ribbon.GetComponentInParent<ObjectContainer>() : null;
            classifiedRibbonController = ribbon;
        }

        // Recheck the event type because the pooled owner may have been rebound since the hierarchy was cached.
        return IsColorTransitionContainer(classifiedRibbonOwner);
    }

    public static bool TryGetColorTransitionTarget(
        ObjectContainer container,
        BaseLightColorBase source,
        out BaseLightColorBase transition)
    {
        transition = null;
        if (source == null || !IsColorTransitionRibbonHit(container))
            return false;
        if (source.EventBoxGroupData != null && colorLightCounts.TryGetValue(source.EventBoxGroupData.ID, out var count))
        {
            var timeline = GetColorTimeline(source, count);
            if (timeline != null)
            {
                // Match hover selection to the shader's per-light strip and active interval.
                if (BeatmapRaycastCache.FirstHitPoint is { } point && classifiedRibbonController.ColorTimelineDuration > 0f)
                {
                    var uv = classifiedRibbonController.GetHitUv(point);
                    var light = count - 1 - Mathf.Clamp(Mathf.FloorToInt(uv.y * count), 0, count - 1);
                    var time = classifiedRibbonController.ColorTimelineStart + (uv.x * classifiedRibbonController.ColorTimelineDuration);
                    var incoming = classifiedRibbonController.IsIncomingColorTransition;
                    LightColorEventStateData state;
                    var found = incoming
                        ? classifiedRibbonController.AggregatesSameTimeBoxes
                            ? timeline.TryGetIncomingAtGroupTime(source, light, out state)
                            : timeline.TryGetIncomingSegment(source, light, out state)
                        : classifiedRibbonController.AggregatesSameTimeBoxes
                            ? timeline.TryGetOutgoingAtGroupTime(source, light, out state)
                            : timeline.TryGetOutgoing(source, light, out state);
                    if (!found)
                    {
                        return false;
                    }
                    if (incoming)
                    {
                        // Incoming lookups exclude the initial off state, so only real preceding nodes can be
                        // hover targets.
                        if (classifiedRibbonController.AggregatesSameTimeBoxes
                                || ReferenceEquals(
                                    state.Base.EventBoxGroupData, source.EventBoxGroupData)
                            || time > state.EndTime
                            || time < Mathf.Max(state.StartTime, timeline.HeadBound))
                        {
                            return false;
                        }
                        transition = state.Next != null ? state.Next.Base : null;
                        return transition != null;
                    }
                    if (state.EndTime == float.MaxValue || time < state.StartTime || time > state.EndTime)
                    {
                        return false;
                    }
                    transition = state.Next.Base;
                    return true;
                }

                if (classifiedRibbonController.IsIncomingColorTransition)
                {
                    transition = source;
                    return true;
                }

                for (var light = 0; light < count; light++)
                {
                    LightColorEventStateData state;
                    var found = classifiedRibbonController.AggregatesSameTimeBoxes
                        ? timeline.TryGetOutgoingAtGroupTime(source, light, out state)
                        : timeline.TryGetOutgoing(source, light, out state);
                    if (!found || state.EndTime == float.MaxValue)
                        continue;
                    transition = state.Next.Base;
                    return true;
                }

                return false;
            }
        }

        return false;
    }

    // Hitting an unlit ribbon strip must not fall through to editing the node body. A null target means no
    // edit.
    public static bool IsColorRibbonHover(ObjectContainer container, BaseLightColorBase source, out BaseLightColorBase target)
    {
        target = null;
        if (!IsColorTransitionRibbonHit(container))
            return false;
        TryGetColorTransitionTarget(container, source, out target);
        return true;
    }

    // Find offscreen source groups whose ribbons cross a pool boundary in either scroll direction.
    public static void GetColorTransitionSourceGroupsAt(
        float boundary,
        string trackFilter,
        ISet<BaseLightColorEventBoxGroup> sourceGroups) =>
        GetColorTransitionSourceGroupsAt(boundary, trackFilter, null, sourceGroups);

    public static void GetColorTransitionSourceGroupsAt(
        float boundary,
        string trackFilter,
        ISet<int> activeGroupIds,
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
        GetColorRibbonSourcesAt(boundary, colorTransitionQueryResults, inner: false);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var sourceGroup = colorTransitionQueryResults[sourceIndex].EventBoxGroupData as BaseLightColorEventBoxGroup;
            if (sourceGroup != null
                && (activeGroupIds == null || activeGroupIds.Contains(sourceGroup.ID))
                && sourceGroup.HasMatchingTrack(trackFilter))
            {
                sourceGroups.Add(sourceGroup);
            }
        }
    }

    public static void GetColorTransitionSourcesAt(
        float boundary,
        string trackFilter,
        ISet<BaseGLSEvent> sources) =>
        GetColorTransitionSourcesAt(boundary, trackFilter, null, sources);

    public static void GetColorTransitionSourcesAt(
        float boundary,
        string trackFilter,
        ISet<int> activeGroupIds,
        ISet<BaseGLSEvent> sources)
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
        GetColorRibbonSourcesAt(boundary, colorTransitionQueryResults, inner: true);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var source = colorTransitionQueryResults[sourceIndex];
            if ((activeGroupIds == null || activeGroupIds.Contains(source.EventBoxGroupData.ID))
                && source.HasMatchingTrack(trackFilter))
            {
                sources.Add(source);
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
        GetColorRibbonSourcesAt(boundary, sources, inner: true);
        for (var sourceIndex = sources.Count - 1; sourceIndex >= 0; sourceIndex--)
        {
            var source = sources[sourceIndex];
            if (!ReferenceEquals(source.EventBoxGroupData, group) || !source.HasMatchingTrack(trackFilter))
            {
                sources.RemoveAt(sourceIndex);
            }
        }
    }

    private static void GetColorRibbonSourcesAt(float boundary, List<BaseLightColorBase> sources, bool inner)
    {
        (inner ? colorInnerIntervals : colorOutgoingIntervals).GetSourcesAt(boundary, sources);
    }

    private static void EnsureColorTransitionCache(BaseDifficulty map)
    {
        if (ReferenceEquals(cachedColorTransitionMap, map))
        {
            RefreshDirtyColorTimelines(map);
            return;
        }

        colorTransitionCaches.Clear();
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        dirtyColorTimelines.Clear();
        pendingColorRefreshNodes.Clear();
        mutatedColorGroupIds.Clear();
        emptiedColorGroupIds.Clear();
        colorRefreshCoversAll = false;
        foreach (var group in map.LightColorEventBoxGroups)
        {
            if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
            {
                groupCache = new ColorTransitionGroupCache();
                colorTransitionCaches.Add(group.ID, groupCache);
            }

            groupCache.AddGroupWithoutRewiring(group);
        }

        foreach (var entry in colorTransitionCaches)
        {
            if (colorLightCounts.TryGetValue(entry.Key, out var lightCount))
            {
                entry.Value.GetTimeline(map, lightCount);
            }
        }

        cachedColorTransitionMap = map;
    }

    private static void RefreshDirtyColorTimelines(BaseDifficulty map)
    {
        foreach (var id in dirtyColorTimelines)
        {
            if (colorTransitionCaches.TryGetValue(id, out var cache)
                && colorLightCounts.TryGetValue(id, out var count))
            {
                cache.GetTimeline(map, count);
            }
        }

        dirtyColorTimelines.Clear();
    }

    private sealed class ColorTransitionGroupCache
    {
        private readonly Dictionary<BaseLightColorEventBoxGroup, long> groups = new();
        private readonly List<BaseLightColorEventBoxGroup> orderedGroups = new();
        private readonly HashSet<BaseLightColorBase> pendingNodes = new();
        private readonly Dictionary<BaseLightColorBase, (float Start, float End)> innerBounds = new();
        private long nextGroupOrder;
        private GLSColorTimeline timeline;
        private bool timelineDirty = true;

        public bool IsEmpty => groups.Count == 0;

        public bool HasIncrementalTimeline => timeline != null && !timelineDirty;

        // A rebuild replaces every segment, so the batch that first observes it falls back to a full refresh.
        private bool rebuiltSinceCollect;
        public bool ConsumeRebuiltFlag()
        {
            var wasRebuilt = rebuiltSinceCollect;
            rebuiltSinceCollect = false;
            return wasRebuilt;
        }

        // Keep changed nodes in pendingNodes until their intervals refresh, and also report them to the
        // ribbon refresh.
        public void CollectChangedNodes(ISet<BaseLightColorBase> target)
        {
            target.UnionWith(pendingNodes);
            timeline.EnsureUpdated();
            pendingNodes.UnionWith(timeline.ChangedNodes);
            target.UnionWith(timeline.ChangedNodes);
            timeline.ClearChangedNodes();
        }

        public GLSColorTimeline GetTimeline(BaseDifficulty map, int lightCount)
        {
            if (timelineDirty || timeline == null || timeline.LightCount != lightCount)
            {
                // Replacing an existing timeline requires a full ribbon refresh. The initial build has no
                // loaded ribbons to refresh.
                rebuiltSinceCollect = timeline != null;
                ClearTimelineIntervals();
                timeline = new GLSColorTimeline(map, lightCount, GetOrderedGroups());
                timelineDirty = false;
                pendingNodes.Clear();
                foreach (var source in timeline.Sources)
                {
                    pendingNodes.Add(source);
                    for (var light = 0; light < lightCount; light++)
                    {
                        if (timeline.TryGetOutgoing(source, light, out var state) && state.EndTime < float.MaxValue)
                            pendingNodes.Add(state.Next.Base);
                    }
                }
            }
            // Finish the remove/add pair before collecting changes, so each interval and ribbon refreshes
            // once.
            timeline.EnsureUpdated();
            pendingNodes.UnionWith(timeline.ChangedNodes);
            pendingColorRefreshNodes.UnionWith(timeline.ChangedNodes);
            timeline.ClearChangedNodes();
            RefreshChangedIntervals();
            return timeline;
        }

        // Equal-beat groups must keep their serialized order because the first matching box controls each
        // light.
        private IReadOnlyList<BaseLightColorEventBoxGroup> GetOrderedGroups()
        {
            orderedGroups.Clear();
            orderedGroups.AddRange(groups.Keys);
            orderedGroups.Sort((left, right) =>
            {
                var time = left.JsonTime.CompareTo(right.JsonTime);
                return time != 0 ? time : groups[left].CompareTo(groups[right]);
            });
            return orderedGroups;
        }

        private void RefreshChangedIntervals()
        {
            foreach (var node in pendingNodes)
            {
                colorOutgoingIntervals.Remove(node);
                colorInnerIntervals.Remove(node);
                innerBounds.Remove(node);
                if (timeline.TryGetBounds(node, out var start, out var end))
                {
                    colorOutgoingIntervals.AddOrReplace(node, start, end);
                    IncludeInnerBounds(node, start, end);
                }
                for (var light = 0; light < timeline.LightCount; light++)
                {
                    if (!timeline.TryGetIncomingSegment(node, light, out var previous))
                    {
                        continue;
                    }

                    // Only a preceding node from another group needs an incoming ribbon in this group's inner
                    // view.
                    if (!ReferenceEquals(previous.Base.EventBoxGroupData, node.EventBoxGroupData))
                    {
                        IncludeInnerBounds(
                            node,
                            Mathf.Max(previous.StartTime, timeline.HeadBound),
                            previous.EndTime);
                    }
                }
                if (innerBounds.TryGetValue(node, out var bounds))
                    colorInnerIntervals.AddOrReplace(node, bounds.Start, bounds.End);
            }
            pendingNodes.Clear();
        }

        public bool TryGetInnerBounds(BaseLightColorBase node, out float start, out float end)
        {
            var found = innerBounds.TryGetValue(node, out var bounds);
            start = bounds.Start;
            end = bounds.End;
            return found;
        }

        private void IncludeInnerBounds(BaseLightColorBase node, float start, float end)
        {
            if (innerBounds.TryGetValue(node, out var previous))
            {
                start = Mathf.Min(start, previous.Start);
                end = Mathf.Max(end, previous.End);
            }
            innerBounds[node] = (start, end);
        }

        public void InvalidateTimeline() => timelineDirty = true;

        public void RecomputeSongBpmTimes(float jsonTime) => timeline.RecomputeSongBpmTimes(jsonTime);

        public void ClearTimelineIntervals()
        {
            foreach (var node in innerBounds.Keys)
            {
                colorOutgoingIntervals.Remove(node);
                colorInnerIntervals.Remove(node);
            }
            innerBounds.Clear();
        }

        public void AddGroupWithoutRewiring(BaseLightColorEventBoxGroup group)
        {
            groups.Add(group, nextGroupOrder++);
            timelineDirty = true;
        }

        public void AddGroup(BaseLightColorEventBoxGroup group)
        {
            if (!groups.TryAdd(group, nextGroupOrder++))
                return;

            if (timeline != null && !timelineDirty)
            {
                timeline.AddGroup(group);
            }
        }

        public void RemoveGroup(BaseLightColorEventBoxGroup group)
        {
            if (!groups.Remove(group))
                return;

            if (timeline != null && !timelineDirty)
            {
                timeline.RemoveGroup(group);
            }
        }
    }
}

// Each pooled ribbon owns a point-sampled per-light timeline table.
public sealed class GLSColorTransitionPreview : IDisposable
{
    private static readonly int lightDistributionTextureId = Shader.PropertyToID("_LightDistributionTex");
    private static readonly int lightDistributionWidthId = Shader.PropertyToID("_LightDistributionWidth");
    private static readonly int useLightDistributionId = Shader.PropertyToID("_UseLightDistribution");
    private static readonly int useLightTimelineId = Shader.PropertyToID("_UseLightTimeline");
    private static readonly int lightTimelineDurationId = Shader.PropertyToID("_LightTimelineDuration");
    private static readonly int lightTimelineStartId = Shader.PropertyToID("_LightTimelineStart");
    private static readonly int ribbonEdgePaddingId = Shader.PropertyToID("_RibbonEdgePadding");
    private static readonly int useRibbonPlaneId = Shader.PropertyToID("_UseRibbonPlane");
    private static readonly int lightBoostDataId = Shader.PropertyToID("_LightBoostData");
    private static readonly int lightBoostCountId = Shader.PropertyToID("_LightBoostCount");
    private static readonly int lightBoostInitialId = Shader.PropertyToID("_LightBoostInitial");
    // When a boost boundary crosses the ribbon span, packed records keep the normal palette and the
    // shader swaps to four raw boosted endpoint colors at each appended boundary. Uniform spans pack
    // the selected palette directly, so they need neither payload nor shader reads.
    private static readonly Func<float, bool> uniformNormalPalette = _ => false;
    private static readonly Func<float, bool> uniformBoostPalette = _ => true;
    private LightColorEventStateData[] timelineStates = Array.Empty<LightColorEventStateData>();
    private LightColorTween[] timelineTweens = Array.Empty<LightColorTween>();
    private readonly LightColorTween boostAltTween = new();
    private readonly List<Vector2> boostBoundaries = new();
    private readonly List<LightColorEventStateData> timelineNeighborStates = new();
    private int[] timelineNeighborOffsets = Array.Empty<int>();

    private Texture2D texture;
    private int textureLightCount;
    private Color[] textureColors = Array.Empty<Color>();
    // Scrubbing often produces the same ribbon pixels. Keep the last upload to avoid repeating
    // SetPixels/Apply.
    private Color[] uploadedColors = Array.Empty<Color>();
    private bool uploadDirty = true;
    // Lights can share endpoint times. Cache boost at those times for this ribbon refresh.
    private readonly Dictionary<float, bool> boostCache = new();
    private Func<float, bool> cachedBoostResolver;
    private Func<float, bool> activeBoostResolver;

    public bool UpdateTimeline(
        GLSColorTimeline timeline, BaseLightColorBase owner, bool incoming,
        EventAppearanceSO appearance, Func<float, bool> isBoostAt, MaterialPropertyBlock properties,
        out float start, out float end, bool aggregateSameTimeBoxes = false,
        ColorBoostEventIndex boostEvents = null)
    {
        start = incoming ? float.PositiveInfinity : owner.SongBpmTime;
        end = float.NegativeInfinity;
        properties.SetFloat(useLightTimelineId, 0f);
        // A recycled ribbon must not keep a previous upload's boost payload.
        properties.SetFloat(lightBoostCountId, 0f);
        properties.SetFloat(lightBoostInitialId, 0f);
        if (timeline == null)
            return false;
        var count = timeline.LightCount;
        // Wait until a visible span is found before allocating texture rows and tweens.
        if (timelineStates.Length != count)
        {
            timelineStates = new LightColorEventStateData[count];
        }

        for (var light = 0; light < count; light++)
        {
            LightColorEventStateData state;
            var found = incoming
                ? aggregateSameTimeBoxes
                    ? timeline.TryGetIncomingAtGroupTime(owner, light, out state)
                    : timeline.TryGetIncomingSegment(owner, light, out state)
                : aggregateSameTimeBoxes
                    ? timeline.TryGetOutgoingAtGroupTime(owner, light, out state)
                    : timeline.TryGetOutgoing(owner, light, out state);
            if (found && incoming)
            {
                // In the outer view, each node draws its own outgoing ribbon. The inner view also needs
                // incoming ribbons from groups whose nodes are outside the opened group.
                if (aggregateSameTimeBoxes
                    || ReferenceEquals(state.Base.EventBoxGroupData, owner.EventBoxGroupData))
                {
                    found = false;
                }
            }
            else if (found && state.EndTime == float.MaxValue)
            {
                var held = state.UsePrevious ? (LightColorEventStateData)state.Previous : state;
                if (!GLSColorTimeline.IsLit(held))
                {
                    found = false;
                }
            }
            if (!found)
            {
                state = null;
            }
            else
            {
                var segmentStart = Mathf.Max(state.StartTime, timeline.HeadBound);
                var segmentEnd = Mathf.Min(state.EndTime, timeline.TailBound);
                if (segmentEnd <= segmentStart)
                {
                    state = null;
                }
                else
                {
                    start = Mathf.Min(start, segmentStart);
                    end = Mathf.Max(end, segmentEnd);
                }
            }
            timelineStates[light] = state;
        }
        if (!(end > start))
            return false;

        timelineNeighborStates.Clear();
        if (timelineNeighborOffsets.Length != count + 1)
            timelineNeighborOffsets = new int[count + 1];
        for (var light = 0; light < count; light++)
        {
            timelineNeighborOffsets[light] = timelineNeighborStates.Count;
            var lowerOwned = light > 0 ? timelineStates[light - 1] : null;
            var upperOwned = light + 1 < count ? timelineStates[light + 1] : null;
            if (lowerOwned != null || upperOwned != null)
            {
                var neighborStart = lowerOwned != null ? lowerOwned.StartTime : upperOwned.StartTime;
                var neighborEnd = lowerOwned != null ? lowerOwned.EndTime : upperOwned.EndTime;
                if (upperOwned != null)
                {
                    neighborStart = Mathf.Min(neighborStart, upperOwned.StartTime);
                    neighborEnd = Mathf.Max(neighborEnd, upperOwned.EndTime);
                }
                timeline.AppendRibbonNeighborStates(owner, timelineStates[light], light,
                    Mathf.Max(start, neighborStart), Mathf.Min(end, neighborEnd),
                    aggregateSameTimeBoxes, timelineNeighborStates);
            }
        }

        timelineNeighborOffsets[count] = timelineNeighborStates.Count;

        // Only boost boundaries within this ribbon's span are packed. A uniform
        // span resolves its palette once; a crossed span appends a boundary table plus the boosted
        // endpoint colors for every record so the shader swaps palettes without splitting segments.
        var boostInitial = false;
        boostBoundaries.Clear();
        Func<float, bool> packResolver;
        if (boostEvents != null)
        {
            var boundaryIndex = boostEvents.GetChunkCursor(start, out boostInitial);
            var boostCount = boostEvents.Count;
            while (boundaryIndex < boostCount)
            {
                var boundaryTime = boostEvents.GetSongBpmTime(boundaryIndex);
                if (boundaryTime > end)
                {
                    break;
                }

                boostBoundaries.Add(new Vector2(
                    boundaryTime - start,
                    boostEvents.IsBoostAtIndex(boundaryIndex) ? 1f : 0f));
                boundaryIndex++;
            }

            packResolver = boostBoundaries.Count > 0
                ? uniformNormalPalette
                : boostInitial
                    ? uniformBoostPalette
                    : uniformNormalPalette;
        }
        else
        {
            // Callers without the boost index keep the original per-endpoint resolution.
            packResolver = BeginBoostMemoization(isBoostAt);
        }

        var recordCount = count + (long)timelineNeighborStates.Count;
        var boostPayloadTexels = boostBoundaries.Count > 0
            ? boostBoundaries.Count + (4L * recordCount)
            : 0L;
        EnsureCapacity(count, (10L * count) + (9L * timelineNeighborStates.Count) + boostPayloadTexels);

        var boostTableBase = (10 * count) + (9 * timelineNeighborStates.Count);
        var altColorsBase = boostTableBase + boostBoundaries.Count;
        // -1 marks records with no alternate boosted colors, keeping the original pack path untouched.
        var packedAltColorsBase = boostBoundaries.Count > 0 ? altColorsBase : -1;
        var textureWidth = texture.width;
        for (var light = 0; light < count; light++)
        {
            var x = count - light - 1;
            var state = timelineStates[light];
            var neighborOffset = timelineNeighborOffsets[light];
            var neighborCount = timelineNeighborOffsets[light + 1] - neighborOffset;
            // Even an inactive light needs neighboring colors in its header for blending strip edges.
            if (state == null)
            {
                for (var row = 0; row < 4; row++)
                    textureColors[(row * count) + x] = Color.black;
                for (var row = 4; row < 9; row++)
                    textureColors[(row * count) + x] = default;
                textureColors[(4 * count) + x] = new Color(0f, -1f, 0f, -1f);
                textureColors[(7 * count) + x] = new Color(0f, 0f,
                    neighborCount > 0 ? 0.015625f : 0f, 0f);
            }
            else
            {
                PackTimelineState(
                    timeline,
                    state,
                    appearance,
                    packResolver,
                    start,
                    x,
                    count,
                    light,
                    neighborCount > 0,
                    owner,
                    aggregateSameTimeBoxes,
                    packedAltColorsBase,
                    x);
            }
            // Store neighbors without padding every light to the longest list. Texture coordinates
            // keep the offset exact even when the packed table exceeds float's exact integer range.
            var neighborTexel = (10 * count) + (9 * neighborOffset);
            textureColors[(9 * count) + x] = new Color(
                neighborCount,
                neighborTexel % textureWidth,
                neighborTexel / textureWidth,
                0f);
            for (var neighbor = 0; neighbor < neighborCount; neighbor++)
            {
                PackTimelineState(
                    timeline,
                    timelineNeighborStates[neighborOffset + neighbor],
                    appearance,
                    packResolver,
                    start,
                    neighborTexel + (9 * neighbor),
                    1,
                    light,
                    false,
                    owner,
                    aggregateSameTimeBoxes,
                    packedAltColorsBase,
                    count + neighborOffset + neighbor);
            }
        }
        for (var boundary = 0; boundary < boostBoundaries.Count; boundary++)
        {
            var packedBoundary = boostBoundaries[boundary];
            textureColors[boostTableBase + boundary] = new Color(packedBoundary.x, packedBoundary.y, 0f, 0f);
        }

        UploadIfChanged();
        properties.SetTexture(lightDistributionTextureId, texture);
        properties.SetFloat(lightDistributionWidthId, count);
        properties.SetFloat(useLightDistributionId, 1f);
        properties.SetFloat(useLightTimelineId, 1f);
        properties.SetFloat(lightTimelineDurationId, end - start);
        properties.SetFloat(lightTimelineStartId, start);
        // Physical XY pairs keep the appended tables exact past float's exact integer range.
        properties.SetVector(lightBoostDataId, new Vector4(
            altColorsBase % textureWidth,
            altColorsBase / textureWidth,
            boostTableBase % textureWidth,
            boostTableBase / textureWidth));
        properties.SetFloat(lightBoostCountId, boostBoundaries.Count);
        properties.SetFloat(lightBoostInitialId, boostInitial ? 1f : 0f);
        // The vertex fringe needs the unit XY mesh used by timeline ribbons.
        properties.SetFloat(ribbonEdgePaddingId, 1f);
        return true;
    }

    private void PackTimelineState(
        GLSColorTimeline timeline,
        LightColorEventStateData state,
        EventAppearanceSO appearance,
        Func<float, bool> boostResolver,
        float start,
        int texelOffset,
        int texelStride,
        int light,
        bool hasNeighbors,
        BaseLightColorBase owner,
        bool aggregateSameTimeBoxes,
        int altColorsBase,
        int altOrdinal)
    {
        var tween = timelineTweens[light];
        timeline.ConfigureTween(tween, state, appearance, boostResolver);
        var startEmits = (tween.StartAlpha > 0f && tween.StartColor.maxColorComponent > 0f)
            || (tween.StartStrobeFrequency > 0f && tween.StartStrobeBrightness > 0f
                && tween.StartStrobeColor.maxColorComponent > 0f);
        var endEmits = (tween.EndAlpha > 0f && tween.EndColor.maxColorComponent > 0f)
            || (tween.EndStrobeFrequency > 0f && tween.EndStrobeBrightness > 0f
                && tween.EndStrobeColor.maxColorComponent > 0f);
        if (altColorsBase >= 0)
        {
            // Join flags keep an edge lit when either palette emits across it, so a boost swap cannot
            // open a seam at a lit join.
            timeline.ConfigureTween(boostAltTween, state, appearance, uniformBoostPalette);
            startEmits |= (boostAltTween.StartAlpha > 0f && boostAltTween.StartColor.maxColorComponent > 0f)
                || (boostAltTween.StartStrobeFrequency > 0f && boostAltTween.StartStrobeBrightness > 0f
                    && boostAltTween.StartStrobeColor.maxColorComponent > 0f);
            endEmits |= (boostAltTween.EndAlpha > 0f && boostAltTween.EndColor.maxColorComponent > 0f)
                || (boostAltTween.EndStrobeFrequency > 0f && boostAltTween.EndStrobeBrightness > 0f
                    && boostAltTween.EndStrobeColor.maxColorComponent > 0f);
            var altIndex = altColorsBase + (4 * altOrdinal);
            textureColors[altIndex] = boostAltTween.StartColor;
            textureColors[altIndex + 1] = boostAltTween.EndColor;
            textureColors[altIndex + 2] = boostAltTween.StartStrobeColor;
            textureColors[altIndex + 3] = boostAltTween.EndStrobeColor;
        }

        var joinedStart = startEmits && timeline.IsStateVisibleInRibbonLane(
            (LightColorEventStateData)state.Previous, owner, aggregateSameTimeBoxes);
        var joinedEnd = endEmits && timeline.IsStateVisibleInRibbonLane(
            (LightColorEventStateData)state.Next, owner, aggregateSameTimeBoxes);
        // Owned records occupy columns in the header. Neighbor records use nine consecutive texels.
        textureColors[texelOffset] = BasicEventColorLerp.ApplyBrightness(tween.StartColor, tween.StartAlpha);
        textureColors[texelOffset + texelStride] = BasicEventColorLerp.ApplyBrightness(tween.EndColor, tween.EndAlpha);
        textureColors[texelOffset + (2 * texelStride)] = BasicEventColorLerp.ApplyBrightness(tween.StartStrobeColor, tween.StartStrobeBrightness);
        textureColors[texelOffset + (3 * texelStride)] = BasicEventColorLerp.ApplyBrightness(tween.EndStrobeColor, tween.EndStrobeBrightness);
        var rowEndAlpha = state.EndTime == float.MaxValue ? timeline.TailBound : tween.EndTimeAlpha;
        var rowEndColor = state.EndTime == float.MaxValue ? timeline.TailBound : tween.EndTimeColor;
        textureColors[texelOffset + (4 * texelStride)] = new Color(
            tween.StartTimeAlpha - start,
            rowEndAlpha - start,
            tween.StartTimeColor - start,
            rowEndColor - start);
        textureColors[texelOffset + (5 * texelStride)] = new Color(
            tween.StartStrobeFrequency,
            tween.EndStrobeFrequency,
            tween.StartAlpha,
            tween.EndAlpha);
        textureColors[texelOffset + (6 * texelStride)] = new Color(
            tween.StartStrobeBrightness,
            tween.EndStrobeBrightness,
            tween.StartColor.a,
            tween.EndColor.a);
        textureColors[texelOffset + (7 * texelStride)] = new Color(
            tween.StartStrobeColor.a,
            tween.EndStrobeColor.a,
            (int)tween.ColorLerpType + (joinedStart ? 0.25f : 0f) + (joinedEnd ? 0.5f : 0f)
                + (hasNeighbors ? 0.015625f : 0f),
            (tween.StrobeFade ? 1f : 0f) + (tween.ComposeAlphaAtColorEndpoints ? 2f : 0f)
                + ((tween.StartStrobePulseWidth - 0.5f) * 0.5f));
        var shaderIds = tween.EasingShaderIds;
        shaderIds.w += (tween.EndStrobePulseWidth - 0.5f) * 0.5f;
        textureColors[texelOffset + (8 * texelStride)] = shaderIds;
    }

    private Func<float, bool> BeginBoostMemoization(Func<float, bool> isBoostAt)
    {
        boostCache.Clear();
        activeBoostResolver = isBoostAt;
        if (isBoostAt == null)
        {
            return null;
        }

        return cachedBoostResolver ??= CachedIsBoostAt;
    }

    private bool CachedIsBoostAt(float time)
    {
        if (boostCache.TryGetValue(time, out var boost))
        {
            return boost;
        }

        boost = activeBoostResolver(time);
        boostCache[time] = boost;
        return boost;
    }

    // Skip uploads when scrubbing produces identical pixels. Exact equality is intentional: unchanged inputs
    // follow the same calculations.
    private bool UploadIfChanged()
    {
        if (!uploadDirty && uploadedColors.AsSpan().SequenceEqual(textureColors))
        {
            return false;
        }

        if (uploadedColors.Length != textureColors.Length)
        {
            uploadedColors = new Color[textureColors.Length];
        }

        Array.Copy(textureColors, uploadedColors, textureColors.Length);
        texture.SetPixels(textureColors);
        texture.Apply(false, false);
        uploadDirty = false;
        return true;
    }

    private void EnsureCapacity(int lightCount, long texelCount)
    {
        // Column headers bound reads, so unused capacity can retain data from a previous update.
        if (texture != null && textureLightCount == lightCount && textureColors.LongLength >= texelCount)
        {
            return;
        }

        var maximumSize = SystemInfo.maxTextureSize;
        // Keep the logical row-major table intact, wrapping it across wider physical rows when needed.
        var width = (int)Math.Min(maximumSize,
            Math.Max(lightCount, (texelCount + maximumSize - 1) / maximumSize));
        var height = (int)((texelCount + width - 1) / width);
        ReleaseTexture();
        textureLightCount = lightCount;
        textureColors = new Color[checked(width * height)];
        // UpdateTimeline has already filled these states; preserve them when resizing only the texture.
        if (timelineStates.Length != lightCount)
        {
            timelineStates = new LightColorEventStateData[lightCount];
        }

        timelineTweens = new LightColorTween[lightCount];
        for (var light = 0; light < lightCount; light++)
            timelineTweens[light] = new LightColorTween();

        texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true)
        {
            name = "GLS Light Transition Preview",
            // Every fetch lands on an exact texel centre; the strip-edge AA blends evaluated colors, not texels.
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        // A freshly allocated texture is blank, so the next pack must upload regardless of payload equality.
        uploadDirty = true;
    }

    public void Dispose()
    {
        ReleaseTexture();
        textureColors = Array.Empty<Color>();
        uploadedColors = Array.Empty<Color>();
        uploadDirty = true;
        boostCache.Clear();
        activeBoostResolver = null;
        timelineStates = Array.Empty<LightColorEventStateData>();
        timelineTweens = Array.Empty<LightColorTween>();
        timelineNeighborStates.Clear();
        timelineNeighborOffsets = Array.Empty<int>();
    }

    private void ReleaseTexture()
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(texture);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        texture = null;
    }
}
