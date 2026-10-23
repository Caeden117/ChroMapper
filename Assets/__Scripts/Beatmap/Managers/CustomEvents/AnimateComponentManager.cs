using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base.Customs;
using SimpleJSON;
using UnityEngine;

/// <summary>Evaluates environment component animations against the current map time.</summary>
public sealed class AnimateComponentManager
{
    private const string BloomFogEnvironmentKey = "BloomFogEnvironment";
    private const string TubeBloomPrePassLightKey = "TubeBloomPrePassLight";
    private const string AttenuationKey = "attenuation";
    private const string OffsetKey = "offset";
    private const string HeightKey = "height";
    private const string StartYKey = "startY";
    private const string LegacyAttenuationKey = "_attenuation";
    private const string LegacyOffsetKey = "_offset";
    private const string LegacyHeightKey = "_height";
    private const string LegacyStartYKey = "_startY";
    private const string ColorAlphaMultiplierKey = "colorAlphaMultiplier";
    private const string BloomFogIntensityMultiplierKey = "bloomFogIntensityMultiplier";

    public TracksManager TracksManager;
    public BeatmapRuntimeContext Context;

    private readonly Dictionary<string, Dictionary<string, ComponentAnimationProperty>> componentFogProperties = new();
    private readonly Dictionary<string, Dictionary<string, ComponentAnimationProperty>> legacyFogProperties = new();
    private readonly Dictionary<string, Dictionary<string, ComponentAnimationProperty>> tubeProperties = new();
    private readonly List<string> componentFogTrackOrder = new();
    private readonly List<string> tubeTrackOrder = new();
    private readonly List<FogAssignment> fogAssignments = new();

    private readonly Dictionary<string, ParametricBloomFogLightController[]> tubeTargets = new();
    private readonly Dictionary<ParametricBloomFogLightController, Dictionary<string, float>> tubeBaselines = new();
    private readonly HashSet<ParametricBloomFogLightController> refreshedControllers = new();
    private readonly Dictionary<ParametricBloomFogLightController, TubePropertyOwnership> tubePropertyOwnership = new();
    private readonly List<ParametricBloomFogLightController> releasedTubeTargets = new();
    private readonly HashSet<string> fogBaselineKeys = new();
    private readonly HashSet<string> ownedFogKeys = new();

    private BloomFogParams fogBaselineTarget;
    private FogBaseline fogBaseline;
    private int cachedComponentRevision = -1;
    private int cachedFogComponentRevision = int.MinValue;

    /// <summary>
    /// Rebuilds the component animation channels from the complete event list. V2 fog-track assignments
    /// are sampled from the same JSON beat clock as the V3 component and tube-light channels.
    /// </summary>
    public void Rebuild(IEnumerable<BaseCustomEvent> events)
    {
        Reset();

        var isV2 = Settings.Instance.MapVersion == 2;
        var assignmentOrder = 0;
        if (events != null)
        {
            foreach (var evt in events)
            {
                if (evt == null) continue;

                if (isV2 && evt.Type == "AssignFogTrack" && evt.CustomTrack is JSONString assignmentTrack)
                {
                    fogAssignments.Add(new FogAssignment(evt.JsonTime, assignmentTrack.Value, assignmentOrder++));
                    continue;
                }

                foreach (var track in TrackNames(evt.CustomTrack))
                {
                    if (evt.Type == "AnimateComponent")
                    {
                        AddComponentFogEvent(track, evt);
                        AddTubeEvent(track, evt);
                    }
                    else if (isV2 && evt.Type == "AnimateTrack")
                    {
                        AddLegacyFogEvent(track, evt);
                    }
                }
            }
        }

        SortProperties(componentFogProperties);
        SortProperties(legacyFogProperties);
        SortProperties(tubeProperties);
        fogAssignments.Sort(FogAssignment.Compare);
        cachedComponentRevision = -1;
    }

