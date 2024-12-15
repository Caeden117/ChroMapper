using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;

public class StrobeLightingPass : StrobeGeneratorPass
{
    private readonly bool alternateColors;
    private readonly bool dynamic;
    private readonly Func<float, float> easingFunc;
    private readonly float precision;
    private readonly IEnumerable<int> values;
    private readonly bool easeTime;
    private readonly bool easeValue;

    public StrobeLightingPass(IEnumerable<int> alternatingValues, bool switchColors, bool dynamicStrobe,
        float strobePrecision, string strobeEasing, bool easingTimeSwitch, bool easingValueSwitch)
    {
        values = alternatingValues;
        alternateColors = switchColors;
        dynamic = dynamicStrobe;
        precision = strobePrecision;
        easingFunc = Easing.Named(strobeEasing);
        easeTime = easingTimeSwitch;
        easeValue = easingValueSwitch;
    }

    public override bool IsEventValidForPass(BaseEvent @event) => !@event.IsUtilityEvent() && !@event.IsLegacyChroma;

    public override IEnumerable<BaseEvent> StrobePassForLane(IEnumerable<BaseEvent> original, int type,
        EventGridContainer.PropMode propMode, int[] propID)
    {
        var generatedObjects = new List<BaseEvent>();

        var startTime = original.First().JsonTime;
        var endTime = original.Last().JsonTime;

        var startFloatValue = original.First().FloatValue;
        var endFloatValue = original.Last().FloatValue;
        var floatValueDiff = endFloatValue - startFloatValue;

        var alternatingTypes = new List<int>(values);
        var typeIndex = 0;
        if (alternateColors)
        {
            for (var i = 0; i < values.Count(); i++)
                alternatingTypes.Add(InvertColors(alternatingTypes[i]));
        }

        var distanceInBeats = endTime - startTime;
        var originalDistance = distanceInBeats;
        BaseEvent lastPassed = null;

        while (distanceInBeats >= 0)
        {
            if (typeIndex >= alternatingTypes.Count) typeIndex = 0;

            var any = original.Where(x => x.JsonTime <= endTime - distanceInBeats).LastOrDefault();
            if (any != lastPassed && dynamic && LightEventHelper.IsBlueFromValue(any.Value) !=
                LightEventHelper.IsBlueFromValue(alternatingTypes[typeIndex]))
            {
                lastPassed = any;
                for (var i = 0; i < alternatingTypes.Count; i++)
                    alternatingTypes[i] = InvertColors(alternatingTypes[i]);
            }

            var value = alternatingTypes[typeIndex];
            var progress = (originalDistance - distanceInBeats) / originalDistance;

            var newTime = easeTime
                ? (easingFunc(progress) * originalDistance) + startTime
                : (progress * originalDistance) + startTime;

            var newFloatValue = easeValue
                ? (easingFunc(progress) * floatValueDiff) + startFloatValue
                : (progress * floatValueDiff) + startFloatValue;

            var data = new BaseEvent { JsonTime = newTime, Type = type, Value = value, FloatValue = newFloatValue };
            if (propMode != EventGridContainer.PropMode.Off)
            {
                data.CustomLightID = propID;
            }

            generatedObjects.Add(data);
            typeIndex++;

            if (distanceInBeats > 0 && (distanceInBeats -= 1 / precision) < -0.001f)
                distanceInBeats = 0;
            else if (distanceInBeats <= 0) break;
        }

        return generatedObjects;
    }

    private int InvertColors(int colorValue)
    {
        return colorValue switch
        {
            (int)LightValue.BlueOn => (int)LightValue.RedOn,
            (int)LightValue.BlueFlash => (int)LightValue.RedFlash,
            (int)LightValue.BlueFade => (int)LightValue.RedFade,
            (int)LightValue.BlueTransition => (int)LightValue.RedTransition,
            (int)LightValue.RedOn => (int)LightValue.BlueOn,
            (int)LightValue.RedFlash => (int)LightValue.BlueFlash,
            (int)LightValue.RedFade => (int)LightValue.BlueFade,
            (int)LightValue.RedTransition => (int)LightValue.BlueTransition,
            (int)LightValue.WhiteOn => (int)LightValue.WhiteOn,
            (int)LightValue.WhiteFlash => (int)LightValue.WhiteFlash,
            (int)LightValue.WhiteFade => (int)LightValue.WhiteFade,
            (int)LightValue.WhiteTransition => (int)LightValue.WhiteTransition,
            _ => (int)LightValue.Off,
        };
    }
}
