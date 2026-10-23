using System;
using System.Text;
using System.Collections.Generic;
using Beatmap.Animations;
using SimpleJSON;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using UnityEngine;

/// <summary>Owns custom-event loading, editor invalidation, and cursor evaluation.</summary>
[DefaultExecutionOrder(-2000)]
public class CustomEventManager : BeatmapObjectManager<BaseCustomEvent>
{
    [SerializeField] private CustomEventStateManager manager;
    private TrackCustomEventManager trackEvents;
    private BaseDifficulty loadedMap;
    private bool ready;
    private bool dirty;
    private bool timingDirty;
    private int loadedVersion;
    private bool loadedOthers;
    private readonly Dictionary<BaseCustomEvent, EventSnapshot> eventSnapshots = new();
    private readonly Dictionary<string, string> definitionContents = new();
    private readonly HashSet<BaseCustomEvent> pendingEvents = new();
    private float previousTime = float.NegativeInfinity;

    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnLevelLoaded += UpdateTime;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += UpdateTime;
    }

    private void Start()
    {
        UIMode.OnPreviewModeSwitched += UpdateTime;
        Context.Atsc.OnPlayToggled += HandlePlayToggled;
        BPMChangeGridContainer.OnBPMChangeRefreshed += RefreshTiming;
        Context.OnEnvironmentUnloaded += SuspendForMapChange;
    }

    private void RefreshTiming()
    {
        timingDirty = true;
        dirty = true;
        if (ready) UpdateTime();
    }

    protected override void OnDestroy()
    {
        MapGraphicsSettings.Reset();
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= UpdateTime;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= UpdateTime;
        UIMode.OnPreviewModeSwitched -= UpdateTime;
        BPMChangeGridContainer.OnBPMChangeRefreshed -= RefreshTiming;
        if (Context != null) Context.OnEnvironmentUnloaded -= SuspendForMapChange;
        if (Context != null && Context.Atsc != null) Context.Atsc.OnTimeChangedEarly -= UpdateTime;
        if (Context != null && Context.Atsc != null) Context.Atsc.OnPlayToggled -= HandlePlayToggled;
        if (manager != null) manager.ResetRuntime();
        if (manager != null) manager.ResetComponentEvents();
        trackEvents?.Reset();
        PointDefinitionParser.ResetCache();
    }

    /// <summary>Restores the outgoing map before its environment or assets can be unloaded.</summary>
    public void SuspendForMapChange()
    {
        ready = false;
        MapGraphicsSettings.Reset();
        manager.ResetComponentEvents();
        manager.ResetRuntime();
        trackEvents?.Reset();
        manager.TracksManager.ClearComponentTargets();
    }

    /// <summary>Builds runtime indexes before note/environment containers attach to their tracks.</summary>
    public void LoadEvents(BaseDifficulty map, IEnumerable<BaseCustomEvent> events)
    {
        ready = false;
        manager.ResetComponentEvents();
        manager.ResetRuntime();
        MapGraphicsSettings.Apply(BeatSaberSongContainer.Instance.MapDifficultyInfo);
        if (trackEvents == null)
        {
            var grid = BeatmapObjectContainerCollection.GetCollectionForType<CustomEventGridContainer>(
                Beatmap.Enums.ObjectType.CustomEvent);
            trackEvents = new TrackCustomEventManager(manager.TracksManager, Context, grid.PlayerCamera);
        }
        if (!ReferenceEquals(loadedMap, map))
        {
            manager.TracksManager.FlushModelUpdates();
            manager.VivifyAssetBundleManager.UnloadAssetBundle();
            manager.VivifyAssetBundleManager.LoadAssetBundle();
            loadedMap = map;
        }

        PointDefinitionParser.ResetCache(preserveInvalidDefinitions: true);
        BaseCustomEvent[] ordered;
        PointDefinitionParser.BeginRegistration();
        try
        {
            foreach (var (name, definition) in map.PointDefinitions) PointDefinitionParser.Get(name, definition);

            // Preserve the existing type ordering at equal beats; stable enumeration resolves equal-type ties.
            ordered = OrderEvents(events);
            manager.Context = Context;
            manager.TrackEvents = trackEvents;
            manager.Initialize();
            trackEvents.Rebuild(ordered);
            manager.RebuildComponentEvents(ordered);
            foreach (var evt in ordered) manager.InsertData(evt);
            // Refresh each target once after all track definitions are ready, including inline animations.
            foreach (var animator in FindObjectsByType<ObjectAnimator>(FindObjectsSortMode.None))
            {
                if (animator.Context != Context || !animator.enabled) continue;
                animator.RefreshTrackAnimation();
                if (animator.enabled) animator.ApplyCustomEventSample(false);
            }
        }
        finally
        {
            PointDefinitionParser.EndRegistration();
        }
        PointDefinitionParser.RetainDefinitions(map.PointDefinitions, AnimationRoots(map, ordered));
        CaptureSources(map, ordered);
        loadedVersion = map.MajorVersion;
        loadedOthers = Settings.Instance.Load_Others;
        previousTime = float.NegativeInfinity;
        dirty = false;
        timingDirty = false;
        pendingEvents.Clear();
        ready = true;
        Context.Atsc.OnTimeChangedEarly -= UpdateTime;
        Context.Atsc.OnTimeChangedEarly += UpdateTime;
    }

    public override void Refresh()
    {
        var map = BeatSaberSongContainer.Instance.Map;
        RefreshEvents(map, true);
        UpdateTime();
    }

    private void CaptureSources(BaseDifficulty map, BaseCustomEvent[] ordered)
    {
        eventSnapshots.Clear();
        for (var i = 0; i < ordered.Length; i++) eventSnapshots.Add(ordered[i], new EventSnapshot(ordered[i], i));
        definitionContents.Clear();
        foreach (var pair in map.PointDefinitions) definitionContents.Add(pair.Key, pair.Value?.ToString());
    }

    private static BaseCustomEvent[] OrderEvents(IEnumerable<BaseCustomEvent> events) => events
        .OrderBy(evt => evt.JsonTime).ThenBy(evt => evt.Type, StringComparer.Ordinal).ToArray();

    private void RefreshEvents(BaseDifficulty map, bool inspectAll)
    {
        var events = Settings.Instance.Load_Others ? map.CustomEvents : Enumerable.Empty<BaseCustomEvent>();
        if (!ready || timingDirty || !ReferenceEquals(map, loadedMap)
            || loadedVersion != map.MajorVersion || loadedOthers != Settings.Instance.Load_Others)
        {
            LoadEvents(map, events);
            return;
        }

        var ordered = OrderEvents(events);
        var current = new Dictionary<BaseCustomEvent, EventSnapshot>();
        var changed = new HashSet<BaseCustomEvent>();
        for (var i = 0; i < ordered.Length; i++)
        {
            var evt = ordered[i];
            if (!eventSnapshots.TryGetValue(evt, out var snapshot))
            {
                snapshot = new EventSnapshot(evt, i);
                changed.Add(evt);
            }
            else if ((inspectAll || pendingEvents.Contains(evt)) && !snapshot.Matches(evt))
            {
                snapshot = new EventSnapshot(evt, i);
                changed.Add(evt);
            }
            current.Add(evt, snapshot);
        }
        foreach (var evt in eventSnapshots.Keys)
            if (!current.ContainsKey(evt)) changed.Add(evt);

        // Inserting a tie does not invalidate other tracks. Reordering existing ties can change
        // last-writer precedence, so only that equal-beat/type group needs fresh bindings.
        for (var first = 0; first < ordered.Length;)
        {
            var end = first + 1;
            while (end < ordered.Length && ordered[end].JsonTime == ordered[first].JsonTime
                && ordered[end].Type == ordered[first].Type) end++;
            var previousIndex = -1;
            var reordered = false;
            for (var i = first; i < end; i++)
            {
                var evt = ordered[i];
                if (changed.Contains(evt) || !eventSnapshots.TryGetValue(evt, out var old)) continue;
                reordered |= old.Order < previousIndex;
                previousIndex = old.Order;
            }
            if (reordered)
                for (var i = first; i < end; i++) changed.Add(ordered[i]);
            first = end;
        }

        var changedNames = new HashSet<string>();
        var currentDefinitions = new Dictionary<string, string>();
        foreach (var pair in map.PointDefinitions)
        {
            var content = pair.Value?.ToString();
            currentDefinitions.Add(pair.Key, content);
            if (!definitionContents.TryGetValue(pair.Key, out var previous) || previous != content)
                changedNames.Add(pair.Key);
        }
        foreach (var name in definitionContents.Keys)
            if (!currentDefinitions.ContainsKey(name)) changedNames.Add(name);
        if (changedNames.Count > 0)
            foreach (var evt in ordered)
                if (ReferencesDefinitions(evt.Data, changedNames)) changed.Add(evt);

        var affectedObjects = new HashSet<ObjectAnimator>();
        var pathObjects = new HashSet<ObjectAnimator>();
        if (inspectAll || changedNames.Count > 0)
        {
            foreach (var animator in FindObjectsByType<ObjectAnimator>(FindObjectsSortMode.None))
            {
                if (animator.Context != Context || !animator.enabled || animator.AnimationObject == null) continue;
                if ((inspectAll && animator.HasChangedObjectAnimation)
                    || ReferencesDefinitions(animator.AnimationObject.CustomAnimation, changedNames))
                {
                    affectedObjects.Add(animator);
                    pathObjects.Add(animator);
                }
            }
        }

        if (changed.Count > 0 || changedNames.Count > 0 || affectedObjects.Count > 0)
        {
            var tracks = new HashSet<string>();
            var poseTracks = new HashSet<string>();
            var paths = new HashSet<string>();
            var components = new HashSet<string>();
            var indexedTracks = new HashSet<string>();
            var playerChanged = false;
            var fogAssignmentChanged = false;
            var replay = false;
            foreach (var evt in changed)
            {
                eventSnapshots.TryGetValue(evt, out var old);
                current.TryGetValue(evt, out var next);
                if (old != null) Include(old);
                if (next != null) Include(next);
                if (EventSnapshot.PoseChanged(old, next, changedNames))
                {
                    if (old != null) poseTracks.UnionWith(old.Tracks);
                    if (next != null) poseTracks.UnionWith(next.Tracks);
                }
            }

            var runtimeChanged = changed.Count > 0 || affectedObjects.Count > 0;
            PointDefinitionParser.BeginRegistration();
            try
            {
                foreach (var name in changedNames)
                    if (map.PointDefinitions.TryGetValue(name, out var definition)) PointDefinitionParser.Get(name, definition);
                replay |= trackEvents.RefreshTracks(ordered, tracks, paths, affectedObjects, pathObjects, indexedTracks, poseTracks);
                if (playerChanged) trackEvents.RefreshPlayerTracks(ordered);
                if (components.Count > 0 || fogAssignmentChanged)
                    manager.RefreshComponentEvents(ordered, components, fogAssignmentChanged);
                if (runtimeChanged) manager.RefreshEvents(ordered, changed, replay);
                foreach (var animator in affectedObjects)
                {
                    if (animator == null || !animator.enabled) continue;
                    if (pathObjects.Contains(animator)) animator.RefreshTrackAnimation();
                    else animator.RefreshTrackContributions();
                }
                PointDefinitionParser.RetainDefinitions(map.PointDefinitions, AnimationRoots(map, ordered),
                    inspectAll, ChangedAnimationRoots());
            }
            finally
            {
                PointDefinitionParser.EndRegistration();
            }

            void Include(EventSnapshot snapshot)
            {
                foreach (var track in snapshot.Tracks)
                {
                    indexedTracks.Add(track);
                    if (snapshot.Type == "AnimateTrack") tracks.Add(track);
                    if (snapshot.Type == "AssignPathAnimation") paths.Add(track);
                    if (snapshot.Type == "AnimateComponent" || (map.MajorVersion == 2 && snapshot.Type == "AnimateTrack"))
                        components.Add(track);
                }
                playerChanged |= snapshot.Type == "AssignPlayerToTrack";
                fogAssignmentChanged |= snapshot.Type == "AssignFogTrack";
                replay |= snapshot.RequiresReplay && snapshot.SongTime <= Context.Atsc.CurrentSongBpmTime;
            }

            IEnumerable<JSONNode> ChangedAnimationRoots()
            {
                foreach (var evt in changed)
                    if (current.ContainsKey(evt)) yield return evt.Data;
                foreach (var name in changedNames)
                    if (map.PointDefinitions.TryGetValue(name, out var definition)) yield return definition;
            }
        }
        eventSnapshots.Clear();
        for (var i = 0; i < ordered.Length; i++)
        {
            current[ordered[i]].Order = i;
            eventSnapshots.Add(ordered[i], current[ordered[i]]);
        }
        definitionContents.Clear();
        foreach (var pair in currentDefinitions) definitionContents.Add(pair.Key, pair.Value);
        dirty = false;
        pendingEvents.Clear();
    }

    private static bool IsStructural(string type) => type is "AssignTrackParent" or "InstantiatePrefab"
        or "DestroyObject" or "Blit" or "CreateScreenTexture" or "CreateCamera" or "SetCameraProperty" or "AssignObjectPrefab";

    private static bool ReferencesDefinitions(JSONNode node, ISet<string> names)
    {
        if (node == null || names.Count == 0) return false;
        if (node is JSONString name) return names.Contains(name.Value);
        if (node is JSONObject obj)
        {
            foreach (var pair in obj)
            {
                if (pair.Key is "track" or "_track" or "parentTrack" or "_parentTrack" or "childrenTracks"
                    or "_childrenTracks" or "target" or "_target") continue;
                if (ReferencesDefinitions(pair.Value, names)) return true;
            }
        }
        else if (node is JSONArray array)
        {
            // Point rows contain base-provider/easing tokens; named curves are whole property values.
            foreach (var child in array.Children)
                if (child is JSONObject && ReferencesDefinitions(child, names)) return true;
        }
        return false;
    }

    private static IEnumerable<JSONNode> AnimationRoots(BaseDifficulty map, IEnumerable<BaseCustomEvent> events)
    {
        foreach (var evt in events) yield return evt.Data;
        foreach (var obj in map.Notes) yield return obj.CustomAnimation;
        foreach (var obj in map.Obstacles) yield return obj.CustomAnimation;
        foreach (var obj in map.Arcs) yield return obj.CustomAnimation;
        foreach (var obj in map.Chains) yield return obj.CustomAnimation;
    }

    private sealed class EventSnapshot
    {
        private static readonly string[] poseKeys = { "position", "localPosition", "offsetPosition", "rotation",
            "localRotation", "offsetWorldRotation", "scale", "_position", "_localPosition", "_rotation",
            "_localRotation", "_scale" };
        private readonly string poseContent;
        private readonly HashSet<string> poseDefinitions;
        internal readonly string Type;
        internal readonly float Time;
        internal readonly float SongTime;
        private readonly JSONNode content;
        internal readonly string[] Tracks;
        internal readonly bool RequiresReplay;
        private readonly JSONNode trackContent;
        private readonly JSONNode parentContent;
        private readonly JSONNode childrenContent;
        private readonly float? duration;
        private readonly string easing;
        private readonly int? repeat;
        private readonly bool? worldPositionStays;
        internal int Order;

        internal EventSnapshot(BaseCustomEvent evt, int order)
        {
            Type = evt.Type;
            // Trigger edits can change Animator transitions, so replay their state-machine effects.
            RequiresReplay = IsStructural(Type) || (Type == "SetAnimatorProperty"
                && evt.Data != null && evt.Data["properties"].Children.Any(property => property["type"] == "Trigger"));
            Time = evt.JsonTime;
            SongTime = evt.SongBpmTime;
            content = evt.Data?.Clone();
            trackContent = evt.CustomTrack?.Clone();
            parentContent = evt.DataParentTrack?.Clone();
            childrenContent = evt.DataChildrenTracks?.Clone();
            duration = evt.DataDuration;
            easing = evt.DataEasing;
            repeat = evt.DataRepeat;
            worldPositionStays = evt.DataWorldPositionStays;
            var pose = new StringBuilder();
            poseDefinitions = new HashSet<string>();
            if (Type == "AnimateTrack" && evt.Data != null)
                foreach (var key in poseKeys)
                {
                    if (!evt.Data.HasKey(key)) continue;
                    var value = evt.Data[key];
                    pose.Append(key).Append(':').Append(value?.ToString()).Append(';');
                    if (value is JSONString named) poseDefinitions.Add(named.Value);
                }
            poseContent = pose.ToString();
            Tracks = evt.CustomTrack is JSONString name ? new[] { name.Value }
                : evt.CustomTrack is JSONArray names ? names.Children.OfType<JSONString>().Select(item => item.Value).ToArray()
                : Array.Empty<string>();
            Order = order;
        }

        internal static bool PoseChanged(EventSnapshot before, EventSnapshot after, ISet<string> names)
        {
            if (before == null) return after.poseContent.Length > 0;
            if (after == null) return before.poseContent.Length > 0;
            if (before.poseContent.Length == 0 && after.poseContent.Length == 0) return false;
            return before.poseContent != after.poseContent || before.Time != after.Time
                || !MatchesJson(before.trackContent, after.trackContent) || before.duration != after.duration
                || before.easing != after.easing || before.repeat != after.repeat
                || before.poseDefinitions.Overlaps(names) || after.poseDefinitions.Overlaps(names);
        }

        internal bool Matches(BaseCustomEvent evt) => Type == evt.Type && Time == evt.JsonTime
            && SongTime == evt.SongBpmTime && MatchesJson(content, evt.Data)
            && duration == evt.DataDuration && easing == evt.DataEasing && repeat == evt.DataRepeat
            && worldPositionStays == evt.DataWorldPositionStays && MatchesJson(trackContent, evt.CustomTrack)
            && MatchesJson(parentContent, evt.DataParentTrack) && MatchesJson(childrenContent, evt.DataChildrenTracks);

        // Snapshots own their nodes so in-place edits remain visible without formatting every number
        // and allocating serialized JSON on every broad refresh. Array order still controls precedence.
        private static bool MatchesJson(JSONNode before, JSONNode after)
        {
            if (ReferenceEquals(before, after)) return true;
            if (ReferenceEquals(before, null) || ReferenceEquals(after, null)
                || before.Tag != after.Tag || before.Count != after.Count) return false;
            if (before is JSONArray)
            {
                for (var i = 0; i < before.Count; i++)
                    if (!MatchesJson(before[i], after[i])) return false;
                return true;
            }
            if (before is JSONObject)
            {
                foreach (var pair in before)
                    if (!after.HasKey(pair.Key) || !MatchesJson(pair.Value, after[pair.Key])) return false;
                return true;
            }
            return before.IsNumber ? before.AsDouble.Equals(after.AsDouble)
                : before.Value == after.Value;
        }
    }

    public override void UpdateTime() => UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);

    public override void UpdateTime(bool isPlaying, float beatTime)
    {
        if (!ready) return;
        if (dirty)
        {
            var map = BeatSaberSongContainer.Instance.Map;
            RefreshEvents(map, false);
        }
        var jsonTime = BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(beatTime) ?? beatTime;
        PointDefinitionParser.BeginSample(jsonTime, beatTime < previousTime);
        manager.UpdateTime(isPlaying, beatTime);
        previousTime = beatTime;
    }

    private void HandlePlayToggled(bool playing) => UpdateTime();

    // Editor actions have already updated the canonical map. Reconcile once after the whole action,
    // including collections and undo/redo, rather than replaying partially modified event lists.
    protected override bool AddData(IEnumerable<BaseCustomEvent> data) => MarkDirty(data);
    protected override bool RemoveData(IEnumerable<BaseCustomEvent> data) => MarkDirty(data);
    protected override bool RemoveData(IEnumerable<(BaseCustomEvent reference, BaseCustomEvent original)> data)
    {
        var changed = false;
        foreach (var pair in data)
        {
            pendingEvents.Add(pair.reference);
            changed = true;
        }
        dirty |= changed;
        return changed;
    }

    private bool MarkDirty(IEnumerable<BaseCustomEvent> data)
    {
        var changed = false;
        foreach (var evt in data)
        {
            pendingEvents.Add(evt);
            changed = true;
        }
        dirty |= changed;
        return changed;
    }
}