    /// <summary>
    /// Replaces channels on the affected tracks from the complete, ordered event list, retaining all other
    /// channel instances and authored target baselines. The caller includes tracks affected by named-curve edits.
    /// </summary>
    public void RefreshEvents(IReadOnlyList<BaseCustomEvent> orderedEvents, ISet<string> affectedTracks,
        bool refreshFogAssignments)
    {
        foreach (var track in affectedTracks)
        {
            componentFogProperties.Remove(track);
            legacyFogProperties.Remove(track);
            tubeProperties.Remove(track);
        }
        componentFogTrackOrder.Clear();
        tubeTrackOrder.Clear();
        if (refreshFogAssignments) fogAssignments.Clear();

        var isV2 = Settings.Instance.MapVersion == 2;
        var assignmentOrder = 0;
        foreach (var evt in orderedEvents)
        {
            if (evt == null) continue;
            if (isV2 && evt.Type == "AssignFogTrack" && evt.CustomTrack is JSONString assignmentTrack)
            {
                if (refreshFogAssignments)
                    fogAssignments.Add(new FogAssignment(evt.JsonTime, assignmentTrack.Value, assignmentOrder++));
                continue;
            }

            foreach (var track in TrackNames(evt.CustomTrack))
            {
                if (evt.Type == "AnimateComponent")
                {
                    if (evt.Data?[BloomFogEnvironmentKey] is JSONObject) AddTrackOnce(componentFogTrackOrder, track);
                    if (evt.Data?[TubeBloomPrePassLightKey] is JSONObject) AddTrackOnce(tubeTrackOrder, track);
                    if (!affectedTracks.Contains(track)) continue;
                    AddComponentFogEvent(track, evt);
                    AddTubeEvent(track, evt);
                }
                else if (isV2 && evt.Type == "AnimateTrack" && affectedTracks.Contains(track))
                    AddLegacyFogEvent(track, evt);
            }
        }

        SortProperties(componentFogProperties, affectedTracks);
        SortProperties(legacyFogProperties, affectedTracks);
        SortProperties(tubeProperties, affectedTracks);
        if (refreshFogAssignments) fogAssignments.Sort(FogAssignment.Compare);

        cachedFogComponentRevision = int.MinValue;
        RefreshTubeTargetsIfNeeded(affectedTracks);
        var parameters = Context != null && Context.Descriptor != null ? Context.Descriptor.BloomFogParams : null;
        if (parameters == null) return;
        var before = new FogBaseline(parameters.Attenuation, parameters.Offset, parameters.Height, parameters.StartY);
        var releasedFog = RefreshFogBaselinesIfNeeded(parameters);
        if (releasedFog || ownedFogKeys.Count > 0) PublishFogParams(parameters, before);
    }

    /// <summary>Samples all registered component channels at a JSON beat time.</summary>
    public void Sample(float jsonTime)
    {
        RefreshTubeTargetsIfNeeded();
        var parameters = Context != null && Context.Descriptor != null ? Context.Descriptor.BloomFogParams : null;
        var fogBefore = parameters == null ? default : new FogBaseline(
            parameters.Attenuation, parameters.Offset, parameters.Height, parameters.StartY);
        var updatedFog = SampleFog(jsonTime);
        SampleTubeLights(jsonTime);

        if (updatedFog && parameters != null) PublishFogParams(parameters, fogBefore);
    }

    /// <summary>Restores captured authored values and releases all event and target references.</summary>
    public void Reset()
    {
        RestoreFogBaseline();
        RestoreAllTubeBaselines();

        componentFogProperties.Clear();
        legacyFogProperties.Clear();
        tubeProperties.Clear();
        componentFogTrackOrder.Clear();
        tubeTrackOrder.Clear();
        fogAssignments.Clear();
        tubeTargets.Clear();
        tubePropertyOwnership.Clear();
        releasedTubeTargets.Clear();
        tubeBaselines.Clear();
        refreshedControllers.Clear();
        fogBaselineTarget = null;
        fogBaseline = default;
        fogBaselineKeys.Clear();
        ownedFogKeys.Clear();
        cachedComponentRevision = -1;
        cachedFogComponentRevision = int.MinValue;
    }

