using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public class BasicLightEffect : BasicEventEffect<BasicLightStateData>
{
    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;

    [SerializeField] public float OffIntensity;
    [SerializeField] public bool LightOnStart;
    [SerializeField] public bool InvertColorScheme;
    // In Pyro, several fire switches use the same game ColorSO for both channels, so their flash has no alpha contrast.
    [SerializeField] public bool HighlightMatchesNormalColor;

    public static readonly float FadeTimeSecond = 1.5f;
    public static readonly float FlashTimeSecond = 0.6f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;

    [SerializeField] public List<Vector2> LightIdRemapEntries = new();
    [SerializeField] private List<LightController> lightEntries = new();
    private readonly Dictionary<int, int> lightIdRemap = new();
    private readonly Dictionary<int, LightController> lightIDToController = new();

    public readonly Dictionary<int, int> LightIDToLane = new();
    public readonly List<int> LaneToLightID = new();
    public readonly List<int[]> LaneToLightIDs = new(); // this also refer to propID

    private readonly Dictionary<LightController, (LightColorTween tween,
            BasicEventStateChunksContainer<BasicLightStateData> container)>
        controllerToContainer = new();

    private readonly List<(LightController controller, LightColorTween tween,
        BasicEventStateChunksContainer<BasicLightStateData> container)> activeControllers = new();

    private int activeSize => activeControllers.Count;

    // Enhancements can register thousands of lights. Seed indexes from serialized scene data once,
    // then update membership and append counters with each registration.
    // Fix O(n^2) issue on controllers. Big deal on envs with massive amounts of clones.
    private int nextRegistrationId = -1;
    private int nextRemapKey;
    private readonly HashSet<int> remapKeys = new();
    private readonly HashSet<LightController> registeredControllers = new();
    private readonly Dictionary<int, LightController> registeredIds = new();

    private readonly BasicEventChromaColorTimeline colorTimeline = new();
    private readonly List<LightController[]> propagationGroups = new();
    private Func<float, float> defaultFlashEasing;
    private Func<float, float> defaultFadeEasing;
    private int sampledColorVersion = -1;
    private float sampledRefreshTime = float.NegativeInfinity;

    private void Awake() => ReseedRegistrationCounters();
    private void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChanged;
    private void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChanged;

    public void Register(LightController controller, bool strict = true)
    {
        LightController overlight = null;
        if (registeredControllers.Contains(controller))
        {
            Debug.LogWarning($"{controller} is already registered in {this}");
            return;
        }

        if (strict && controller.ID != -1 && registeredIds.TryGetValue(controller.ID, out overlight))
        {
            var marker = controller.GetComponent<ChromaIDMarker>();
            var overmarker = overlight.GetComponent<ChromaIDMarker>();
            Debug.LogError(
                $"{marker.ChromaID} {controller.Type}:{controller.ID} is already used by:\n{overmarker.ChromaID} {overlight.Type}:{overlight.ID}; re-registering occupied as new");
            Unregister(overlight);
            overlight.ID = -1;
        }

        if (controller.ID == -1) controller.ID = 0;
        while (registeredIds.ContainsKey(controller.ID))
            controller.ID++;
        lightEntries.Add(controller);
        registeredControllers.Add(controller);
        registeredIds.Add(controller.ID, controller);
        if (controller.ID > nextRegistrationId) nextRegistrationId = controller.ID;
        if (overlight != null) Register(overlight, false);
    }

    // Chroma registers every enhancement light (duplicates, ILightWithId retargets, geometry lights) at the next
    // index of its event-type list and records a lightID-table entry for it (LightIDTableManager.RegisterIndex):
    // the mapper's requested key when _lightID is given (bumped while occupied), else the next free key. Pinning
    // controller.ID to the authored _lightID instead stole a scene light's index and bound the key to the wrong
    // light - e.g. RunwayAndTrapezoidLightsFollowChromaLightIdsAroundBeat170's duplicated runway lights.
    public void Register(LightController controller, int? requestedKey)
    {
        if (registeredControllers.Contains(controller))
        {
            Debug.LogWarning($"{controller} is already registered in {this}");
            return;
        }

        controller.ID = ++nextRegistrationId;
        lightEntries.Add(controller);
        registeredControllers.Add(controller);
        registeredIds.Add(controller.ID, controller);

        var key = requestedKey ?? nextRemapKey;
        while (remapKeys.Contains(key)) key++;
        remapKeys.Add(key);
        nextRemapKey = remapKeys.Count == 1 ? key + 1 : Mathf.Max(nextRemapKey, key + 1);
        LightIdRemapEntries.Add(new Vector2(key, controller.ID));

        // The serialized entries feed CalculateMapping, so the new key/index survives the post-enhancement
        // Reinitialize; adopt the controller into the dispatch structures immediately so lightID events
        // dispatch before that rebuild too. KamikaziLightArrayTest's generated-light fixture proved a full
        // RebuildLightIdMapping per registration is O(n^3): each call rebuilt the lane tables (IndexOf in a
        // loop) and reallocated the whole dispatch array. Only the dispatch dictionaries and the new
        // controller's state container are needed here; the lane tables rebuild once in the post-load
        // Reinitialize -> Initialize path.
        lightIDToController[controller.ID] = controller;
        lightIdRemap[key] = controller.ID;
        AdoptController(controller);
        activeControllers.Add((controller, controllerToContainer[controller].tween,
            controllerToContainer[controller].container));
    }

    // Shared state-container adoption used by Initialize, RebuildLightIdMapping, and incremental Register.
    private void AdoptController(LightController controller)
    {
        if (controllerToContainer.ContainsKey(controller)) return;
        controllerToContainer[controller] =
            (new LightColorTween(), InitializeStates(new BasicEventStateChunksContainer<BasicLightStateData>()));
        foreach (var state in controllerToContainer[controller].container.Collection.Select(chunk => chunk))
        {
            if (!LightOnStart) continue;
            state.Base.FloatValue = 1f;
            state.StartAlpha = state.EndAlpha = state.Base.FloatValue * OffIntensity;
        }
    }

    public void Unregister(LightController controller)
    {
        if (!lightEntries.Remove(controller)) return;

        // Chroma's ForceUnregister unbinds the first table entry that pointed at the freed index, so the authored
        // key falls back to identity rather than silently retargeting the re-registered light's new index.
        var remapIndex = LightIdRemapEntries.FindIndex(entry => (int)entry.y == controller.ID);
        if (remapIndex != -1)
        {
            remapKeys.Remove((int)LightIdRemapEntries[remapIndex].x);
            LightIdRemapEntries.RemoveAt(remapIndex);
        }

        // Removing the max ID must reseed the append counter or the next registration's Max+1 differs from
        // the tracked counter.
        if (controller.ID == nextRegistrationId)
            nextRegistrationId = lightEntries.Count != 0 ? lightEntries.Max(l => l.ID) : -1;

        // Drop the stale state container so UpdateTime stops driving the light in this effect (Chroma removes its
        // tween via ChromaLightSwitchEventEffect.UnregisterLight).
        controllerToContainer.Remove(controller);
        activeControllers.RemoveAll(x => x.controller == controller);
        RebuildLightIdMapping();
    }

    public bool SetColorForId(int lightId, Color color)
    {
        if (!lightIDToController.TryGetValue(lightId, out var controller)) return false;
        controller.SetColor(color);
        return true;
    }

    private void CalculateMapping()
    {
        LaneToLightID.Clear();
        LaneToLightIDs.Clear();
        LightIDToLane.Clear();
        lightIDToController.Clear();
        lightIdRemap.Clear();

        foreach (var x in lightEntries) lightIDToController[x.ID] = x;

        var reverseLightIdRemap = new Dictionary<int, int>();
        foreach (var lightId in LightIdRemapEntries)
        {
            lightIdRemap[(int)lightId.x] = (int)lightId.y;
            reverseLightIdRemap[(int)lightId.y] = (int)lightId.x;
        }

        var physicalLights = lightEntries
            .Where(x => x.IsPhysical)
            .Select(x => (controller: x, ID: reverseLightIdRemap.GetValueOrDefault(x.ID, x.ID)))
            .OrderBy(x => x.ID)
            .ToList();
        LaneToLightID.AddRange(physicalLights.Select(x => x.ID));
        LaneToLightIDs.AddRange(
            physicalLights
                .GroupBy(x => Mathf.RoundToInt(x.controller.transform.position.z))
                .OrderBy(x => x.Key)
                .Select(x => x.Select(y => y.ID).ToArray()));
        // LaneToLightID.IndexOf per entry made the rebuild O(n^2); a direct dictionary lookup keeps it
        // linear while TryAdd preserves IndexOf's first-match semantics on duplicate IDs.
        for (var i = 0; i < LaneToLightID.Count; i++) LightIDToLane.TryAdd(LaneToLightID[i], i);
    }

    private void CalculatePropagationGroups()
    {
        // Native propID uses registration order and ring membership, whereas editor lanes use
        // sorted light IDs and world Z. Cache the two different groupings independently at load time.
        var managers = FindObjectsByType<TrackLaneRingsManager>(FindObjectsSortMode.InstanceID);
        var ringGroups = new Dictionary<TrackLaneRing, int>();
        foreach (var manager in managers)
        {
            for (var i = 0; i < manager.Rings.Count; i++)
                ringGroups.TryAdd(manager.Rings[i], 1000 + i);

        }

        var groupIndexes = new Dictionary<int, int>();
        var groups = new List<List<LightController>>();
        foreach (var controller in lightEntries)
        {
            var groupKey = Mathf.RoundToInt(controller.transform.position.z);
            var ring = controller.GetComponentInParent<TrackLaneRing>();
            if (ring != null && ringGroups.TryGetValue(ring, out var ringKey))
                groupKey = ringKey;

            if (!groupIndexes.TryGetValue(groupKey, out var groupIndex))
            {
                groupIndex = groups.Count;
                groupIndexes.Add(groupKey, groupIndex);
                groups.Add(new List<LightController>());
            }

            groups[groupIndex].Add(controller);
        }

        propagationGroups.Clear();
        foreach (var group in groups)
            propagationGroups.Add(group.ToArray());

    }

    public override void Initialize()
    {
        // Shipping Chroma replaces the native switch only for an activated capability. Its
        // default Expo differs from vanilla even without an authored easing field.
        var useChromaTweens = (Settings.Instance.EmulateChromaLite || Settings.Instance.EmulateChromaAdvanced)
            && BeatmapRuntimeContext.MapDeclaresBasicChroma();
        defaultFlashEasing = useChromaTweens
            ? Easing.HeckNamed("easeOutCubic")
            : Easing.Cubic.Out;
        defaultFadeEasing = useChromaTweens
            ? Easing.HeckNamed("easeOutExpo")
            : Easing.Exponential.Out;
        colorTimeline.Initialize(BeatSaberSongContainer.Instance.Map, Settings.Instance.EmulateChromaLite);
        sampledColorVersion = -1;
        sampledRefreshTime = float.NegativeInfinity;
        CalculateMapping();
        CalculatePropagationGroups();
        ReseedRegistrationCounters();
        controllerToContainer.Clear();
        foreach (var controller in lightEntries.Select(x => x))
        {
            AdoptController(controller);
        }

        activeControllers.Clear();
        activeControllers.AddRange(
            controllerToContainer.Select(x => (x.Key, x.Value.tween, x.Value.container)));
    }

    // The append counters track Register's sequential Max+1 ID allocation; rebuild them whenever the
    // serialized lists may have been edited outside Register (scene deserialization, full rebuilds).
    private void ReseedRegistrationCounters()
    {
        registeredControllers.Clear();
        registeredIds.Clear();
        nextRegistrationId = lightEntries.Count == 0 ? -1 : lightEntries[0].ID;
        foreach (var controller in lightEntries)
        {
            registeredControllers.Add(controller);
            registeredIds.TryAdd(controller.ID, controller);
            nextRegistrationId = Mathf.Max(nextRegistrationId, controller.ID);
        }
        remapKeys.Clear();
        nextRemapKey = LightIdRemapEntries.Count == 0 ? 0 : (int)LightIdRemapEntries[0].x + 1;
        foreach (var entry in LightIdRemapEntries)
        {
            var key = (int)entry.x;
            remapKeys.Add(key);
            nextRemapKey = Mathf.Max(nextRemapKey, key + 1);
        }
    }

    // Environment enhancements register their generated ILightWithId lights during 
    // HardRefresh, which runs AFTER Descriptor.Initialize built lightIDToController
    // from the scene's serialized lights. Every customData.lightID-filtered event targeting a generated light
    // then resolved to nothing and the light never received a color (unlit TransparentLight geometry renders
    // nothing). Rebuild the ID mapping and adopt post-initialize registrations once, after the enhancement
    // load has finished, so lightID routing covers the full registered light set.
    public void RebuildLightIdMapping()
    {
        CalculateMapping();
        CalculatePropagationGroups();
        ReseedRegistrationCounters();
        foreach (var controller in lightEntries)
        {
            AdoptController(controller);
        }

        activeControllers.Clear();
        activeControllers.AddRange(
            controllerToContainer.Select(x => (x.Key, x.Value.tween, x.Value.container)));
    }

    public override void Refresh()
    {
        for (var i = 0; i < activeSize; i++)
        {
            var (controller, tween, _) = activeControllers[i];
            controller.SetColor(tween.Color);
        }
    }

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        var frame = colorTimeline.GetFrame(currentTime, ColorBoostEffect.GetLastChange(currentTime));
        var refreshColors = sampledColorVersion != colorTimeline.Version
            || sampledRefreshTime != frame.RefreshTime
            || frame.ActiveGradient;
        for (var i = 0; i < activeSize; i++)
        {
            var (controller, tween, container) = activeControllers[i];
            var stateChanged = !container.IsCurrentOrFindState(currentTime, isPlaying);
            if (stateChanged || refreshColors)
                UpdateObject(tween, container.CurrentState, frame);

            if (tween.UpdateTime(currentTime))
                controller.SetColor(tween.Color);

        }

        sampledColorVersion = colorTimeline.Version;
        sampledRefreshTime = frame.RefreshTime;
    }

    private void UpdateObject(LightColorTween tween, BasicLightStateData stateData)
    {
        var time = Atsc.CurrentSongBpmTime;
        UpdateObject(tween, stateData, colorTimeline.GetFrame(time, ColorBoostEffect.GetLastChange(time)));
    }

    private void UpdateObject(
        LightColorTween tween,
        BasicLightStateData stateData,
        BasicEventChromaColorTimeline.Frame frame)
    {
        var sourceColor = colorTimeline.GetSourceColor(stateData.Base);
        if (frame.ActiveGradient || frame.RefreshTime > stateData.StartTime)
            sourceColor = frame.Color;

        // Soft colorizer refreshes replace a tween's source color, but its next transition still
        // resolves that node's own color data. Fixed flash/fade clocks also retain their start time.
        var targetColor = stateData.TransitionTarget != null
            ? colorTimeline.GetColorData(stateData.TransitionTarget)
            : sourceColor;
        tween.StartTimeAlpha = stateData.StartTime;
        tween.StartTimeColor = stateData.StartTime;
        tween.StartAlpha = stateData.StartAlpha;
        tween.StartColor = GetPreviewLightColor(stateData, sourceColor, false);
        tween.EndTimeAlpha = stateData.EndTimeAlpha;
        tween.EndTimeColor = stateData.EndTimeAlpha;
        tween.EndAlpha = stateData.EndAlpha;
        tween.EndColor = GetPreviewLightColor(stateData, targetColor, true);
        tween.ColorLerpType = stateData.ColorLerpType;
        tween.Easing = stateData.Easing;
        tween.ColorEasing = stateData.Easing;
        tween.ComposeAlphaAtColorEndpoints = true;
    }

    private Color GetPreviewLightColor(BasicLightStateData stateData, Color? customColor, bool end)
    {
        var lightColor = end ? stateData.EndColor : stateData.StartColor;
        var colorEvent = end && stateData.TransitionTarget != null
            ? stateData.TransitionTarget
            : stateData.Base;
        var color = (colorEvent.IsWhite
            ? null
            : customColor)
            ?? ColorSchemeProvider.ColorScheme.GetColorFrom(lightColor, InvertColorScheme);
        var isOff = end ? stateData.Base.IsFade : stateData.Base.IsOff;
        var isHighlight = !end && (stateData.Base.IsFade || stateData.Base.IsFlash);
        // Native switches apply ColorSO alpha before dispatch to every adapter, including
        // combined-light and lightmap inputs. Normalizing it here overexposes their outputs.
        if (isOff)
            color.a = 1f;
        else if (!HighlightMatchesNormalColor)
        {
            color = BasicEventLightIntensity.ApplyNative(
                color,
                lightColor == LightColor.White,
                ColorBoostEffect.Boost,
                isHighlight);
        }

        return color;
    }

    private void HandleBoostChanged(bool boost)
    {
        var time = Atsc.CurrentSongBpmTime;
        var frame = colorTimeline.GetFrame(time, ColorBoostEffect.GetLastChange(time));
        for (var i = 0; i < activeSize; i++)
        {
            var (controller, tween, container) = activeControllers[i];
            UpdateObject(tween, container.CurrentState, frame);
            if (tween.UpdateTime(time))
                controller.SetColor(tween.Color);
        }
    }

    protected override BasicLightStateData CreateState(BaseEvent data) => new(data);

    protected override void OnInsertUpdateToPreviousState(
        BasicLightStateData newStateData,
        BasicLightStateData previousStateData)
    {
        base.OnInsertUpdateToPreviousState(newStateData, previousStateData);

        if (newStateData.Base.IsTransition && IsValidEventToTransition(previousStateData.Base))
        {
            LinkTransition(previousStateData, newStateData);
            return;
        }

        previousStateData.TransitionTarget = null;
        previousStateData.EndColor = previousStateData.StartColor;

        if (!previousStateData.Base.IsFade && !previousStateData.Base.IsFlash)
        {
            previousStateData.EndTimeAlpha = newStateData.StartTime;
            previousStateData.EndAlpha = previousStateData.StartAlpha;
        }

        if (previousStateData.Base.IsOff)
        {
            previousStateData.StartAlpha =
                previousStateData.EndAlpha = previousStateData.Base.FloatValue * OffIntensity;
        }

        if (newStateData.Base.IsOff)
            newStateData.StartColor = previousStateData.EndColor;

    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        BasicLightStateData newStateData,
        BasicLightStateData previousStateData,
        BasicLightStateData nextStateData)
    {
        if (newStateData.Base.IsOff && !nextStateData.Base.IsTransition)
            newStateData.StartColor = newStateData.EndColor = previousStateData.StartColor;
    }

    protected override void OnInsertUpdateFromNextState(
        BasicLightStateData newStateData,
        BasicLightStateData nextStateData)
    {
        base.OnInsertUpdateFromNextState(newStateData, nextStateData);
        if (nextStateData.Base.IsTransition && IsValidEventToTransition(newStateData.Base))
        {
            LinkTransition(newStateData, nextStateData);
            return;
        }

        if (!newStateData.Base.IsFade && !newStateData.Base.IsFlash)
            newStateData.EndTimeAlpha = nextStateData.StartTime;
    }

    protected override void OnInsertUpdateToNextState(
        BasicLightStateData newStateData,
        BasicLightStateData nextState)
    {
        if (nextState.Base.IsOff)
            nextState.StartColor = nextState.EndColor = newStateData.StartColor;

    }

    public override void InsertData(BaseEvent data)
    {
        colorTimeline.Insert(data);
        if (Settings.Instance.EmulateChromaLite
            && (data.Value >= ColourManager.RgbintOffset || data.Value == ColourManager.RGBReset))
        {
            return;
        }

        var affectedLights = GetAffectedLights(data);
        var defaultEasing = data.IsFlash
            ? defaultFlashEasing
            : data.IsFade
                ? defaultFadeEasing
                : Easing.Linear;
        var easing = data.CustomEasing != null
            ? Easing.HeckNamed(data.CustomEasing)
            : defaultEasing;
        var colorLerpType = BasicEventColorLerp.FromSerializedName(data.CustomLerpType);

        foreach (var lightingObject in affectedLights)
        {
            var newState = CreateState(data);
            newState.Easing = easing;
            newState.ColorLerpType = colorLerpType;
            newState.StartTime = data.SongBpmTime;
            newState.StartColor = InferColorFromEvent(data);
            newState.StartAlpha = data.FloatValue;
            newState.EndTime = float.MaxValue;
            newState.EndTimeAlpha = float.MaxValue;
            newState.EndColor = InferColorFromEvent(data);
            newState.EndAlpha = data.FloatValue;

            if (data.IsOff)
                newState.StartAlpha = newState.EndAlpha = data.FloatValue * OffIntensity;
            else if (data.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.StartAlpha = data.FloatValue;
                newState.EndAlpha = data.FloatValue;
            }
            else if (data.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                // Native fade retains its 1.5-second highlight-to-off clock when interrupted.
                newState.StartAlpha = data.FloatValue;
                newState.EndAlpha = data.FloatValue * OffIntensity;
            }

            var (tween, container) = controllerToContainer[lightingObject];

            var previousState = container.CurrentState;
            var previousValid = previousState.IsWithinRange(data.SongBpmTime);
            HandleInsertState(container, newState);

            if (!previousValid)
                continue;

            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(tween, container.CurrentState);
        }
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        colorTimeline.Remove(reference);
        if (Settings.Instance.EmulateChromaLite
            && (original.Value >= ColourManager.RgbintOffset || original.Value == ColourManager.RGBReset))
        {
            return;
        }

        var affectedLights = GetAffectedLights(original);

        foreach (var lightingObject in affectedLights)
        {
            var (tween, container) = controllerToContainer[lightingObject];

            HandleRemoveState(container, reference, original);

            var (_, _, previousState) = container.GetStateAt(Atsc.CurrentSongBpmTime);
            if (!previousState.IsWithinRange(reference.SongBpmTime))
                continue;

            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(tween, container.CurrentState);
        }
    }

    protected override void
        OnRemoveUpdatePreviousAndNextState(
            BasicLightStateData currentStateData,
            BasicLightStateData previousStateData,
            BasicLightStateData nextStateData)
    {
        base.OnRemoveUpdatePreviousAndNextState(currentStateData, previousStateData, nextStateData);
        if (nextStateData.Base.IsTransition && IsValidEventToTransition(previousStateData.Base))
        {
            LinkTransition(previousStateData, nextStateData);
        }
        else
        {
            previousStateData.TransitionTarget = null;
            // Removing a later event must not stretch the fixed flash/fade clock.
            previousStateData.EndColor = previousStateData.StartColor;

            if (!previousStateData.Base.IsFade && !previousStateData.Base.IsFlash)
            {
                previousStateData.EndTimeAlpha = nextStateData.StartTime;
                previousStateData.EndAlpha = previousStateData.StartAlpha;
            }

            if (previousStateData.Base.IsOff)
            {
                previousStateData.StartAlpha =
                    previousStateData.EndAlpha = previousStateData.Base.FloatValue * OffIntensity;
            }

            if (nextStateData.Base.IsOff)
                nextStateData.StartColor = previousStateData.EndColor;

        }
    }

    private static void LinkTransition(BasicLightStateData source, BasicLightStateData target)
    {
        if (source.Base.IsOff)
            source.StartColor = target.StartColor;

        source.EndTimeAlpha = target.StartTime;
        source.EndColor = target.StartColor;
        source.TransitionTarget = target.Base;
        source.EndAlpha = target.StartAlpha;
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    private static bool IsValidEventToTransition(BaseEvent evt) => evt.IsOn || evt.IsOff || evt.IsTransition;

    private IEnumerable<LightController> GetAffectedLights(BaseEvent data)
    {
        if (Settings.Instance.EmulateChromaAdvanced)
        {
            if (data.CustomLegacyPropID.HasValue)
            {
                var groupId = data.CustomLegacyPropID.Value;
                return groupId >= 0 && groupId < propagationGroups.Count
                    ? propagationGroups[groupId]
                    : Array.Empty<LightController>();
            }

            if (data.CustomLightID != null)
                return GetLightControllerFromLightIds(data);

        }

        return lightIDToController.Values;
    }

    private IEnumerable<LightController> GetLightControllerFromLightIds(BaseEvent data)
    {
        var set = new HashSet<int>();
        for (var i = 0; i < data.CustomLightID.Length; i++)
        {
            var lightID = data.CustomLightID[i];
            var newId = lightIdRemap.GetValueOrDefault(lightID, lightID);
            if (!set.Add(newId)) continue;
            if (!lightIDToController.TryGetValue(newId, out var controller))
                continue;

            yield return controller;
        }
    }
}

public class BasicLightStateData : BasicEventStateData
{
    public LightColor StartColor;
    public float StartAlpha;

    public float EndTimeAlpha;
    public LightColor EndColor;
    public float EndAlpha;

    public Func<float, float> Easing = global::Easing.Linear;
    public BasicEventColorLerpType ColorLerpType;

    public BaseEvent TransitionTarget;

    public BasicLightStateData(BaseEvent evt) : base(evt)
    {
    }
}
