using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
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

    public List<LightingObject> ControllingLights = new();
    public LightGroup[] LightsGroupedByZ = { };

    public List<RotatingLightsManagerBase> RotatingLights = new();

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    public Dictionary<int, LightingObject> LightIDMap;

    public readonly Dictionary<LightingObject, List<List<BasicLightState>>> StateChunksMap = new();
    public readonly Dictionary<LightingObject, BasicLightState> CurrentStateMap = new();
    public readonly List<(float time, Color? data)> ChromaLiteColorTimes = new();
    public readonly List<ChromaGradientData> ChromaGradientTimes = new();

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
            {
                // No, stop that. Enforcing Light ID breaks Glass Desert
                if (!e.OverrideLightGroup) ControllingLights.Add(e);
            }

            foreach (var e in GetComponentsInChildren<RotatingLightsManagerBase>())
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
        foreach (var lightingObject in ControllingLights)
        {
            var states = StateChunksMap[lightingObject];
            var currentIndex = CurrentStateMap[lightingObject];
            var currentState = GetCurrentState(currentTime, currentIndex, states);

            if (CurrentStateMap[lightingObject] != currentState)
            {
                UpdateObject(lightingObject, currentState);
                CurrentStateMap[lightingObject] = currentState;
            }

            lightingObject.UpdateTime(currentTime);
        }
    }

    private void UpdateObject(LightingObject lightingObject, BasicLightState state)
    {
        lightingObject.UpdateStartTimeAlpha(state.StartTime);
        lightingObject.UpdateStartTimeColor(state.StartTimeColor);
        lightingObject.UpdateStartAlpha(state.StartAlpha);
        lightingObject.UpdateStartColor(GetStartColorFromState(lightingObject, state));

        lightingObject.UpdateEndTimeAlpha(state.EndTimeAlpha);
        lightingObject.UpdateEndTimeColor(state.EndTimeColor);
        lightingObject.UpdateEndAlpha(state.EndAlpha);
        lightingObject.UpdateEndColor(GetEndColorFromState(lightingObject, state));

        lightingObject.UpdateUseHSV(state.UseHSV);
        lightingObject.UpdateEasing(state.Easing);
    }

    public void SetColors(PlatformColorScheme colorScheme) => ColorScheme = colorScheme;

    public void ToggleBoost(bool boost)
    {
        useBoost = boost;
        foreach (var lightingObject in ControllingLights)
        {
            lightingObject.UpdateBoostState(boost);
            if (!CurrentStateMap.TryGetValue(lightingObject, out var currentIndex)) continue;
            lightingObject.UpdateStartColor(
                GetStartColorFromState(lightingObject, CurrentStateMap[lightingObject]));
            lightingObject.UpdateEndColor(
                GetEndColorFromState(lightingObject, CurrentStateMap[lightingObject]));
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

    protected override BasicLightState CreateState(BaseEvent evt) =>
        new() { BaseEvent = evt, StartTime = float.MinValue, EndTime = float.MaxValue, Easing = Easing.Linear };

    public override void BuildFromEvents(IEnumerable<BaseEvent> events)
    {
        foreach (var lightingObject in ControllingLights.Where(lightingObject =>
            !StateChunksMap.ContainsKey(lightingObject)))
        {
            StateChunksMap[lightingObject] = new();
            InitializeStates(StateChunksMap[lightingObject]);
            CurrentStateMap[lightingObject] = GetStateAt(0, StateChunksMap[lightingObject]);
        }

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

            if (evt.IsOff)
            {
                if (lightingObject.CanBeTurnedOff)
                    newState.StartAlpha = newState.EndAlpha = 0f;
                // The game uses its floatValue but it's still dimmer than what an On would be
                // This factor is very quick eyeball probably not that accurate
                else
                    newState.StartAlpha = newState.EndAlpha = evt.FloatValue * (2f / 3f);
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
                if (!lightingObject.CanBeTurnedOff) newState.EndAlpha = evt.FloatValue * (2f / 3f);
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

            InsertState(newState, StateChunksMap[lightingObject]);
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
            var state = RemoveState(evt, StateChunksMap[lightingObject]);
            if (CurrentStateMap[lightingObject] == state)
                CurrentStateMap[lightingObject] = GetStateAt(evt.SongBpmTime, StateChunksMap[lightingObject]);
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
            previousState.EndTimeAlpha = nextState.StartTime;
            previousState.EndTimeColor = nextState.StartTimeColor;
            previousState.EndColor = previousState.StartColor;
            previousState.EndChromaColor = previousState.StartChromaColor;
            if (!previousState.BaseEvent.IsFade && !previousState.BaseEvent.IsFlash)
                previousState.EndAlpha = previousState.StartAlpha;
        }
    }


    public override void Reset()
    {
        foreach (var lightingObject in CurrentStateMap.Keys.ToArray())
            UpdateObject(lightingObject, CurrentStateMap[lightingObject]);
    }

    private LightColor InferColorFromEvent(BaseEvent evt) =>
        evt.IsBlue ? LightColor.Blue : evt.IsRed ? LightColor.Red : LightColor.White;

    private Color GetStartColorFromState(LightingObject lightingObject, BasicLightState state) =>
        (state.StartChromaColor ?? GetColorFromScheme(state.StartColor, lightingObject.UseInvertedPlatformColors))
        .Multiply(HDRIntensity);

    private Color GetEndColorFromState(LightingObject lightingObject, BasicLightState state) =>
        (state.EndChromaColor ?? GetColorFromScheme(state.EndColor, lightingObject.UseInvertedPlatformColors))
        .Multiply(HDRIntensity);

    private Color GetColorFromScheme(LightColor value, bool useInvertedPlatformColors)
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

    public Func<float, float> Easing;
    public bool UseHSV;
}