    private void AddComponentFogEvent(string track, BaseCustomEvent evt)
    {
        if (evt.Data?[BloomFogEnvironmentKey] is not JSONObject component) return;
        AddTrackOnce(componentFogTrackOrder, track);

        foreach (var pair in component)
        {
            if (!IsFogProperty(pair.Key)) continue;
            AddFloatProperty(componentFogProperties, track, pair.Key, pair.Value, evt,
                value => WriteFogParam(pair.Key, value));
        }
    }

    private void AddLegacyFogEvent(string track, BaseCustomEvent evt)
    {
        foreach (var pair in evt.Data)
        {
            var key = pair.Key switch
            {
                LegacyAttenuationKey => AttenuationKey,
                LegacyOffsetKey => OffsetKey,
                LegacyHeightKey => HeightKey,
                LegacyStartYKey => StartYKey,
                _ => null
            };
            if (key == null) continue;

            AddFloatProperty(legacyFogProperties, track, key, pair.Value, evt,
                value => WriteFogParam(key, value));
        }
    }

    private void AddTubeEvent(string track, BaseCustomEvent evt)
    {
        if (evt.Data?[TubeBloomPrePassLightKey] is not JSONObject component) return;
        AddTrackOnce(tubeTrackOrder, track);

        foreach (var pair in component)
        {
            if (pair.Key != ColorAlphaMultiplierKey && pair.Key != BloomFogIntensityMultiplierKey) continue;
            AddFloatProperty(tubeProperties, track, pair.Key, pair.Value, evt,
                value => WriteTubeValue(track, pair.Key, value));
        }
    }

    private void AddFloatProperty(
        Dictionary<string, Dictionary<string, ComponentAnimationProperty>> channels,
        string track,
        string key,
        JSONNode points,
        BaseCustomEvent evt,
        Action<float> setter)
    {
        var parameters = new IPointDefinition.UntypedParams
        {
            Key = key,
            Points = points,
            Easing = evt.DataEasing,
            Time = evt.JsonTime,
            Duration = evt.DataDuration ?? 0,
            TimeBegin = evt.JsonTime,
            TimeEnd = evt.JsonTime + (evt.DataDuration ?? 0),
            Repeat = evt.DataRepeat ?? 0
        };

        var stops = points == null || points.IsNull;
        if (!stops && AnimateProperty<float>.SkipsMissingPointDefinition(parameters, evt)) return;

        if (!channels.TryGetValue(track, out var properties))
            channels.Add(track, properties = new Dictionary<string, ComponentAnimationProperty>());
        if (!properties.TryGetValue(key, out var property))
            properties.Add(key, property = new ComponentAnimationProperty(setter, 0f));

        if (stops) property.StopAt(evt);
        else property.AddPointDef(PointDataParsers.ParseFloat, parameters, evt);
        if (property.IsEmpty()) RemoveProperty(channels, track, key);
    }

    private bool SampleFog(float jsonTime)
    {
        var parameters = Context != null && Context.Descriptor != null ? Context.Descriptor.BloomFogParams : null;
        if (parameters == null) return false;
        var releasedFog = RefreshFogBaselinesIfNeeded(parameters);

        if (Settings.Instance.MapVersion == 2 && fogAssignments.Count > 0)
        {
            WriteFogBaseline();

            var assignmentIndex = GetFogAssignmentIndex(jsonTime);
            if (assignmentIndex >= 0)
                SampleFogTrack(legacyFogProperties, fogAssignments[assignmentIndex].Track, jsonTime);

            return true;
        }

        if (ownedFogKeys.Count == 0) return releasedFog;

        // Each track-owned property is sampled even before its first event so the evaluator restores its
        // authored default on backward seeks. Multiple owners are applied in event-list order.
        foreach (var track in componentFogTrackOrder)
        {
            if (TracksManager == null || !TracksManager.FogComponentTracks.Contains(track)) continue;
            SampleFogTrack(componentFogProperties, track, jsonTime);
        }

        return true;
    }

