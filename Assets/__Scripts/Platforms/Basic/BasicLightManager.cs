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

    private readonly List<(float time, Color? data)> chromaLiteColorTimes = new();
    private readonly List<ChromaGradientData> chromaGradientTimes = new();

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
            SetCurrentState(currentTime, Atsc.IsPlaying, container);
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

    protected override void UpdateToPreviousStateOnInsert(
        BasicLightState newState,
        BasicLightState previousState)
    {
        base.UpdateToPreviousStateOnInsert(newState, previousState);
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

    protected override void UpdateFromPreviousStateAndNextStateOnInsert(
        BasicLightState newState,
        BasicLightState previousState,
        BasicLightState nextState)
    {
        if (newState.BaseEvent.IsOff && !nextState.BaseEvent.IsTransition)
            newState.StartColor = newState.EndColor = previousState.StartColor;
    }

    protected override void UpdateFromNextStateOnInsert(
        BasicLightState newState,
        BasicLightState nextState)
    {
        base.UpdateFromNextStateOnInsert(newState, nextState);
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

    protected override void UpdateToNextStateOnInsert(
        BasicLightState newState,
        BasicLightState nextState)
    {
        if (nextState.BaseEvent.IsOff) nextState.StartColor = nextState.EndColor = newState.StartColor;
    }

    public override void InsertEvent(BaseEvent evt)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteColorTimes.Add((evt.SongBpmTime, ColourManager.ColourFromInt(evt.Value)));
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaLiteColorTimes.Add((evt.SongBpmTime, null));
                    break; // not return?
                }
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            chromaGradientTimes.Add(
                new ChromaGradientData
                {
                    StartTime = evt.SongBpmTime,
                    EndTime =
                        evt.SongBpmTime
                        + evt.CustomLightGradient.Duration, // TODO: duration is not actual song bpm time
                    StartColor = evt.CustomLightGradient.StartColor,
                    EndColor = evt.CustomLightGradient.EndColor,
                    Easing = Easing.Named(evt.CustomLightGradient.EasingType)
                });
        }

        //Check if it is a PogU new Chroma event
        if ((evt.CustomColor != null) && Settings.Instance.EmulateChromaLite && !evt.IsWhite) // White overrides Chroma
            chromaColor = (Color)evt.CustomColor;

        if (chromaLiteColorTimes.Count > 0)
        {
            var lastChromaColor = chromaLiteColorTimes[^1];
            if (lastChromaColor.data != null && Settings.Instance.EmulateChromaLite)
                chromaColor = lastChromaColor.data.Value;
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && evt.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

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

            if (chromaGradientTimes.Count > 0)
            {
                if (chromaGradientTimes.Any(cg => cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime))
                {
                    var chromaGradient =
                        chromaGradientTimes.Last(cg =>
                            cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime);
                    newState.StartTimeColor = chromaGradient.StartTime;
                    newState.EndTimeColor = chromaGradient.EndTime;
                    newState.StartChromaColor = chromaGradient.StartColor;
                    newState.EndChromaColor = chromaGradient.EndColor;
                    newState.Easing = chromaGradient.Easing;
                }
            }

            InsertState(newState, stateChunksContainerMap[lightingObject].Chunks);
        }
    }

    public override void RemoveEvent(BaseEvent evt)
    {
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
            var state = RemoveState(evt, container.Chunks);
            if (container.CurrentState != state) continue;
            SetStateAt(evt.SongBpmTime, container);
            UpdateObject(lightingObject, container.CurrentState);
        }
    }

    // TODO: need to properly recalculate when (affected) chroma lite changed or removed
    protected override void
        UpdatePreviousAndNextStateOnRemove(
            BasicLightState previousState,
            BasicLightState nextState,
            BasicLightState currentState)
    {
        base.UpdatePreviousAndNextStateOnRemove(previousState, nextState, currentState);
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
                previousState.StartAlpha =
                    previousState.EndAlpha = GetNoTurnOffAlpha(previousState.BaseEvent.FloatValue);

            if (nextState.BaseEvent.IsOff && !nextState.CanBeTurnedOff) nextState.StartColor = previousState.EndColor;
        }
    }


    public override void Reset()
    {
        foreach (var lightingObject in stateChunksContainerMap.Keys)
            UpdateObject(lightingObject, stateChunksContainerMap[lightingObject].CurrentState);
    }

    private LightColor InferColorFromEvent(BaseEvent evt) =>
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

    public struct ChromaGradientData
    {
        public float StartTime;
        public float EndTime;
        public Color StartColor;
        public Color EndColor;
        public Func<float, float> Easing;
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
