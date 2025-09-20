using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class BasicLightManager : BasicEventManager<BasicLightState>
{
    public static PlatformColorScheme ColorScheme;
    private static bool useBoost;

    [FormerlySerializedAs("disableCustomInitialization")]
    public bool DisableCustomInitialization;

    public static readonly float FadeTimeSecond = 1.2f;
    public static readonly float FlashTimeSecond = 0.5f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    public List<LightingObject> ControllingLights = new();
    public LightGroup[] LightsGroupedByZ = { };

    public List<RotatingLightsManagerBase> RotatingLights = new();

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    public Dictionary<int, LightingObject> LightIDMap;

    private readonly Dictionary<LightingObject, EventStateChunksContainer<BasicLightState>> stateChunksContainerMap =
        new();

    private List<ChromaLiteData> chromaLiteDatas = new();
    private List<ChromaGradientData> chromaGradientDatas = new();

    private void Start()
    {
        Priority = EventPriority.Light;
        LoadOldLightOrder();
    }

    public void LoadOldLightOrder()
    {
        if (!DisableCustomInitialization)
        {
            foreach (var e in GetComponentsInChildren<LightingObject>())
                // No, stop that. Enforcing Light ID breaks Glass Desert
                if (!e.OverrideLightGroup)
                    ControllingLights.Add(e);

            foreach (var e in GetComponentsInChildren<RotatingLightsManagerBase>())
                if (!e.IsOverrideLightGroup())
                    RotatingLights.Add(e);

            var lightIdOrder = ControllingLights
                .OrderBy(x => x.LightID)
                .GroupBy(x => x.LightID)
                .Select(x => x.First())
                .ToList();
            LightIDPlacementMap = lightIdOrder.ToDictionary(x => lightIdOrder.IndexOf(x), x => x.LightID);
            LightIDPlacementMapReverse = lightIdOrder.ToDictionary(x => x.LightID, x => lightIdOrder.IndexOf(x));
            LightIDMap = lightIdOrder.ToDictionary(x => x.LightID, x => x);

            LightsGroupedByZ = GroupLightsBasedOnZ();
            RotatingLights = RotatingLights.OrderBy(x => x.transform.localPosition.z).ToList();
        }
    }

    public LightGroup[] GroupLightsBasedOnZ() =>
        ControllingLights
            .Where(x => x.gameObject.activeInHierarchy)
            .Where(x => x.PropGroup >= 0)
            .GroupBy(x => Mathf.RoundToInt(x.PropGroup))
            .OrderBy(x => x.Key)
            .Select(x => new LightGroup { Lights = x.ToList() })
            .ToArray();

    public override void Initialize()
    {
        stateChunksContainerMap.Clear();
        foreach (var lightingObject in ControllingLights)
        {
            stateChunksContainerMap[lightingObject] =
                InitializeStates(new EventStateChunksContainer<BasicLightState>());
            foreach (var state in stateChunksContainerMap[lightingObject].Chunks.SelectMany(chunk => chunk))
            {
                if (lightingObject.CanBeTurnedOff) continue;
                state.CanBeTurnedOff = false;
                state.BaseEvent.FloatValue = 1f;
                state.StartAlpha = state.EndAlpha = GetNoTurnOffAlpha(state.BaseEvent.FloatValue);
            }
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var (lightingObject, container) in stateChunksContainerMap)
        {
            var previousState = container.CurrentState;
            container.SetCurrentState(currentTime, Atsc.IsPlaying);
            if (container.CurrentState != previousState) UpdateObject(lightingObject, container.CurrentState);
            lightingObject.UpdateTime(currentTime);
        }
    }

    private static void UpdateObject(LightingObject lightingObject, BasicLightState state) =>
        lightingObject.UpdateFromState(state);

    public void ToggleBoost(bool boost)
    {
        useBoost = boost;
        foreach (var lightingObject in ControllingLights)
        {
            lightingObject.UpdateBoostState(boost);
            if (!stateChunksContainerMap.TryGetValue(lightingObject, out var container)) continue;
            lightingObject.UpdateStartAndEndColor(
                GetStartColorFromState(lightingObject, container.CurrentState),
                GetEndColorFromState(lightingObject, container.CurrentState));
        }
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Gizmos.color = Color.red;
    //    if (GroupingMultiplier <= 0.1f) return;
    //    for (var i = -5; i < 150; i++)
    //    {
    //        var z = ((i - GroupingOffset) / GroupingMultiplier) + 0.5f;
    //        Gizmos.DrawLine(new Vector3(-50, 0, z), new Vector3(50, 0, z));
    //    }
    //}

    protected override BasicLightState CreateState(BaseEvent evt) => new(evt);

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertEvent(evt);
    }

    protected override void OnInsertUpdateToPreviousState(
        BasicLightState newState,
        BasicLightState previousState)
    {
        base.OnInsertUpdateToPreviousState(newState, previousState);
        if (!previousState.BaseEvent.IsFade && !previousState.BaseEvent.IsFlash)
            previousState.EndTimeAlpha = newState.StartTime;
        if (newState.BaseEvent.IsTransition
            && (previousState.BaseEvent.IsOff
                || previousState.BaseEvent.IsOn
                || previousState.BaseEvent.IsTransition))
        {
            if (previousState.BaseEvent.IsOff) previousState.StartColor = newState.StartColor;
            previousState.EndTimeColor = newState.StartTime;
            previousState.EndColor = newState.StartColor;
            previousState.EndChromaColor = newState.StartChromaColor;
            previousState.EndAlpha = newState.StartAlpha;
            previousState.Easing = Easing.Named(newState.BaseEvent.CustomEasing ?? "easeLinear");
            previousState.UseHSV = newState.BaseEvent.CustomLerpType == "HSV";
        }
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        BasicLightState newState,
        BasicLightState previousState,
        BasicLightState nextState)
    {
        if (newState.BaseEvent.IsOff && !nextState.BaseEvent.IsTransition)
            newState.StartColor = newState.EndColor = previousState.StartColor;
    }

    protected override void OnInsertUpdateFromNextState(
        BasicLightState newState,
        BasicLightState nextState)
    {
        base.OnInsertUpdateFromNextState(newState, nextState);
        if (!newState.BaseEvent.IsFade && !newState.BaseEvent.IsFlash) newState.EndTimeAlpha = nextState.StartTime;
        if (nextState.BaseEvent.IsTransition
            && (newState.BaseEvent.IsOff
                || newState.BaseEvent.IsOn
                || newState.BaseEvent.IsTransition))
        {
            if (newState.BaseEvent.IsOff) newState.StartColor = nextState.StartColor;
            newState.EndTimeColor = nextState.StartTime;
            newState.EndColor = nextState.StartColor;
            newState.EndChromaColor = nextState.StartChromaColor;
            newState.EndAlpha = nextState.StartAlpha;
            newState.Easing = Easing.Named(nextState.BaseEvent.CustomEasing ?? "easeLinear");
            newState.UseHSV = nextState.BaseEvent.CustomLerpType == "HSV";
        }
    }

    protected override void OnInsertUpdateToNextState(
        BasicLightState newState,
        BasicLightState nextState)
    {
        if (nextState.BaseEvent.IsOff) nextState.StartColor = nextState.EndColor = newState.StartColor;
    }

    private void UpdateExistingWithChromaLite(float time)
    {
        var fromIndex = chromaLiteDatas.FindLastIndex(cl => cl.BaseEvent.SongBpmTime <= time);
        var from = fromIndex != -1 && fromIndex < chromaLiteDatas.Count
            ? chromaLiteDatas[fromIndex]
            : new ChromaLiteData { BaseEvent = new BaseEvent { songBpmTime = float.MinValue } };

        var untilIndex = chromaLiteDatas.FindIndex(cl => cl.BaseEvent.SongBpmTime > time);
        var until = untilIndex != -1 ? chromaLiteDatas[untilIndex].BaseEvent.SongBpmTime : float.MaxValue;

        foreach (var enumerator in stateChunksContainerMap.Values.Select(container =>
            container.EnumerateFrom(from.BaseEvent.SongBpmTime)))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime >= until) break;
                if (state.BaseEvent.CustomColor == null) state.StartChromaColor = state.EndChromaColor = from.Color;
            }
        }
    }

    private void UpdateExistingWithChromaGradient(float time)
    {
        return; // TODO: properly handle this
        var fromIndex = chromaGradientDatas.FindLastIndex(cl => cl.StartTime <= time && time <= cl.EndTime);
        var from = fromIndex != -1 && fromIndex < chromaGradientDatas.Count
            ? chromaGradientDatas[fromIndex]
            : new ChromaGradientData { BaseEvent = new BaseEvent { songBpmTime = float.MinValue } };

        var untilIndex = chromaGradientDatas.FindIndex(cl => cl.StartTime > time);
        var until = untilIndex != -1 ? chromaGradientDatas[untilIndex].StartTime : float.MaxValue;

        foreach (var enumerator in stateChunksContainerMap.Values.Select(container =>
            container.EnumerateFrom(from.StartTime)))
        {
            while (enumerator.MoveNext())
            {
                var state = enumerator.Current;
                if (state!.StartTime > from.EndTime || state!.StartTime >= until) break;
                UpdateStateWithChromaGradient(state, from);
            }
        }
    }

    private void InsertWithChromaGradient(BasicLightState state)
    {
        var chromaGradientIndex =
            chromaGradientDatas.FindLastIndex(cg => cg.StartTime <= state.StartTime && state.StartTime <= cg.EndTime);
        if (chromaGradientIndex != -1) UpdateStateWithChromaGradient(state, chromaGradientDatas[chromaGradientIndex]);
    }

    private void UpdateStateWithChromaGradient(BasicLightState state, ChromaGradientData chromaGradientData)
    {
        state.StartTimeColor = chromaGradientData.StartTime;
        state.EndTimeColor = chromaGradientData.EndTime;
        state.StartChromaColor = chromaGradientData.StartColor;
        state.EndChromaColor = chromaGradientData.EndColor;
        state.Easing = chromaGradientData.Easing;
    }

    public override void InsertEvent(BaseEvent evt)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(
                        new() { BaseEvent = evt, Color = ColourManager.ColourFromInt(evt.Value) });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.BaseEvent.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteDatas.Add(new() { BaseEvent = evt, Color = null });
                    chromaLiteDatas = chromaLiteDatas.OrderBy(cl => cl.BaseEvent.SongBpmTime).ToList();
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return; // this was a break, not sure why
                }
        }

        //Check if it is a PogU new Chroma event
        if ((evt.CustomColor != null) && Settings.Instance.EmulateChromaLite && !evt.IsWhite) // White overrides Chroma
            chromaColor = (Color)evt.CustomColor;

        if (chromaLiteDatas.Count > 0)
        {
            var chromaLiteIndex = chromaLiteDatas.FindLastIndex(data => data.BaseEvent.SongBpmTime <= evt.SongBpmTime);
            if (chromaLiteIndex != -1 && Settings.Instance.EmulateChromaLite)
                chromaColor = chromaLiteDatas[chromaLiteIndex].Color;
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientDatas.Add(
                new ChromaGradientData
                {
                    BaseEvent = evt,
                    StartTime = evt.SongBpmTime,
                    EndTime =
                        evt.SongBpmTime
                        + evt.CustomLightGradient.Duration, // TODO: duration is not actual song bpm time
                    StartColor = evt.CustomLightGradient.StartColor,
                    EndColor = evt.CustomLightGradient.EndColor,
                    Easing = Easing.Named(evt.CustomLightGradient.EasingType)
                });
            chromaGradientDatas = chromaGradientDatas.OrderBy(cl => cl.StartTime).ToList();
            UpdateExistingWithChromaGradient(evt.SongBpmTime);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && evt.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        var affectedLights = ControllingLights;
        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingObject>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var newState = CreateState(evt);
            newState.StartTime = evt.SongBpmTime;
            newState.StartTimeColor = evt.SongBpmTime;
            newState.StartColor = InferColorFromEvent(evt);
            newState.StartChromaColor = chromaColor;
            newState.StartAlpha = evt.FloatValue;
            newState.EndTime = float.MaxValue;
            newState.EndTimeAlpha = float.MaxValue;
            newState.EndTimeColor = float.MaxValue;
            newState.EndColor = InferColorFromEvent(evt);
            newState.EndChromaColor = chromaColor;
            newState.EndAlpha = evt.FloatValue;
            newState.CanBeTurnedOff = lightingObject.CanBeTurnedOff;

            if (evt.IsOff)
            {
                if (lightingObject.CanBeTurnedOff)
                    newState.StartAlpha = newState.EndAlpha = 0f;
                else
                    newState.StartAlpha = newState.EndAlpha = GetNoTurnOffAlpha(evt.FloatValue);
            }
            else if (evt.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = evt.FloatValue;
                newState.Easing = Easing.Cubic.Out;
            }
            else if (evt.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = 0f;
                newState.Easing = Easing.Exponential.Out;
                if (!lightingObject.CanBeTurnedOff) newState.EndAlpha = GetNoTurnOffAlpha(evt.FloatValue);
            }

            InsertWithChromaGradient(newState);
            HandleInsertState(stateChunksContainerMap[lightingObject], newState);
        }
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    var data = chromaLiteDatas.Find(data => data.BaseEvent == evt);
                    chromaLiteDatas.Remove(data);
                    UpdateExistingWithChromaLite(evt.SongBpmTime);
                    return;
                }
        }

        IEnumerable<LightingObject> affectedLights = ControllingLights;

        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingObject>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingObject)) continue;
                filteredLights.Add(lightingObject);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingObject in affectedLights)
        {
            var container = stateChunksContainerMap[lightingObject];
            var state = HandleRemoveState(container, evt);
            if (container.CurrentState != state) continue;
            container.SetStateAt(evt.SongBpmTime);
            UpdateObject(lightingObject, container.CurrentState);
        }
    }

    // TODO: need to properly recalculate when (affected) chroma lite changed or removed
    protected override void
        OnRemoveUpdatePreviousAndNextState(
            BasicLightState currentState,
            BasicLightState previousState,
            BasicLightState nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currentState, previousState, nextState);
        if (nextState.BaseEvent.IsTransition
            && (previousState.BaseEvent.IsOff
                || previousState.BaseEvent.IsOn
                || previousState.BaseEvent.IsTransition))
        {
            previousState.EndTimeAlpha = nextState.StartTime;
            previousState.EndTimeColor = nextState.StartTimeColor;
            previousState.EndColor = nextState.StartColor;
            previousState.EndChromaColor = nextState.StartChromaColor;
            previousState.EndAlpha = nextState.StartAlpha;
            previousState.Easing = Easing.Named(nextState.BaseEvent.CustomEasing ?? "easeLinear");
            previousState.UseHSV = nextState.BaseEvent.CustomLerpType == "HSV";
        }
        else
        {
            previousState.EndTimeColor = nextState.StartTimeColor;
            previousState.EndColor = previousState.StartColor;
            previousState.EndChromaColor = previousState.StartChromaColor;

            if (!previousState.BaseEvent.IsFade && !previousState.BaseEvent.IsFlash)
            {
                previousState.EndTimeAlpha = nextState.StartTime;
                previousState.EndAlpha = previousState.StartAlpha;
            }

            if (previousState.BaseEvent.IsOff && !previousState.CanBeTurnedOff)
            {
                previousState.StartAlpha =
                    previousState.EndAlpha = GetNoTurnOffAlpha(previousState.BaseEvent.FloatValue);
            }

            if (nextState.BaseEvent.IsOff && !nextState.CanBeTurnedOff) nextState.StartColor = previousState.EndColor;
        }
    }


    public override void Reset()
    {
        foreach (var lightingObject in stateChunksContainerMap.Keys)
            UpdateObject(lightingObject, stateChunksContainerMap[lightingObject].CurrentState);
    }

    private static LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    public static Color GetStartColorFromState(LightingObject lightingObject, BasicLightState state) =>
        (state.StartChromaColor ?? GetColorFromScheme(state.StartColor, lightingObject.UseInvertedPlatformColors))
        .Multiply(HDRIntensity);

    public static Color GetEndColorFromState(LightingObject lightingObject, BasicLightState state) =>
        (state.EndChromaColor ?? GetColorFromScheme(state.EndColor, lightingObject.UseInvertedPlatformColors))
        .Multiply(HDRIntensity);

    private static Color GetColorFromScheme(LightColor value, bool useInvertedPlatformColors)
    {
        return value switch
        {
            LightColor.Blue when useInvertedPlatformColors => useBoost
                ? ColorScheme.RedBoostColor
                : ColorScheme.RedColor,
            LightColor.Blue => useBoost ? ColorScheme.BlueBoostColor : ColorScheme.BlueColor,
            LightColor.Red when useInvertedPlatformColors => useBoost
                ? ColorScheme.BlueBoostColor
                : ColorScheme.BlueColor,
            LightColor.Red => useBoost ? ColorScheme.RedBoostColor : ColorScheme.RedColor,
            LightColor.White => useBoost ? ColorScheme.WhiteBoostColor : ColorScheme.WhiteColor,
            _ => Color.white
        };
    }

    public struct ChromaLiteData : IEquatable<ChromaLiteData>
    {
        public BaseEvent BaseEvent;
        public Color? Color;

        public bool Equals(ChromaLiteData other) => Equals(BaseEvent, other.BaseEvent);
        public override bool Equals(object obj) => obj is ChromaLiteData other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(BaseEvent, Color);
    }

    public struct ChromaGradientData : IEquatable<ChromaGradientData>
    {
        public BaseEvent BaseEvent;
        public float StartTime;
        public float EndTime;
        public Color StartColor;
        public Color EndColor;
        public Func<float, float> Easing;

        public bool Equals(ChromaGradientData other) => Equals(BaseEvent, other.BaseEvent);
        public override bool Equals(object obj) => obj is ChromaGradientData other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(BaseEvent, StartTime, EndTime, StartColor, EndColor, Easing);
    }

    private static float GetNoTurnOffAlpha(float value) => value * 2f / 3f;

    [Serializable]
    public class LightGroup
    {
        public List<LightingObject> Lights = new();
    }
}

public class BasicLightState : BasicEventState
{
    public float StartTimeColor; // this is supposedly the same as start time, special case for chroma gradient
    public LightColor StartColor;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTimeAlpha; // similarly this match next start, otherwise used to interpolate flash/fade
    public float EndTimeColor; // also same case above, only special case for chroma gradient
    public LightColor EndColor;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing = global::Easing.Linear;
    public bool UseHSV;
    public bool CanBeTurnedOff = true;

    public BasicLightState(BaseEvent evt) : base(evt) { }
}
