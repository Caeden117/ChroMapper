using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public class BasicLightManager : BasicEventManager<BasicLightState>
{
    public PlatformColorScheme ColorScheme;
    private bool useBoost;

    [FormerlySerializedAs("disableCustomInitialization")]
    public bool DisableCustomInitialization;

    public static readonly float FadeTimeSecond = 1.2f;
    public static readonly float FlashTimeSecond = 0.5f;
    public static float FadeTimeBeat = FadeTimeSecond;
    public static float FlashTimeBeat = FlashTimeSecond;
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    public List<LightingEvent> ControllingLights = new();
    public LightGroup[] LightsGroupedByZ = { };
    public List<RotatingLightsBase> RotatingLights = new();

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    public Dictionary<int, LightingEvent> LightIDMap;

    public readonly Dictionary<LightingEvent, List<BasicLightState>> LightStatesMap = new();
    public readonly Dictionary<LightingEvent, int> CurrentIndexMap = new();
    public readonly List<(float time, Color? data)> ChromaLiteColorTimes = new();
    public readonly List<ChromaGradientData> ChromaGradientTimes = new();

    public struct ChromaGradientData
    {
        public float StartTime;
        public float EndTime;
        public Color StartColor;
        public Color EndColor;
        public Func<float, float> Easing;
    }

    private void Start() => LoadOldLightOrder();

    public void LoadOldLightOrder()
    {
        if (!DisableCustomInitialization)
        {
            foreach (var e in GetComponentsInChildren<LightingEvent>())
            {
                // No, stop that. Enforcing Light ID breaks Glass Desert
                if (!e.OverrideLightGroup) ControllingLights.Add(e);
            }

            foreach (var e in GetComponentsInChildren<RotatingLightsBase>())
            {
                if (!e.IsOverrideLightGroup()) RotatingLights.Add(e);
            }

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

    // public void LateUpdate()
    // {
    //     if (atsc.IsPlaying) UpdateTime(atsc.CurrentSongBpmTime);
    // }

    public override void UpdateTime(float currentTime)
    {
        foreach (var lightingEvent in ControllingLights)
        {
            var states = LightStatesMap[lightingEvent];
            var currentIndex = CurrentIndexMap[lightingEvent];
            var (index, currentState) = GetCurrentState(currentTime, currentIndex, states);

            if (index != currentIndex)
            {
                UpdateObject(lightingEvent, currentState);
                CurrentIndexMap[lightingEvent] = index;
            }

            lightingEvent.UpdateTime(currentTime);
        }
    }

    private void UpdateObject(LightingEvent lightingEvent, BasicLightState state)
    {
        lightingEvent.UpdateStartTimeAlpha(state.StartTimeAlpha);
        lightingEvent.UpdateStartTimeColor(state.StartTimeColor);
        lightingEvent.UpdateStartAlpha(state.StartAlpha);
        lightingEvent.UpdateStartColor(
            (state.StartChromaColor
                ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, state.StartValue))
            .Multiply(
                HDRIntensity));

        lightingEvent.UpdateEndTimeAlpha(state.EndTimeAlpha);
        lightingEvent.UpdateEndTimeColor(state.EndTimeColor);
        lightingEvent.UpdateEndAlpha(state.EndAlpha);
        lightingEvent.UpdateEndColor(
            (state.EndChromaColor
                ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, state.EndValue))
            .Multiply(
                HDRIntensity));

        lightingEvent.UpdateUseHSV(state.UseHSV);
        lightingEvent.UpdateEasing(state.Easing);
    }

    public void SetColors(PlatformColorScheme colorScheme) => ColorScheme = colorScheme;

    public void ToggleBoost(bool boost)
    {
        useBoost = boost;
        foreach (var lightingEvent in ControllingLights)
        {
            lightingEvent.UpdateBoostState(boost);
            if (!CurrentIndexMap.TryGetValue(lightingEvent, out var currentIndex)) continue;
            lightingEvent.UpdateStartColor(
                (
                    LightStatesMap[lightingEvent][currentIndex].StartChromaColor
                    ?? InferColorFromValue(
                        lightingEvent.UseInvertedPlatformColors,
                        LightStatesMap[lightingEvent][currentIndex].StartValue)).Multiply(
                    HDRIntensity));
            lightingEvent.UpdateEndColor(
                (
                    LightStatesMap[lightingEvent][currentIndex].EndChromaColor
                    ?? InferColorFromValue(
                        lightingEvent.UseInvertedPlatformColors,
                        LightStatesMap[lightingEvent][currentIndex].EndValue)).Multiply(
                    HDRIntensity));
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

    protected override BasicLightState InitializeState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue, Easing = Easing.Linear };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var lightingEvent in ControllingLights)
        {
            CurrentIndexMap[lightingEvent] = 0;
            LightStatesMap[lightingEvent] = new();
            InitializeStates(LightStatesMap[lightingEvent]);
        }

        foreach (var evt in events) InsertEvent(evt);
    }

    protected override void UpdateToPreviousStateOnInsert(
        ref BasicLightState newState,
        ref BasicLightState previousState)
    {
        base.UpdateToPreviousStateOnInsert(ref newState, ref previousState);
        if (newState.BaseEvent.IsTransition
            && (previousState.BaseEvent.IsOff
                || previousState.BaseEvent.IsOn
                || previousState.BaseEvent.IsTransition))
        {
            if (previousState.BaseEvent.IsOff) previousState.StartValue = newState.StartValue;
            previousState.EndTimeAlpha = newState.StartTime;
            previousState.EndTimeColor = newState.StartTime;
            previousState.EndValue = newState.StartValue;
            previousState.EndChromaColor = newState.StartChromaColor;
            previousState.EndAlpha = newState.StartAlpha;
            previousState.Easing = Easing.Named(newState.BaseEvent.CustomEasing ?? "easeLinear");
            previousState.UseHSV = newState.BaseEvent.CustomLerpType == "HSV";
        }
    }

    protected override void UpdateFromNextStateOnInsert(
        ref BasicLightState newState,
        ref BasicLightState nextState)
    {
        base.UpdateFromNextStateOnInsert(ref newState, ref nextState);
        if (nextState.BaseEvent.IsTransition
            && (newState.BaseEvent.IsOff
                || newState.BaseEvent.IsOn
                || newState.BaseEvent.IsTransition))
        {
            if (newState.BaseEvent.IsOff) newState.StartValue = nextState.StartValue;
            newState.EndTimeAlpha = nextState.StartTime;
            newState.EndTimeColor = nextState.StartTime;
            newState.EndValue = nextState.StartValue;
            newState.EndChromaColor = nextState.StartChromaColor;
            newState.EndAlpha = nextState.StartAlpha;
            newState.Easing = Easing.Named(nextState.BaseEvent.CustomEasing ?? "easeLinear");
            newState.UseHSV = nextState.BaseEvent.CustomLerpType == "HSV";
        }
    }

    public override void InsertEvent(BaseEvent evt)
    {
        Color? chromaColor = null;

        // Check if its a legacy Chroma RGB event
        switch (evt.Value)
        {
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    ChromaLiteColorTimes.Add((evt.SongBpmTime, ColourManager.ColourFromInt(evt.Value)));
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    ChromaLiteColorTimes.Add((evt.SongBpmTime, null));
                    break; // not return?
                }
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            ChromaGradientTimes.Add(
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

        if (ChromaLiteColorTimes.Count > 0)
        {
            var lastChromaColor = ChromaLiteColorTimes[^1];
            if (lastChromaColor.data != null && Settings.Instance.EmulateChromaLite)
                chromaColor = lastChromaColor.data.Value;
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && evt.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        IEnumerable<LightingEvent> affectedLights = ControllingLights;

        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingEvent>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingEvent)) continue;
                filteredLights.Add(lightingEvent);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingEvent in affectedLights)
        {
            var newState = new BasicLightState
            {
                BaseEvent = evt,
                StartTime = evt.SongBpmTime,
                StartTimeAlpha = evt.SongBpmTime,
                StartTimeColor = evt.SongBpmTime,
                StartValue = evt.Value,
                StartChromaColor = chromaColor,
                StartAlpha = evt.FloatValue,
                EndTime = float.MaxValue,
                EndTimeAlpha = float.MaxValue,
                EndTimeColor = float.MaxValue,
                EndValue = evt.Value,
                EndChromaColor = chromaColor,
                EndAlpha = evt.FloatValue,
                Easing = Easing.Linear
            };

            if (evt.IsOff)
            {
                if (lightingEvent.CanBeTurnedOff)
                    newState.StartAlpha = newState.EndAlpha = 0f;
                // The game uses its floatValue but it's still dimmer than what an On would be
                // This factor is very quick eyeball probably not that accurate
                else
                    newState.StartAlpha = newState.EndAlpha = evt.FloatValue * (2f / 3f);
            }
            else if (evt.IsFlash)
            {
                newState.EndTimeAlpha = newState.StartTime + FlashTimeBeat;
                newState.EndTimeColor = newState.StartTime + FlashTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = evt.FloatValue;
                newState.Easing = Easing.Cubic.Out;
            }
            else if (evt.IsFade)
            {
                newState.EndTimeAlpha = newState.StartTime + FadeTimeBeat;
                newState.EndTimeColor = newState.StartTime + FadeTimeBeat;
                newState.StartAlpha = evt.FloatValue * 1.2f;
                newState.EndAlpha = 0f;
                newState.Easing = Easing.Exponential.Out;
                if (!lightingEvent.CanBeTurnedOff) newState.EndAlpha = evt.FloatValue * (2f / 3f);
            }

            if (ChromaGradientTimes.Count > 0)
            {
                if (ChromaGradientTimes.Any(cg => cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime))
                {
                    var chromaGradient =
                        ChromaGradientTimes.Last(cg =>
                            cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime);
                    newState.StartTimeColor = chromaGradient.StartTime;
                    newState.EndTimeColor = chromaGradient.EndTime;
                    newState.StartChromaColor = chromaGradient.StartColor;
                    newState.EndChromaColor = chromaGradient.EndColor;
                    newState.Easing = chromaGradient.Easing;
                }
            }

            InsertState(newState, LightStatesMap[lightingEvent]);
        }
    }

    private Color InferColorFromValue(bool useInvertedPlatformColors, int value)
    {
        return value switch
        {
            <= 4 when !useInvertedPlatformColors => useBoost ? ColorScheme.BlueBoostColor : ColorScheme.BlueColor,
            <= 4 => useBoost ? ColorScheme.RedBoostColor : ColorScheme.RedColor,
            <= 8 when !useInvertedPlatformColors => useBoost ? ColorScheme.RedBoostColor : ColorScheme.RedColor,
            <= 8 => useBoost ? ColorScheme.BlueBoostColor : ColorScheme.BlueColor,
            <= 12 => useBoost ? ColorScheme.WhiteBoostColor : ColorScheme.WhiteColor,
            _ => Color.white
        };
    }

    public override void RemoveEvent(BaseEvent evt)
    {
        IEnumerable<LightingEvent> affectedLights = ControllingLights;

        if (evt.CustomLightID != null && LightIDMap != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingEvent>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (!LightIDMap.TryGetValue(lightID, out var lightingEvent)) continue;
                filteredLights.Add(lightingEvent);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingEvent in affectedLights)
        {
            var state = LightStatesMap[lightingEvent].Find(s => s.BaseEvent == evt);
            RemoveState(state, LightStatesMap[lightingEvent]);
        }
    }

    // TODO: need to properly recalculate
    protected override void
        UpdatePreviousAndNextStateOnRemove(
            ref BasicLightState previousState,
            ref BasicLightState nextState,
            ref BasicLightState currentState)
    {
        base.UpdatePreviousAndNextStateOnRemove(ref previousState, ref nextState, ref currentState);
        if (nextState.BaseEvent.IsTransition
            && (previousState.BaseEvent.IsOff
                || previousState.BaseEvent.IsOn
                || previousState.BaseEvent.IsTransition))
        {
            previousState.EndTimeAlpha = nextState.StartTime;
            previousState.EndTimeColor = nextState.StartTime;
            previousState.EndValue = nextState.StartValue;
            previousState.EndChromaColor = nextState.StartChromaColor;
            previousState.EndAlpha = nextState.StartAlpha;
            previousState.Easing = Easing.Named(nextState.BaseEvent.CustomEasing ?? "easeLinear");
            previousState.UseHSV = nextState.BaseEvent.CustomLerpType == "HSV";
        }
        else
        {
            previousState.EndTimeAlpha = nextState.StartTimeAlpha;
            previousState.EndTimeColor = nextState.StartTimeColor;
            previousState.EndValue = previousState.StartValue;
            previousState.StartChromaColor = previousState.StartChromaColor;
            previousState.EndAlpha = previousState.StartAlpha;
        }
    }


    public override void ResetState()
    {
        foreach (var lightingEvent in CurrentIndexMap.Keys.ToArray())
            UpdateObject(lightingEvent, LightStatesMap[lightingEvent][CurrentIndexMap[lightingEvent]]);
    }

    [Serializable]
    public class LightGroup
    {
        public List<LightingEvent> Lights = new();
    }
}

public struct BasicLightState : IBasicEventState
{
    public BaseEvent BaseEvent { get; set; }

    public float StartTime { get; set; }
    public float StartTimeAlpha; // used to interpolate flash/fade
    public float StartTimeColor; // special case for chroma gradient
    public int StartValue;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTime { get; set; }
    public float EndTimeAlpha;
    public float EndTimeColor;
    public int EndValue;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing;
    public bool UseHSV;
}
