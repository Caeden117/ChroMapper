using System.Collections.Generic;
using Beatmap.Base.Customs;
using UnityEngine;

/// <summary>Dispatches structural custom events and samples property state at the editor cursor.</summary>
public class CustomEventStateManager : StateManager<CustomEventStateData, BaseCustomEvent>
{
    public VisualRepositorySO VisualRepository;
    public VivifyAssetBundleManager VivifyAssetBundleManager;
    public TracksManager TracksManager;
    public BeatmapRuntimeContext Context;
    public TrackCustomEventManager TrackEvents;

    private readonly AnimateComponentManager animateComponentManager = new();
    private readonly InstantiateObjectPrefabManager instantiateObjectPrefabManager = new();
    public readonly AssignObjectPrefabManager AssignObjectPrefabManager = new();
    private readonly ObjectPropertyManager objectPropertyManager = new();
    private readonly CustomEventPropertyTimeline properties = new();
    private readonly RenderingSettingsManager renderingSettings = new();
    public VivifyRenderingManager Rendering { get; private set; }
    private PostProcessRenderingController postProcess;
    private readonly List<CustomEventStateData> states = new();
    private int nextEvent;
    private float previousTime = float.NegativeInfinity;
    private bool dirty;

    public override void Initialize()
    {
        ResetRuntime();
        var song = BeatSaberSongContainer.Instance;
        renderingSettings.ApplyDefaults(song != null && song.MapDifficultyInfo?.CustomRequirements.Contains("Vivify") == true);
        states.Clear();
        dirty = false;
        animateComponentManager.Context = Context;
        animateComponentManager.TracksManager = TracksManager;
        instantiateObjectPrefabManager.VivifyAssetBundleManager = VivifyAssetBundleManager;
        instantiateObjectPrefabManager.TracksManager = TracksManager;
        instantiateObjectPrefabManager.BeforeTargetReleased = ReleaseTarget;
        AssignObjectPrefabManager.VisualRepository = VisualRepository;
        AssignObjectPrefabManager.TracksManager = TracksManager;
        objectPropertyManager.InstantiateObjectPrefabManager = instantiateObjectPrefabManager;
        objectPropertyManager.VivifyAssetBundleManager = VivifyAssetBundleManager;
        objectPropertyManager.PropertyTimeline = properties;
        renderingSettings.PropertyTimeline = properties;
        renderingSettings.VivifyAssetBundleManager = VivifyAssetBundleManager;
        renderingSettings.InstantiateObjectPrefabManager = instantiateObjectPrefabManager;
        renderingSettings.SetSkyboxOverride = enabled =>
        {
            if (BloomfogRenderingController.Instance != null)
                BloomfogRenderingController.Instance.MapSkyboxOverride = enabled;
        };
        if (Rendering == null)
        {
            Rendering = new VivifyRenderingManager(TracksManager, VivifyAssetBundleManager, objectPropertyManager);
            postProcess = FindFirstObjectByType<PostProcessRenderingController>();
            if (postProcess != null) postProcess.AttachVivify(Rendering);
        }
    }

    private void ReleaseTarget(Object target)
    {
        objectPropertyManager.RemoveTarget(target);
        properties.RemoveTarget(target);
    }

    public void RebuildComponentEvents(IEnumerable<BaseCustomEvent> events) => animateComponentManager.Rebuild(events);

    public void ResetComponentEvents() => animateComponentManager.Reset();

    internal void RefreshComponentEvents(IReadOnlyList<BaseCustomEvent> events, ISet<string> tracks,
        bool refreshFogAssignments) => animateComponentManager.RefreshEvents(events, tracks, refreshFogAssignments);

    // Unchanged state descriptors remain valid; only structural edits at the cursor need ownership replay.
    internal void RefreshEvents(IReadOnlyList<BaseCustomEvent> events, ISet<BaseCustomEvent> changed,
        bool replayRuntime)
    {
        replayRuntime |= dirty;
        var retained = new Dictionary<BaseCustomEvent, CustomEventStateData>();
        foreach (var state in states)
        {
            retained[state.Base] = state;
            if (!replayRuntime && changed.Contains(state.Base)) objectPropertyManager.RemoveEvent(state);
        }
        states.Clear();
        var updatedProperties = new List<CustomEventStateData>();
        foreach (var evt in events)
        {
            if (changed.Contains(evt) || !retained.TryGetValue(evt, out var state))
            {
                state = BuildState(evt);
                if (IsPropertyEvent(evt.Type) && state.StartTime <= previousTime) updatedProperties.Add(state);
            }
            state.Sequence = states.Count;
            states.Add(state);
        }
        if (replayRuntime) dirty = true;
        else
        {
            foreach (var state in updatedProperties) FireEvent(state);
            nextEvent = 0;
            while (nextEvent < states.Count && states[nextEvent].StartTime <= previousTime) nextEvent++;
        }
    }

    private static bool IsPropertyEvent(string type) => type is "SetMaterialProperty" or "SetGlobalProperty"
        or "SetAnimatorProperty" or "SetRenderingSettings";

