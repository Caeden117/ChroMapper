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

    // Fix O(n^2) issue on controllers. Big deal on envs with massive amounts of clones.
    private int nextRegistrationId = -1;
    private HashSet<int> remapKeys;

    private List<ChromaLiteData> chromaLiteData = new();
    private List<ChromaGradientData> chromaGradientData = new();

    private void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChanged;
    private void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChanged;

    public void Register(LightController controller, bool strict = true)
    {
        LightController overlight = null;
        if (lightEntries.Exists(l => l == controller))
        {
            Debug.LogWarning($"{controller} is already registered in {this}");
            return;
        }

        if (strict && controller.ID != -1 && lightEntries.Exists(l => l.ID == controller.ID))
        {
            overlight = lightEntries.First(l => l.ID == controller.ID);
            var marker = controller.GetComponent<ChromaIDMarker>();
            var overmarker = overlight.GetComponent<ChromaIDMarker>();
            Debug.LogError(
                $"{marker.ChromaID} {controller.Type}:{controller.ID} is already used by:\n{overmarker.ChromaID} {overlight.Type}:{overlight.ID}; re-registering occupied as new");
            Unregister(overlight);
            overlight.ID = -1;
        }

        if (controller.ID == -1) controller.ID = 0;
        while (lightEntries.Exists(l => l.ID == controller.ID)) controller.ID++;
        lightEntries.Add(controller);
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
        if (lightEntries.Exists(l => l == controller))
        {
            Debug.LogWarning($"{controller} is already registered in {this}");
            return;
        }

        if (nextRegistrationId < 0)
            nextRegistrationId = lightEntries.Count != 0 ? lightEntries.Max(l => l.ID) : -1;
        controller.ID = ++nextRegistrationId;
        lightEntries.Add(controller);

        remapKeys ??= LightIdRemapEntries.Select(entry => (int)entry.x).ToHashSet();
        var key = requestedKey ?? (remapKeys.Count != 0 ? remapKeys.Max() + 1 : 0);
        while (remapKeys.Contains(key)) key++;
        remapKeys.Add(key);
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
            remapKeys?.Remove((int)LightIdRemapEntries[remapIndex].x);
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

    public override void Initialize()
    {
        // Reinitialization rebuilds the event cache, so discard auxiliary Chroma state before re-inserting map events.
        chromaLiteData.Clear();
        chromaGradientData.Clear();
        CalculateMapping();
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
        nextRegistrationId = lightEntries.Count != 0 ? lightEntries.Max(l => l.ID) : -1;
        remapKeys = LightIdRemapEntries.Select(entry => (int)entry.x).ToHashSet();
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
        for (var i = 0; i < activeSize; i++)
        {
            var (controller, tween, container) = activeControllers[i];
            if (!container.IsCurrentOrFindState(currentTime, isPlaying))
                UpdateObject(tween, container.CurrentState);

            if (tween.UpdateTime(currentTime)) controller.SetColor(tween.Color);
        }
    }

    private void UpdateObject(LightColorTween tween, BasicLightStateData stateData)
    {
        tween.StartTimeAlpha = stateData.StartTime;
        tween.StartTimeColor = stateData.StartTimeColor;
        tween.StartAlpha = stateData.StartAlpha;
        tween.StartColor = GetPreviewLightColor(stateData, false);

        tween.EndTimeAlpha = stateData.EndTimeAlpha;
        tween.EndTimeColor = stateData.EndTimeColor;
        tween.EndAlpha = stateData.EndAlpha;
        tween.EndColor = GetPreviewLightColor(stateData, true);

        // The state caches serialized lerpType classification so the per-frame tween never compares strings.
        tween.ColorLerpType = stateData.ColorLerpType;
        tween.Easing = stateData.Easing;
        tween.ColorEasing = stateData.HasChromaGradient
            ? stateData.GradientEasing
            : stateData.Easing;
        // Chroma composes endpoints only when color and brightness share the ordinary Basic Event timeline.
        tween.ComposeAlphaAtColorEndpoints = !stateData.HasChromaGradient
            && Mathf.Approximately(stateData.StartTimeColor, stateData.StartTime)
            && Mathf.Approximately(stateData.EndTimeColor, stateData.EndTimeAlpha);
    }

    public void UpdateStartAndEndColor(LightColorTween tween, BasicLightStateData stateData)
    {
        // A boost toggle changes the highlight-to-normal contrast while a fixed-duration
        // tween is active, so refresh both endpoints through the same path.
        tween.StartColor = GetPreviewLightColor(stateData, false);
        tween.EndColor = GetPreviewLightColor(stateData, true);
    }

    private Color GetPreviewLightColor(BasicLightStateData stateData, bool end)
    {
        var lightColor = end ? stateData.EndColor : stateData.StartColor;
        var color = (end ? stateData.EndChromaColor : stateData.StartChromaColor)
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
                color, lightColor == LightColor.White, ColorBoostEffect.Boost, isHighlight);
        }

        return color;
    }

    private void HandleBoostChanged(bool boost)
    {
        for (var i = 0; i < activeSize; i++)
        {
            var (controller, tween, container) = activeControllers[i];
            UpdateStartAndEndColor(tween, container.CurrentState);
            if (tween.UpdateTime(Atsc.CurrentSongBpmTime))
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
            if (previousStateData.Base.IsOff) previousStateData.StartColor = newStateData.StartColor;
            previousStateData.EndTimeAlpha = newStateData.StartTime;
            previousStateData.EndTimeColor = newStateData.StartTime;
            previousStateData.EndColor = newStateData.StartColor;
            previousStateData.EndChromaColor = newStateData.StartChromaColor;
            previousStateData.EndAlpha = newStateData.StartAlpha;
            // Basic Event transition interpolation is serialized on the preceding source node. Chroma
            // resolves the authored _easing through Heck's table, so HeckNamed applies its variants.
            previousStateData.Easing = Easing.HeckNamed(previousStateData.Base.CustomEasing ?? "easeLinear");
            previousStateData.ColorLerpType = BasicEventColorLerp.FromSerializedName(
                previousStateData.Base.CustomLerpType);
            return;
        }

        if (!previousStateData.HasChromaGradient && !previousStateData.Base.IsFade
            && !previousStateData.Base.IsFlash)
        {
            previousStateData.EndTimeColor = newStateData.StartTimeColor;
            previousStateData.EndChromaColor = previousStateData.StartChromaColor;
        }

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

        if (newStateData.Base.IsOff) newStateData.StartColor = previousStateData.EndColor;
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
            if (newStateData.Base.IsOff) newStateData.StartColor = nextStateData.StartColor;
            newStateData.EndTimeAlpha = nextStateData.StartTime;
            newStateData.EndTimeColor = nextStateData.StartTime;
            newStateData.EndColor = nextStateData.StartColor;
            newStateData.EndChromaColor = nextStateData.StartChromaColor;
            newStateData.EndAlpha = nextStateData.StartAlpha;
            // Basic Event transition interpolation is serialized on the preceding source node. Chroma
            // resolves the authored _easing through Heck's table, so HeckNamed applies its variants.
            newStateData.Easing = Easing.HeckNamed(newStateData.Base.CustomEasing ?? "easeLinear");
            newStateData.ColorLerpType = BasicEventColorLerp.FromSerializedName(newStateData.Base.CustomLerpType);
            return;
        }

        if (!newStateData.Base.IsFade && !newStateData.Base.IsFlash)
            newStateData.EndTimeAlpha = nextStateData.StartTime;
    }

    protected override void OnInsertUpdateToNextState(
        BasicLightStateData newStateData,
        BasicLightStateData nextState)
    {
        if (nextState.Base.IsOff) nextState.StartColor = nextState.EndColor = newStateData.StartColor;
    }

    private void UpdateExistingWithChromaLite(float time)
    {
        var fromIndex = chromaLiteData.FindLastIndex(cl => cl.Base.SongBpmTime <= time);
        var from = fromIndex != -1 && fromIndex < chromaLiteData.Count
            ? chromaLiteData[fromIndex]
            : new ChromaLiteData { Base = new BaseEvent { songBpmTime = float.MinValue } };

        var untilIndex = chromaLiteData.FindIndex(cl => cl.Base.SongBpmTime > time);
        var until = untilIndex != -1 ? chromaLiteData[untilIndex].Base.SongBpmTime : float.MaxValue;

        foreach (var enumerator in controllerToContainer.Values.Select(c =>
            c.container.Collection.EnumerateFrom(from.Base.SongBpmTime)))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= until) break;
                if (state.Base.CustomColor == null) state.StartChromaColor = state.EndChromaColor = from.Color;
            }
        }
    }

    // i would like if chroma gradient just stopped working entirely so i dont have to deal with this shit again
    private void UpdateExistingWithChromaGradient(float startTime, float endTime)
    {
        foreach (var (container, enumerator) in controllerToContainer.Values.Select(c =>
            (c.container, c.container.Collection.EnumerateFrom(startTime))))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= endTime) break;

                var fromIndex = chromaGradientData.FindLastIndex(cl =>
                    cl.StartTime <= state.StartTime && state.StartTime <= cl.EndTime);
                if (fromIndex == -1)
                {
                    // Removing or moving a legacy gradient must return affected states to ordinary insertion semantics.
                    state.HasChromaGradient = false;
                    // Clear the removed gradient's interpolation mode before ordinary transition linking classifies it again.
                    state.ColorLerpType = BasicEventColorLerpType.RGB;
                    state.GradientEasing = null;
                    state.StartTimeColor = state.StartTime;
                    state.EndTimeColor = state.EndTime;

                    if (state.Base.IsFlash)
                        state.Easing = Easing.Cubic.Out;
                    else if (state.Base.IsFade)
                        state.Easing = Easing.Exponential.Out;
                    else
                        state.Easing = Easing.Linear;

                    state.StartChromaColor = state.EndChromaColor = null;
                    if (state.Base.CustomColor != null
                        && Settings.Instance.EmulateChromaLite
                        && !state.Base.IsWhite)
                        state.StartChromaColor = state.EndChromaColor = (Color)state.Base.CustomColor;

                    if (chromaLiteData.Count > 0)
                    {
                        var chromaLiteIndex =
                            chromaLiteData.FindLastIndex(data =>
                                data.Base.SongBpmTime <= state.Base.SongBpmTime);
                        if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                            state.StartChromaColor = state.EndChromaColor = chromaLiteData[chromaLiteIndex].Color;
                    }
                }
                else
                {
                    var from = chromaGradientData[fromIndex];
                    UpdateStateWithChromaGradient(state, from);
                }

                var prevState = container.GetPreviousStateFrom(state);
                var nextState = container.GetNextStateFrom(state);

                OnInsertUpdateToPreviousState(state, prevState);
                OnInsertUpdateFromPreviousStateAndNextState(state, prevState, nextState);
                OnInsertUpdateFromNextState(state, nextState);
                OnInsertUpdateToNextState(state, nextState);
            }
        }
    }

    private void InsertWithChromaGradient(BasicLightStateData stateData)
    {
        var chromaGradientIndex =
            chromaGradientData.FindLastIndex(cg =>
                cg.StartTime <= stateData.StartTime && stateData.StartTime <= cg.EndTime);
        if (chromaGradientIndex != -1)
            UpdateStateWithChromaGradient(stateData, chromaGradientData[chromaGradientIndex]);
    }

    private void UpdateStateWithChromaGradient(BasicLightStateData stateData, ChromaGradientData chromaGradientData)
    {
        if (stateData.Base.IsOff)
            return;

        stateData.StartTimeColor = chromaGradientData.StartTime;
        stateData.EndTimeColor = chromaGradientData.EndTime;
        stateData.StartChromaColor = chromaGradientData.StartColor;
        stateData.EndChromaColor = chromaGradientData.EndColor;
        // BasicEventChromaColorParityTest proves Chroma applies gradient easing only to its RGB color interpolation.
        stateData.GradientEasing = chromaGradientData.Easing;
        stateData.ColorLerpType = BasicEventColorLerpType.RGB;
        // Cache the classification on the state so later insertion remains O(1) without rescanning gradient data.
        stateData.HasChromaGradient = true;
    }

    public override void InsertData(BaseEvent data)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (data.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteData.Add(
                        new ChromaLiteData { Base = data, Color = ColourManager.ColourFromInt(data.Value) });
                    chromaLiteData = chromaLiteData.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteData.Add(new ChromaLiteData { Base = data, Color = null });
                    chromaLiteData = chromaLiteData.OrderBy(cl => cl.Base.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(data.SongBpmTime);
                    return; // this was a break, not sure why
                }
        }

        //Check if it is a PogU new Chroma event
        if (data.CustomColor != null
            && Settings.Instance.EmulateChromaLite
            && !data.IsWhite) // White overrides Chroma
            chromaColor = (Color)data.CustomColor;

        if (chromaLiteData.Count > 0)
        {
            var chromaLiteIndex = chromaLiteData.FindLastIndex(d => d.Base.SongBpmTime <= data.SongBpmTime);
            if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                chromaColor = chromaLiteData[chromaLiteIndex].Color;
        }

        if (data.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientData.Add(
                new ChromaGradientData
                {
                    Base = data,
                    StartTime = data.SongBpmTime,
                    EndTime =
                        data.SongBpmTime
                        + data.CustomLightGradient.Duration, // TODO: duration is not actual song bpm time
                    StartColor = data.CustomLightGradient.StartColor,
                    EndColor = data.CustomLightGradient.EndColor,
                    // ChromaGradientController eases _lightGradient colors through Heck's table.
                    Easing = Easing.HeckNamed(data.CustomLightGradient.EasingType)
                });
            chromaGradientData = chromaGradientData.OrderBy(cl => cl.StartTime).ToList();
            UpdateExistingWithChromaGradient(data.SongBpmTime, data.SongBpmTime + data.CustomLightGradient.Duration);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && data.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        var affectedLights = data.CustomLightID != null && Settings.Instance.EmulateChromaAdvanced
            ? GetLightControllerFromLightIds(data)
            : lightIDToController.Values.AsEnumerable();

        foreach (var lightingObject in affectedLights)
        {
            var newState = CreateState(data);
            newState.StartTime = data.SongBpmTime;
            newState.StartTimeColor = data.SongBpmTime;
            newState.StartColor = InferColorFromEvent(data);
            newState.StartChromaColor = chromaColor;
            newState.StartAlpha = data.FloatValue;
            newState.EndTime = float.MaxValue;
            newState.EndTimeAlpha = float.MaxValue;
            newState.EndTimeColor = float.MaxValue;
            newState.EndColor = InferColorFromEvent(data);
            newState.EndChromaColor = chromaColor;
            newState.EndAlpha = data.FloatValue;

            if (data.IsOff)
                newState.StartAlpha = newState.EndAlpha = data.FloatValue * OffIntensity;
            else if (data.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.EndTimeColor = newState.EndTimeAlpha;
                newState.StartAlpha = data.FloatValue;
                newState.EndAlpha = data.FloatValue;
                newState.Easing = Easing.Cubic.Out;
            }
            else if (data.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                // Native fade samples both color and brightness at the same 1.5-second time;
                // a later event may interrupt the state but must not stretch this endpoint.
                newState.EndTimeColor = newState.EndTimeAlpha;
                // Beat Saber fades from its brighter highlight ColorSO to the off alpha;
                // the 1.5-second OutExpo clock is already represented by EndTimeAlpha.
                newState.StartAlpha = data.FloatValue;
                newState.Easing = Easing.Exponential.Out;
                newState.EndAlpha = data.FloatValue * OffIntensity;
            }

            InsertWithChromaGradient(newState);

            var (tween, container) = controllerToContainer[lightingObject];

            // let's assume this will be previous state if this is inserted within the range
            var previousState = container.CurrentState;
            var previousValid = previousState.IsWithinRange(data.SongBpmTime);
            HandleInsertState(container, newState);

            if (!previousValid) continue;
            container.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(tween, container.CurrentState);
        }

    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        switch (original.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    var d = chromaLiteData.Find(d => d.Base == reference);
                    chromaLiteData.Remove(d);
                    UpdateExistingWithChromaLite(original.SongBpmTime);
                    return;
                }
        }

        if (original.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            var d = chromaGradientData.Find(d => d.Base == reference);
            chromaGradientData.Remove(d);
            UpdateExistingWithChromaGradient(
                original.SongBpmTime,
                original.SongBpmTime + original.CustomLightGradient.Duration);
        }

        var affectedLights = original.CustomLightID != null && Settings.Instance.EmulateChromaAdvanced
            ? GetLightControllerFromLightIds(original)
            : lightIDToController.Values.AsEnumerable();

        foreach (var lightingObject in affectedLights)
        {
            var (tween, container) = controllerToContainer[lightingObject];

            HandleRemoveState(container, reference, original);

            // unfortunately, we cannot do the same as insertion so we need to search
            var (_, _, previousState) = container.GetStateAt(Atsc.CurrentSongBpmTime);
            if (!previousState.IsWithinRange(reference.SongBpmTime)) continue;
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
            if (previousStateData.Base.IsOff) previousStateData.StartColor = nextStateData.StartColor;
            previousStateData.EndTimeAlpha = nextStateData.StartTime;
            previousStateData.EndTimeColor = nextStateData.StartTimeColor;
            previousStateData.EndColor = nextStateData.StartColor;
            previousStateData.EndChromaColor = nextStateData.StartChromaColor;
            previousStateData.EndAlpha = nextStateData.StartAlpha;
            // Basic Event transition interpolation is serialized on the preceding source node. Chroma
            // resolves the authored _easing through Heck's table, so HeckNamed applies its variants.
            previousStateData.Easing = Easing.HeckNamed(previousStateData.Base.CustomEasing ?? "easeLinear");
            previousStateData.ColorLerpType = BasicEventColorLerp.FromSerializedName(
                previousStateData.Base.CustomLerpType);
        }
        else
        {
            // Fixed-duration flash/fade colors retain their own clock when another event is
            // removed; only ordinary events link their color endpoint to the next event.
            if (!previousStateData.Base.IsFade && !previousStateData.Base.IsFlash)
            {
                previousStateData.EndTimeColor = nextStateData.StartTimeColor;
                previousStateData.EndColor = previousStateData.StartColor;
                previousStateData.EndChromaColor = previousStateData.StartChromaColor;
            }

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

            if (nextStateData.Base.IsOff) nextStateData.StartColor = previousStateData.EndColor;
        }

        InsertWithChromaGradient(previousStateData);
        InsertWithChromaGradient(nextStateData);
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    private static bool IsValidEventToTransition(BaseEvent evt) => evt.IsOn || evt.IsOff || evt.IsTransition;

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

    public struct ChromaLiteData : IEquatable<ChromaLiteData>
    {
        public BaseEvent Base;
        public Color? Color;

        public bool Equals(ChromaLiteData other) => Equals(Base, other.Base);
        public override bool Equals(object obj) => obj is ChromaLiteData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Base, Color);
    }

    public struct ChromaGradientData : IEquatable<ChromaGradientData>
    {
        public BaseEvent Base;
        public float StartTime;
        public float EndTime;
        public Color StartColor;
        public Color EndColor;
        public Func<float, float> Easing;

        public bool Equals(ChromaGradientData other) => Equals(Base, other.Base);
        public override bool Equals(object obj) => obj is ChromaGradientData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Base, StartTime, EndTime, StartColor, EndColor, Easing);
    }
}

public class BasicLightStateData : BasicEventStateData
{
    public float
        StartTimeColor = float.MinValue; // this is supposedly the same as start time, special case for chroma gradient

    public LightColor StartColor;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTimeAlpha; // similarly this match next start, otherwise used to interpolate flash/fade
    public float EndTimeColor; // also same case above, only special case for chroma gradient
    public LightColor EndColor;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing = global::Easing.Linear;
    public Func<float, float> GradientEasing;
    public BasicEventColorLerpType ColorLerpType;

    // Preserve authored gradient timing and colors across subsequent event insertion without querying the gradient list.
    public bool HasChromaGradient;

    public BasicLightStateData(BaseEvent evt) : base(evt) { }
}
