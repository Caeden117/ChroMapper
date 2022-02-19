using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

public class StrobeLightingPass : StrobeGeneratorPass
{
    private readonly bool alternateColors;
    private readonly bool dynamic;
    private readonly Func<float, float> easingFunc;
    private readonly float precision;
    private readonly IEnumerable<int> values;

    public StrobeLightingPass(IEnumerable<int> alternatingValues, bool switchColors, bool dynamicStrobe,
        float strobePrecision, string strobeEasing)
    {
        values = alternatingValues;
        alternateColors = switchColors;
        dynamic = dynamicStrobe;
        precision = strobePrecision;
        easingFunc = Easing.Named(strobeEasing);
    }

    public override bool IsEventValidForPass(MapEvent @event) => !@event.IsUtilityEvent && !@event.IsLegacyChromaEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type,
        EventsContainer.PropMode propMode, JSONNode propID)
    {
        var generatedObjects = new List<MapEvent>();

        var startTime = original.First().Time;
        var endTime = original.Last().Time;

        var alternatingTypes = new List<int>(values);
        var typeIndex = 0;
        if (alternateColors)
        {
            for (var i = 0; i < values.Count(); i++)
                alternatingTypes.Add(InvertColors(alternatingTypes[i]));
        }

        var distanceInBeats = endTime - startTime;
        var originalDistance = distanceInBeats;
        MapEvent lastPassed = null;

        while (distanceInBeats >= 0)
        {
            if (typeIndex >= alternatingTypes.Count) typeIndex = 0;

            var any = original.Where(x => x.Time <= endTime - distanceInBeats).LastOrDefault();
            if (any != lastPassed && dynamic && MapEvent.IsBlueEventFromValue(any.Value) !=
                MapEvent.IsBlueEventFromValue(alternatingTypes[typeIndex]))
            {
                lastPassed = any;
                for (var i = 0; i < alternatingTypes.Count; i++)
                    alternatingTypes[i] = InvertColors(alternatingTypes[i]);
            }

            var value = alternatingTypes[typeIndex];
            var progress = (originalDistance - distanceInBeats) / originalDistance;
            var newTime = (easingFunc(progress) * originalDistance) + startTime;
            var data = new MapEvent(newTime, type, value);
            if (propMode != EventsContainer.PropMode.Off)
            {
                data.CustomData = new JSONObject();
                data.CustomData.Add("_lightID", propID);
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
            MapEvent.LightValueBlueON => MapEvent.LightValueRedON,
            MapEvent.LightValueBlueFlash => MapEvent.LightValueRedFlash,
            MapEvent.LightValueBlueFade => MapEvent.LightValueRedFade,
            MapEvent.LightValueRedON => MapEvent.LightValueBlueON,
            MapEvent.LightValueRedFlash => MapEvent.LightValueBlueFlash,
            MapEvent.LightValueRedFade => MapEvent.LightValueBlueFade,
            _ => MapEvent.LightValueOff,
        };
    }
}