    private void SampleFogTrack(
        Dictionary<string, Dictionary<string, ComponentAnimationProperty>> channels,
        string track,
        float jsonTime)
    {
        if (!channels.TryGetValue(track, out var properties)) return;
        foreach (var pair in properties)
        {
            pair.Value.Default = ReadFogBaseline(pair.Key);
            pair.Value.UpdateProperty(jsonTime, true);
        }
    }

    private void SampleTubeLights(float jsonTime)
    {
        foreach (var track in tubeTrackOrder)
        {
            if (!tubeProperties.TryGetValue(track, out var properties)
                || !tubeTargets.TryGetValue(track, out var controllers)
                || controllers.Length == 0) continue;

            foreach (var pair in properties)
            {
                if (jsonTime >= pair.Value.StartTime)
                    pair.Value.UpdateProperty(jsonTime, true);
                else
                    RestoreTubeBaseline(track, pair.Key);
            }
        }

        RefreshTubeControllers();
    }

    private void RefreshTubeTargetsIfNeeded(ISet<string> affectedTracks = null)
    {
        var revision = TracksManager != null ? TracksManager.ComponentRevision : -1;
        if (cachedComponentRevision == revision && affectedTracks == null) return;

        var refreshAll = cachedComponentRevision != revision;
        if (refreshAll) tubeTargets.Clear();
        else
            foreach (var track in affectedTracks) tubeTargets.Remove(track);
        foreach (var track in tubeTrackOrder)
        {
            if (refreshAll || affectedTracks.Contains(track)) tubeTargets[track] = GetCurrentTubeTargets(track);
        }

        ReconcileTubeBaselines();
        // Capture every target baseline before applying any track. A light can be assigned to more than
        // one track, so capturing during sampling could save another track's animated value.
        foreach (var track in tubeTrackOrder)
        {
            if (!tubeProperties.TryGetValue(track, out var properties)
                || !tubeTargets.TryGetValue(track, out var controllers)) continue;
            EnsureTubeBaselines(controllers, properties);
            foreach (var pair in properties) SetTubeDefault(pair.Value, pair.Key, controllers);
        }

        cachedComponentRevision = revision;
        RefreshTubeControllers();
    }

    private void ReconcileTubeBaselines()
    {
        tubePropertyOwnership.Clear();
        foreach (var track in tubeTrackOrder)
        {
            if (!tubeProperties.TryGetValue(track, out var properties)
                || !tubeTargets.TryGetValue(track, out var controllers)) continue;
            var ownership = TubePropertyOwnership.None;
            if (properties.ContainsKey(ColorAlphaMultiplierKey)) ownership |= TubePropertyOwnership.Alpha;
            if (properties.ContainsKey(BloomFogIntensityMultiplierKey)) ownership |= TubePropertyOwnership.Intensity;
            foreach (var controller in controllers)
            {
                if (controller == null) continue;
                tubePropertyOwnership.TryGetValue(controller, out var existing);
                tubePropertyOwnership[controller] = existing | ownership;
            }
        }

        releasedTubeTargets.Clear();
        foreach (var (controller, baselines) in tubeBaselines)
        {
            if (controller == null)
            {
                releasedTubeTargets.Add(controller);
                continue;
            }
            tubePropertyOwnership.TryGetValue(controller, out var ownership);
            RestoreReleasedTubeProperty(controller, baselines, ColorAlphaMultiplierKey,
                (ownership & TubePropertyOwnership.Alpha) != 0);
            RestoreReleasedTubeProperty(controller, baselines, BloomFogIntensityMultiplierKey,
                (ownership & TubePropertyOwnership.Intensity) != 0);
            if (baselines.Count > 0) continue;
            RefreshTubeController(controller);
            releasedTubeTargets.Add(controller);
        }
        foreach (var controller in releasedTubeTargets) tubeBaselines.Remove(controller);
    }

    private static void RestoreReleasedTubeProperty(ParametricBloomFogLightController controller,
        Dictionary<string, float> baselines, string key, bool owned)
    {
        if (owned || !baselines.TryGetValue(key, out var baseline)) return;
        WriteTubeValue(controller, key, baseline);
        baselines.Remove(key);
    }