    /// <summary>Restores owned properties before releasing their targets and structural event state.</summary>
    /// <param name="preserveBlitTextures">Retain map defaults, playback captures, and reusable prefabs when rebuilding the current map's event state.</param>
    public void ResetRuntime(bool preserveBlitTextures = false)
    {
        objectPropertyManager.Reset();
        properties.Reset();
        renderingSettings.RestoreDefaults(preserveBlitTextures);
        HashSet<string> textureIds = null;
        HashSet<string> cameraIds = null;
        if (preserveBlitTextures)
        {
            textureIds = new HashSet<string>();
            cameraIds = new HashSet<string>();
            foreach (var state in states)
            {
                if (state.Base.Type == "CreateScreenTexture") textureIds.Add(state.Base.Data["id"]);
                else if (state.Base.Type == "CreateCamera") cameraIds.Add(state.Base.Data["id"]);
            }
        }
        Rendering?.Reset(preserveBlitTextures, textureIds, cameraIds);
        AssignObjectPrefabManager.Reset();
        if (preserveBlitTextures) instantiateObjectPrefabManager.BeginReplay();
        else instantiateObjectPrefabManager.Reset();
        TrackEvents?.ResetParenting();
        nextEvent = 0;
        previousTime = float.NegativeInfinity;
    }

    public override void Refresh() => dirty = true;

    private void SortStates() => states.Sort((left, right) =>
    {
        var comparison = left.StartTime.CompareTo(right.StartTime);
        return comparison != 0 ? comparison : left.Sequence.CompareTo(right.Sequence);
    });

    public override void UpdateTime(bool isPlaying, float time)
    {
        // Reconstruct structural ownership on rewind. Property values are sampled rather than
        // captured while crossing animations that have not yet been evaluated.
        if (dirty || time < previousTime)
        {
            ResetRuntime(true);
            SortStates();
            dirty = false;
        }

        Rendering?.Sample(time, isPlaying);
        while (nextEvent < states.Count && states[nextEvent].StartTime <= time)
            FireEvent(states[nextEvent++]);

        instantiateObjectPrefabManager.CompleteReplay();
        properties.Sample(time);
        TrackEvents?.Sample(BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(time) ?? time);
        animateComponentManager.Sample(BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(time) ?? time);
        previousTime = time;
    }

    private void FireEvent(CustomEventStateData state)
    {
        switch (state.Base.Type)
        {
            case "AssignTrackParent":
                TrackEvents.AssignParent(state.Base);
                break;
            case "SetMaterialProperty":
                objectPropertyManager.AssignMaterial(state);
                break;
            case "SetGlobalProperty":
                objectPropertyManager.AssignGlobal(state);
                break;
            case "SetAnimatorProperty":
                objectPropertyManager.AssignAnimator(state);
                break;
            case "InstantiatePrefab":
                properties.Sample(state.StartTime);
                instantiateObjectPrefabManager.InstantiateVivifyObject(state);
                break;
            case "DestroyObject":
                instantiateObjectPrefabManager.RemoveVivifyObjectById(state, Rendering.Destroy);
                break;
            case "Blit":
                Rendering.AddBlit(state);
                break;
            case "CreateScreenTexture":
                Rendering.CreateScreenTexture(state.Base.Data);
                break;
            case "CreateCamera":
                Rendering.Cameras.CreateCamera(state.Base.Data);
                break;
            case "SetCameraProperty":
                Rendering.Cameras.SetCameraProperty(state.Base.Data);
                break;
            case "SetRenderingSettings":
                renderingSettings.Assign(state);
                break;
            case "AssignObjectPrefab":
                AssignObjectPrefabManager.Assign(state, state.Sequence);
                break;
        }
    }

    public override void InsertData(BaseCustomEvent data)
    {
        var state = BuildState(data);
        state.Sequence = states.Count;
        states.Add(state);
        dirty = true;
    }

    private CustomEventStateData BuildState(BaseCustomEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.StartSecondTime = Atsc.GetSecondsFromBeat(data.SongBpmTime);
        var map = BeatSaberSongContainer.Instance.Map;
        var start = data.JsonTime;
        var duration = data.DataDuration ?? 0f;
        state.EndTimeAnimation = map.JsonTimeToSongBpmTime(start + duration) ?? start + duration;
        state.NormalizeTime = duration <= 0f
            ? _ => 1f
            : time => Mathf.Clamp01(((map.SongBpmTimeToJsonTime(time) ?? time) - start) / duration);
        return state;
    }

    public override void RemoveData(BaseCustomEvent reference, BaseCustomEvent original)
    {
        states.RemoveAll(state => ReferenceEquals(state.Base, reference));
        dirty = true;
    }

    protected override CustomEventStateData CreateState(BaseCustomEvent data) => new(data);

    private void OnDestroy()
    {
        ResetComponentEvents();
        ResetRuntime();
        if (postProcess != null) postProcess.AttachVivify(null);
        Rendering?.Dispose();
        Rendering = null;
    }
}

public class CustomEventStateData : StateData<BaseCustomEvent>
{
    public int Sequence;
    public float StartSecondTime;
    public float EndTimeAnimation;
    public System.Func<float, float> NormalizeTime;
    public CustomEventStateData(BaseCustomEvent data) : base(data) { }
}
