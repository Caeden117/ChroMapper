using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

public class BasicLightManager : MonoBehaviour
{
    public AudioTimeSyncController atsc;
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

    public readonly Dictionary<LightingEvent, (int index, LightingData data)> CurrentLightingDataMap = new();
    public readonly Dictionary<LightingEvent, List<LightingData>> LightingDataMap = new();
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

    public void UpdateTime(float currentTime)
    {
        foreach (var lightingEvent in ControllingLights)
        {
            var (idx, lightingData) = CurrentLightingDataMap[lightingEvent];

            // swap out event
            // do we bsearch this fella and next object for playback update
            if (lightingData.EndTime <= currentTime)
            {
                while (++idx < LightingDataMap[lightingEvent].Count)
                {
                    lightingData = LightingDataMap[lightingEvent][idx];
                    if (lightingData.EndTime > currentTime) break;
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
                lightingEvent.UpdateStartTime(lightingData.StartTimeLocal);
                lightingEvent.UpdateStartAlpha(lightingData.StartAlpha);
                lightingEvent.UpdateStartColor(
                    (lightingData.StartChromaColor
                        ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, lightingData.StartValue))
                    .Multiply(
                        HDRIntensity));

                lightingEvent.UpdateEndTime(lightingData.EndTimeLocal);
                lightingEvent.UpdateEndAlpha(lightingData.EndAlpha);
                lightingEvent.UpdateEndColor(
                    (lightingData.EndChromaColor
                        ?? InferColorFromValue(lightingEvent.UseInvertedPlatformColors, lightingData.EndValue))
                    .Multiply(
                        HDRIntensity));

                lightingEvent.UpdateUseHSV(lightingData.UseHSV);
                lightingEvent.UpdateEasing(lightingData.Easing);
                CurrentLightingDataMap[lightingEvent] = (idx, lightingData);
            }

            lightingEvent.UpdateTime(currentTime);
        }
    }

    public void SetColors(PlatformColorScheme colorScheme) => ColorScheme = colorScheme;

    public void ToggleBoost(bool boost)
    {
        useBoost = boost;
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
        foreach (var evt in events) AddLightingData(evt);
        foreach (var lightingEvent in ControllingLights)
        {
            if (LightingDataMap[lightingEvent].Count == 0 || LightingDataMap[lightingEvent][0].StartTime != 0f)
            {
                var data = new LightingData { Easing = Easing.Linear };
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

    // only sequential for now
    private void AddLightingData(BaseEvent evt)
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
            var data = new LightingData
            {
                StartTime = evt.SongBpmTime,
                StartTimeLocal = evt.SongBpmTime,
                StartValue = evt.Value,
                StartChromaColor = chromaColor,
                StartAlpha = evt.FloatValue,
                EndTime = float.MaxValue,
                EndTimeLocal = float.MaxValue,
                EndValue = evt.Value,
                EndChromaColor = chromaColor,
                EndAlpha = evt.FloatValue,
                Easing = Easing.Linear
            };

            LightingData? previousData = null;
            if (LightingDataMap[lightingEvent].Count > 0)
            {
                previousData = LightingDataMap[lightingEvent][^1];
                var lightingData = previousData.Value;
                lightingData.EndTime = data.StartTime;
                previousData = lightingData;
            }

            switch (evt.Value)
            {
                case (int)LightValue.Off:
                    if (lightingEvent.CanBeTurnedOff)
                        data.StartAlpha = data.EndAlpha = 0f;
                    // The game uses its floatValue but it's still dimmer than what an On would be
                    // This factor is very quick eyeball probably not that accurate
                    else
                        data.StartAlpha = data.EndAlpha = evt.FloatValue * (2f / 3f);
                    break;
                case (int)LightValue.BlueOn:
                case (int)LightValue.RedOn:
                case (int)LightValue.WhiteOn:
                    break;
                case (int)LightValue.BlueTransition:
                case (int)LightValue.RedTransition:
                case (int)LightValue.WhiteTransition:
                    if (previousData != null)
                    {
                        var lightingData = previousData.Value;
                        if ((lightingData.StartValue % 4 == 0
                                || lightingData.StartValue % 4 == 1)
                            && lightingData.StartValue <= 12) // Is On or Transition (off is also included)
                        {
                            lightingData.EndTime = data.StartTime;
                            lightingData.EndTimeLocal = data.StartTime;
                            lightingData.EndValue = data.StartValue;
                            lightingData.EndChromaColor = chromaColor;
                            lightingData.EndAlpha = data.StartAlpha;
                            lightingData.Easing = Easing.Named(evt.CustomEasing ?? "easeLinear");
                            lightingData.UseHSV = evt.CustomLerpType == "HSV";
                        }

                        previousData = lightingData;
                    }

                    break;
                case (int)LightValue.BlueFlash:
                case (int)LightValue.RedFlash:
                case (int)LightValue.WhiteFlash:
                    data.EndTimeLocal = data.StartTime + FlashTimeBeat;
                    data.StartAlpha = evt.FloatValue * 1.2f;
                    data.EndAlpha = evt.FloatValue;
                    data.Easing = Easing.Cubic.Out;
                    break;
                case (int)LightValue.BlueFade:
                case (int)LightValue.RedFade:
                case (int)LightValue.WhiteFade:
                    data.EndTimeLocal = data.StartTime + FadeTimeBeat;
                    data.StartAlpha = evt.FloatValue * 1.2f;
                    data.EndAlpha = 0f;
                    data.Easing = Easing.Exponential.Out;
                    if (!lightingEvent.CanBeTurnedOff) data.EndAlpha = evt.FloatValue * (2f / 3f);

                    break;
            }

            if (ChromaGradientTimes.Count > 0)
            {
                if (ChromaGradientTimes.Any(cg => cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime))
                {
                    var chromaGradient =
                        ChromaGradientTimes.Last(cg =>
                            cg.StartTime <= evt.SongBpmTime && evt.SongBpmTime <= cg.EndTime);
                    data.StartTimeLocal = chromaGradient.StartTime;
                    data.EndTimeLocal = chromaGradient.EndTime;
                    data.StartChromaColor = chromaGradient.StartColor;
                    data.EndChromaColor = chromaGradient.EndColor;
                    data.Easing = chromaGradient.Easing;
                }
            }

            if (previousData != null) LightingDataMap[lightingEvent][^1] = previousData.Value;
            LightingDataMap[lightingEvent].Add(data);
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

    [Serializable]
    public class LightGroup
    {
        public List<LightingEvent> Lights = new List<LightingEvent>();
    }
}