    private ParametricBloomFogLightController[] GetCurrentTubeTargets(string track)
    {
        if (TracksManager != null && TracksManager.TrackToBloomFogLights.TryGetValue(track, out var lights))
            return lights.Where(light => light != null).Distinct().ToArray();
        return Array.Empty<ParametricBloomFogLightController>();
    }

    private void EnsureTubeBaselines(
        ParametricBloomFogLightController[] controllers,
        Dictionary<string, ComponentAnimationProperty> properties)
    {
        foreach (var controller in controllers)
        {
            if (controller == null) continue;
            if (!tubeBaselines.TryGetValue(controller, out var baselines))
                tubeBaselines.Add(controller, baselines = new Dictionary<string, float>());
            foreach (var key in properties.Keys)
                if (!baselines.ContainsKey(key)) baselines.Add(key, ReadTubeValue(controller, key));
        }
    }

    private void SetTubeDefault(ComponentAnimationProperty property, string key,
        ParametricBloomFogLightController[] controllers)
    {
        if (controllers.Length > 0 && controllers[0] != null
            && tubeBaselines.TryGetValue(controllers[0], out var baselines)
            && baselines.TryGetValue(key, out var firstBaseline)) property.Default = firstBaseline;
    }

    private void RestoreTubeBaseline(string track, string key)
    {
        if (!tubeTargets.TryGetValue(track, out var controllers)) return;
        foreach (var controller in controllers)
            if (controller != null && tubeBaselines.TryGetValue(controller, out var baselines)
                && baselines.TryGetValue(key, out var baseline)) WriteTubeValue(controller, key, baseline);
    }

    private void RestoreAllTubeBaselines()
    {
        foreach (var baseline in tubeBaselines)
        {
            if (baseline.Key == null) continue;
            foreach (var pair in baseline.Value) WriteTubeValue(baseline.Key, pair.Key, pair.Value);
            RefreshTubeController(baseline.Key);
        }
    }

    private void WriteTubeValue(string track, string key, float value)
    {
        if (!tubeTargets.TryGetValue(track, out var controllers)) return;
        foreach (var controller in controllers)
            if (controller != null) WriteTubeValue(controller, key, value);
    }

    private static float ReadTubeValue(ParametricBloomFogLightController controller, string key) => key switch
    {
        ColorAlphaMultiplierKey => controller.ColorAlphaMultiplier,
        BloomFogIntensityMultiplierKey => controller.BloomFogIntensityMultiplier,
        _ => 0f
    };

    private static void WriteTubeValue(ParametricBloomFogLightController controller, string key, float value)
    {
        switch (key)
        {
            case ColorAlphaMultiplierKey:
                if (controller.ColorAlphaMultiplier != value) controller.ColorAlphaMultiplier = value;
                break;
            case BloomFogIntensityMultiplierKey:
                if (controller.BloomFogIntensityMultiplier != value) controller.BloomFogIntensityMultiplier = value;
                break;
        }
    }

    private void RefreshTubeControllers()
    {
        refreshedControllers.Clear();
        foreach (var targets in tubeTargets.Values)
        foreach (var controller in targets)
        {
            if (controller == null || !refreshedControllers.Add(controller)) continue;
            RefreshTubeController(controller);
        }
    }

    private static void RefreshTubeController(ParametricBloomFogLightController controller)
    {
        if (controller.HasInitialized && !controller.UpdateAlways && controller.ShouldRefresh)
            controller.Refresh();
    }

