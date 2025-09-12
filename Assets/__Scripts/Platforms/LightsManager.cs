using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;

public class LightsManager : MonoBehaviour
{
    public AudioTimeSyncController atsc;
    private int id = 0;

    public struct LightingData
    {
        public int Id;
        public float StartTime;
        public float StartAlpha;
        public int StartValue;
        public Color? StartChromaColor;
        public float EndTime; // this is not true end time, only used to interpolate flash/fade/chroma gradient
        public float EndAlpha;
        public int EndValue;
        public Color? EndChromaColor;
        public float EndTimeLocal; // this is to check for next event
        public Func<float, float> Easing;
        public bool UseHSV;

        // this is far cheaper to do
        public static bool operator ==(LightingData m1, LightingData m2) => m1.Id == m2.Id;
        public static bool operator !=(LightingData m1, LightingData m2) => m1.Id != m2.Id;
    }

    public int ID;
    public static readonly float FadeTime = 1.5f;
    public static readonly float FlashTime = 0.6f;
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);
    public static readonly float HDRFlashIntensity = Mathf.GammaToLinearSpace(3);

    [FormerlySerializedAs("disableCustomInitialization")]
    public bool DisableCustomInitialization;

    public List<LightingEvent> ControllingLights = new();
    public LightGroup[] LightsGroupedByZ = { };
    public List<RotatingLightsBase> RotatingLights = new();

    public float GroupingMultiplier = 1.0f;
    public float GroupingOffset = 0.001f;

    public Dictionary<int, int> LightIDPlacementMap;
    public Dictionary<int, int> LightIDPlacementMapReverse;
    public Dictionary<int, LightingEvent> LightIDMap;

    public readonly Dictionary<LightingEvent, (int index, LightingData data)> CurrentLightingDataMap = new();
    public readonly Dictionary<LightingEvent, List<LightingData>> LightingDataMap = new();

    public PlatformColors Colors;
    public bool UseBoost;

    private Color? chromaCustomColor;
    [CanBeNull] private PlatformDescriptor.Gradient chromaGradient;

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

    public void UpdateTime(float currentTime)
    {
        foreach (var lightingEvent in ControllingLights)
        {
            var (idx, lightingData) = CurrentLightingDataMap[lightingEvent];

            // do we bsearch this fella and next object for playback update
            if (lightingData.EndTimeLocal <= currentTime)
            {
                while (++idx < LightingDataMap[lightingEvent].Count)
                {
                    lightingData = LightingDataMap[lightingEvent][idx];
                    if (lightingData.EndTimeLocal > currentTime) break;
                }
            }
            else if (lightingData.StartTime > currentTime)
            {
                while (--idx >= 0)
                {
                    lightingData = LightingDataMap[lightingEvent][idx];
                    if (lightingData.StartTime <= currentTime) break;
                }
            }

            if (lightingData != CurrentLightingDataMap[lightingEvent].data)
            {
                lightingEvent.UpdateStartTime(lightingData.StartTime);
                lightingEvent.UpdateStartAlpha(lightingData.StartAlpha);
                lightingEvent.UpdateStartColor(
                    (lightingData.StartChromaColor
                        ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, lightingData.StartValue))
                    .Multiply(
                        HDRIntensity));

                lightingEvent.UpdateEndTime(lightingData.EndTime);
                lightingEvent.UpdateEndAlpha(lightingData.EndAlpha);
                lightingEvent.UpdateEndColor(
                    (lightingData.EndChromaColor
                        ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, lightingData.EndValue))
                    .Multiply(
                        HDRIntensity));

                lightingEvent.UpdateEasing(lightingData.Easing);
                CurrentLightingDataMap[lightingEvent] = (idx, lightingData);
            }

            lightingEvent.UpdateTime(currentTime);
        }
    }

    public void ChangeAlpha(float alpha, float time, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateEndAlpha(alpha);
    }

    public void ChangeMultiplierAlpha(float alpha, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateMultiplyAlpha(alpha);
    }

    public void ChangeColor(Color color, float time, IEnumerable<LightingEvent> lights)
    {
        foreach (var light in lights) light.UpdateEndColor(color * HDRIntensity);
    }

    public void SetColors(PlatformColors colors) => Colors = colors;

    public void ToggleBoost(bool boost)
    {
        UseBoost = boost;
        foreach (var lightingEvent in ControllingLights)
        {
            lightingEvent.UpdateBoostState(boost);
            if (!CurrentLightingDataMap.ContainsKey(lightingEvent)) continue;
            lightingEvent.UpdateStartColor(
                CurrentLightingDataMap[lightingEvent].data.StartChromaColor
                ?? InferColorFromValue(
                    lightingEvent.UseInvertedPlatformColors,
                    CurrentLightingDataMap[lightingEvent].data.StartValue));
            lightingEvent.UpdateEndColor(
                CurrentLightingDataMap[lightingEvent].data.EndChromaColor
                ?? InferColorFromValue(
                    lightingEvent.UseInvertedPlatformColors,
                    CurrentLightingDataMap[lightingEvent].data.EndValue));
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

    public void BuildLightingData(IEnumerable<BaseEvent> events)
    {
        foreach (var lightingEvent in ControllingLights) LightingDataMap[lightingEvent] = new List<LightingData>();
        foreach (var evt in events) GenerateLightingData(evt);
        foreach (var lightingEvent in ControllingLights)
        {
            if (LightingDataMap[lightingEvent].Count == 0 || LightingDataMap[lightingEvent][0].StartTime != 0f)
            {
                var data = new LightingData { Id = ID++, Easing = Easing.Linear };
                if (LightingDataMap[lightingEvent].Count > 0)
                {
                    data.EndTime = LightingDataMap[lightingEvent][0].StartTime;
                    data.EndTimeLocal = LightingDataMap[lightingEvent][0].StartTime;
                }
                else
                {
                    data.EndTime = float.MaxValue;
                    data.EndTimeLocal = float.MaxValue;
                }

                LightingDataMap[lightingEvent].Insert(0, data);
            }

            CurrentLightingDataMap[lightingEvent] = (0, LightingDataMap[lightingEvent][0]);
        }
        UpdateTime(atsc.CurrentSongBpmTime);
    }

    private void GenerateLightingData(BaseEvent evt)
    {
        Color? chromaColor = null;
        switch (evt.Value)
        {
            //Check if its a legacy Chroma RGB event
            case >= ColourManager.RgbintOffset when Settings.Instance.EmulateChromaLite:
                {
                    chromaCustomColor = ColourManager.ColourFromInt(evt.Value);
                    return;
                }
            case ColourManager.RGBReset when Settings.Instance.EmulateChromaLite:
                {
                    chromaCustomColor = null;
                    break;
                }
        }

        if (chromaGradient != null)
        {
            var gradientEvent = chromaGradient.GradientEvent;
            if (gradientEvent.CustomLightGradient.Duration + gradientEvent.JsonTime < evt.JsonTime
                || !Settings.Instance.EmulateChromaLite)
            {
                StopCoroutine(chromaGradient.Routine);
                chromaGradient = null;
                chromaCustomColor = null;
            }
        }

        if (evt.CustomLightGradient != null && Settings.Instance.EmulateChromaLite)
        {
            if (chromaGradient != null)
            {
                StopCoroutine(chromaGradient.Routine);
                chromaGradient = null;
            }

            var gradient =
                new PlatformDescriptor.Gradient { GradientEvent = evt, Routine = StartCoroutine(GradientRoutine(evt)) };

            // If the gradient is over already then null is returned due to coroutine never yielding
            if (gradient.Routine != null) chromaGradient = gradient;
        }

        //Check if it is a PogU new Chroma event
        // it was not pogu
        if ((evt.CustomColor != null) && Settings.Instance.EmulateChromaLite && !evt.IsWhite) // White overrides Chroma
        {
            chromaColor = (Color)evt.CustomColor;
            chromaCustomColor = null;
            if (chromaGradient != null)
            {
                StopCoroutine(chromaGradient.Routine);
                chromaGradient = null;
            }
        }

        if (chromaCustomColor != null && Settings.Instance.EmulateChromaLite)
        {
            chromaColor = chromaCustomColor.Value;
            ChangeMultiplierAlpha(chromaColor.Value.a, ControllingLights);
        }

        //Check to see if we're soloing any particular event
        // wtf is solo event
        // if (SoloAnEventType && evt.Type != SoloEventType) mainColor = invertedColor = Color.black.WithAlpha(0);

        IEnumerable<LightingEvent> affectedLights = ControllingLights;

        if (evt.CustomLightID != null && Settings.Instance.EmulateChromaAdvanced)
        {
            var lightIDArr = evt.CustomLightID;
            var filteredLights = new List<LightingEvent>(lightIDArr.Length);
            foreach (var lightID in lightIDArr)
            {
                if (LightIDMap == null) continue;
                if (!LightIDMap.TryGetValue(lightID, out var lightingEvent)) continue;
                if (lightingEvent != null) filteredLights.Add(lightingEvent);
            }

            affectedLights = filteredLights;
        }

        foreach (var lightingEvent in affectedLights)
        {
            var data = new LightingData
            {
                Id = ID++,
                StartTime = evt.SongBpmTime,
                StartAlpha = evt.FloatValue,
                StartValue = evt.Value,
                StartChromaColor = chromaColor,
                EndAlpha = evt.FloatValue,
                EndValue = evt.Value,
                EndChromaColor = chromaColor,
                Easing = Easing.Linear
            };

            var nextEvt = evt.Next;
            data.EndTime = nextEvt?.SongBpmTime ?? float.MaxValue;
            data.EndTimeLocal = nextEvt?.SongBpmTime ?? float.MaxValue;

            var floatValue = evt.FloatValue;

            switch (evt.Value)
            {
                case (int)LightValue.Off:
                    if (lightingEvent.CanBeTurnedOff)
                    {
                        data.StartAlpha = data.EndAlpha = 0;
                    }
                    else
                    {
                        // The game uses its floatValue but it's still dimmer than what an On would be
                        // This factor is very quick eyeball probably not that accurate
                        data.StartAlpha = data.EndAlpha = floatValue * (2f / 3f);
                    }

                    TrySetTransition(evt, ref data);
                    break;
                case (int)LightValue.BlueOn:
                case (int)LightValue.RedOn:
                case (int)LightValue.WhiteOn:
                case (int)LightValue.BlueTransition:
                case (int)LightValue.RedTransition:
                case (int)LightValue.WhiteTransition:
                    TrySetTransition(evt, ref data);
                    break;
                case (int)LightValue.BlueFlash:
                case (int)LightValue.RedFlash:
                case (int)LightValue.WhiteFlash:
                    data.EndTime = data.StartTime + FlashTime;
                    data.StartAlpha = floatValue * 1.2f;
                    data.EndAlpha = floatValue;
                    data.Easing = Easing.Cubic.Out;
                    break;
                case (int)LightValue.BlueFade:
                case (int)LightValue.RedFade:
                case (int)LightValue.WhiteFade:
                    data.EndTime = data.StartTime + FadeTime;
                    data.StartAlpha = floatValue * 1.2f;
                    data.EndAlpha = 0f;
                    data.EndChromaColor = chromaColor;
                    data.Easing = Easing.Exponential.Out;
                    if (lightingEvent.CanBeTurnedOff)
                    {
                        data.EndAlpha = 0;
                        // data.EndChromaColor = Color.black;
                    }
                    else
                        data.EndChromaColor = chromaColor;

                    break;
            }

            LightingDataMap[lightingEvent].Add(data);
        }
    }

    private void TrySetTransition(BaseEvent e, ref LightingData data)
    {
        if (!TryGetNextTransitionNote(e, out var transition)) return;
        var nextChromaColor = transition.CustomColor;
        if (e.IsWhite) // White overrides Chroma
            nextChromaColor = null;
        if (e.IsOff)
        {
            data.StartAlpha = 0;
            data.StartValue = transition.Value;
        }

        data.EndTime = transition.SongBpmTime;
        data.EndTimeLocal = transition.SongBpmTime;
        data.EndAlpha = transition.FloatValue;
        data.EndValue = transition.Value;
        data.EndChromaColor = nextChromaColor;
        data.Easing = Easing.ByName[e.CustomEasing ?? "easeLinear"];
    }

    private bool TryGetNextTransitionNote(in BaseEvent e, out BaseEvent transitionEvent)
    {
        transitionEvent = null;
        if (e.Next is not { IsTransition: true }) return false;
        transitionEvent = e.Next;
        return true;
    }

    private Color InferColorFromValue(bool useInvertedPlatformColors, int value)
    {
        return value switch
        {
            <= 4 when !useInvertedPlatformColors => UseBoost ? Colors.BlueBoostColor : Colors.BlueColor,
            <= 4 => UseBoost ? Colors.RedBoostColor : Colors.RedColor,
            <= 8 when !useInvertedPlatformColors => UseBoost ? Colors.RedBoostColor : Colors.RedColor,
            <= 8 => UseBoost ? Colors.BlueBoostColor : Colors.BlueColor,
            <= 12 => UseBoost ? Colors.WhiteBoostColor : Colors.WhiteColor,
            _ => Color.white
        };
    }

    private IEnumerator GradientRoutine(BaseEvent gradientEvent)
    {
        var gradient = gradientEvent.CustomLightGradient;
        var easingFunc = Easing.ByName[gradient.EasingType];

        // TODO: Proper Duration Scaling
        float progress;
        while (false)
        {
            var lerped = Color.LerpUnclamped(gradient.StartColor, gradient.EndColor, easingFunc(progress));
            // if (!SoloAnEventType || gradientEvent.Type == SoloEventType)
            {
                chromaCustomColor = lerped;
                ChangeColor(lerped.WithAlpha(1), 0, ControllingLights);
                ChangeMultiplierAlpha(lerped.a, ControllingLights);
            }

            yield return new WaitForEndOfFrame();
        }

        chromaCustomColor = gradient.EndColor;
        ChangeColor(chromaCustomColor.Value.WithAlpha(1), 0, ControllingLights);
        ChangeMultiplierAlpha(chromaCustomColor.Value.a, ControllingLights);
    }

    [Serializable]
    public class LightGroup
    {
        public List<LightingEvent> Lights = new List<LightingEvent>();
    }
}