    private bool RefreshFogBaselinesIfNeeded(BloomFogParams parameters)
    {
        var revision = TracksManager != null ? TracksManager.ComponentRevision : -1;
        if (ReferenceEquals(fogBaselineTarget, parameters) && cachedFogComponentRevision == revision) return false;
        if (!ReferenceEquals(fogBaselineTarget, parameters))
        {
            RestoreFogBaseline();
            fogBaselineTarget = parameters;
            fogBaseline = default;
            fogBaselineKeys.Clear();
        }

        ownedFogKeys.Clear();
        if (Settings.Instance.MapVersion == 2 && fogAssignments.Count > 0)
        {
            ownedFogKeys.Add(AttenuationKey);
            ownedFogKeys.Add(OffsetKey);
            ownedFogKeys.Add(HeightKey);
            ownedFogKeys.Add(StartYKey);
        }
        else if (TracksManager != null)
        {
            foreach (var track in componentFogTrackOrder)
            {
                if (!TracksManager.FogComponentTracks.Contains(track)
                    || !componentFogProperties.TryGetValue(track, out var properties)) continue;
                foreach (var key in properties.Keys) ownedFogKeys.Add(key);
            }
        }

        var released = RestoreReleasedFogProperty(AttenuationKey);
        released |= RestoreReleasedFogProperty(OffsetKey);
        released |= RestoreReleasedFogProperty(HeightKey);
        released |= RestoreReleasedFogProperty(StartYKey);
        foreach (var key in ownedFogKeys)
        {
            if (!fogBaselineKeys.Add(key)) continue;
            fogBaseline = fogBaseline.WithValue(key, ReadFogValue(parameters, key));
        }
        SetFogDefaults(componentFogProperties);
        SetFogDefaults(legacyFogProperties);
        cachedFogComponentRevision = revision;
        return released;
    }

    private bool RestoreReleasedFogProperty(string key)
    {
        if (ownedFogKeys.Contains(key) || !fogBaselineKeys.Remove(key)) return false;
        WriteFogParam(key, ReadFogBaseline(key));
        return true;
    }

    private void SetFogDefaults(Dictionary<string, Dictionary<string, ComponentAnimationProperty>> channels)
    {
        foreach (var properties in channels.Values)
        foreach (var pair in properties)
            pair.Value.Default = ReadFogBaseline(pair.Key);
    }

    private void RestoreFogBaseline()
    {
        if (fogBaselineTarget == null || fogBaselineKeys.Count == 0) return;
        var before = new FogBaseline(fogBaselineTarget.Attenuation, fogBaselineTarget.Offset,
            fogBaselineTarget.Height, fogBaselineTarget.StartY);
        foreach (var key in fogBaselineKeys) WriteFogValue(fogBaselineTarget, key, ReadFogBaseline(key));
        PublishFogParams(fogBaselineTarget, before);
    }

    private void PublishFogParams(BloomFogParams parameters, FogBaseline before)
    {
        if (Context == null || Context.Descriptor == null
            || !ReferenceEquals(Context.Descriptor.BloomFogParams, parameters)) return;

        if (!before.Matches(parameters))
        {
            Context.NotifyBloomFogParamsChanged();
            return;
        }

        var bloomfogRenderer = BloomfogRenderingController.Instance;
        if (bloomfogRenderer != null)
            bloomfogRenderer.ReconcileBloomFogParams(Context, parameters);
    }

    private void WriteFogBaseline()
    {
        if (fogBaselineTarget == null) return;
        fogBaseline.WriteTo(fogBaselineTarget);
    }

    private float ReadFogBaseline(string key) => key switch
    {
        AttenuationKey => fogBaseline.Attenuation,
        OffsetKey => fogBaseline.Offset,
        HeightKey => fogBaseline.Height,
        StartYKey => fogBaseline.StartY,
        _ => 0f
    };

    private void WriteFogParam(string key, float value)
    {
        var parameters = Context != null && Context.Descriptor != null ? Context.Descriptor.BloomFogParams : null;
        if (parameters == null) return;
        WriteFogValue(parameters, key, value);
    }

    private static float ReadFogValue(BloomFogParams parameters, string key) => key switch
    {
        AttenuationKey => parameters.Attenuation,
        OffsetKey => parameters.Offset,
        HeightKey => parameters.Height,
        StartYKey => parameters.StartY,
        _ => 0f
    };

    private static void WriteFogValue(BloomFogParams parameters, string key, float value)
    {
        switch (key)
        {
            case AttenuationKey:
                if (!parameters.Attenuation.Equals(value)) parameters.Attenuation = value;
                break;
            case OffsetKey:
                if (!parameters.Offset.Equals(value)) parameters.Offset = value;
                break;
            case HeightKey:
                if (!parameters.Height.Equals(value)) parameters.Height = value;
                break;
            case StartYKey:
                if (!parameters.StartY.Equals(value)) parameters.StartY = value;
                break;
        }
    }

    private int GetFogAssignmentIndex(float time)
    {
        var left = 0;
        var right = fogAssignments.Count;
        while (left < right)
        {
            var middle = left + (right - left) / 2;
            if (fogAssignments[middle].Time <= time) left = middle + 1;
            else right = middle;
        }
        return left - 1;
    }

    private static IEnumerable<string> TrackNames(JSONNode node)
    {
        if (node is JSONString name)
            yield return name.Value;
        else if (node is JSONArray names)
            foreach (var child in names.Children)
                if (child is JSONString item) yield return item.Value;
    }

    private static bool IsFogProperty(string key) =>
        key == AttenuationKey || key == OffsetKey || key == HeightKey || key == StartYKey;

    private static void AddTrackOnce(List<string> tracks, string track)
    {
        if (!tracks.Contains(track)) tracks.Add(track);
    }

    private static void RemoveProperty(
        Dictionary<string, Dictionary<string, ComponentAnimationProperty>> channels,
        string track,
        string key)
    {
        if (!channels.TryGetValue(track, out var properties)) return;
        properties.Remove(key);
        if (properties.Count == 0) channels.Remove(track);
    }

    private static void SortProperties(
        Dictionary<string, Dictionary<string, ComponentAnimationProperty>> channels, ISet<string> affectedTracks = null)
    {
        foreach (var (track, properties) in channels.ToArray())
        {
            if (affectedTracks != null && !affectedTracks.Contains(track)) continue;
            foreach (var (key, property) in properties.ToArray())
            {
                if (property.IsEmpty()) properties.Remove(key);
                else property.Sort();
            }
            if (properties.Count == 0) channels.Remove(track);
        }
    }

    [Flags]
    private enum TubePropertyOwnership
    {
        None = 0,
        Alpha = 1,
        Intensity = 2
    }

    private readonly struct FogAssignment
    {
        public readonly float Time;
        public readonly string Track;
        private readonly int order;

        public FogAssignment(float time, string track, int order)
        {
            Time = time;
            Track = track;
            this.order = order;
        }

        public static int Compare(FogAssignment left, FogAssignment right)
        {
            var timeComparison = left.Time.CompareTo(right.Time);
            return timeComparison != 0 ? timeComparison : left.order.CompareTo(right.order);
        }
    }

    private readonly struct FogBaseline
    {
        public readonly float Attenuation;
        public readonly float Offset;
        public readonly float Height;
        public readonly float StartY;

        public FogBaseline(float attenuation, float offset, float height, float startY)
        {
            Attenuation = attenuation;
            Offset = offset;
            Height = height;
            StartY = startY;
        }

        public void WriteTo(BloomFogParams parameters)
        {
            parameters.Attenuation = Attenuation;
            parameters.Offset = Offset;
            parameters.Height = Height;
            parameters.StartY = StartY;
        }

        public FogBaseline WithValue(string key, float value) => key switch
        {
            AttenuationKey => new FogBaseline(value, Offset, Height, StartY),
            OffsetKey => new FogBaseline(Attenuation, value, Height, StartY),
            HeightKey => new FogBaseline(Attenuation, Offset, value, StartY),
            StartYKey => new FogBaseline(Attenuation, Offset, Height, value),
            _ => this
        };

        public bool Matches(BloomFogParams parameters) =>
            Attenuation.Equals(parameters.Attenuation)
            && Offset.Equals(parameters.Offset)
            && Height.Equals(parameters.Height)
            && StartY.Equals(parameters.StartY);
    }
}
