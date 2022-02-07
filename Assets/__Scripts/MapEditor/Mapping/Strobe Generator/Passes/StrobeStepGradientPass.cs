using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public class StrobeStepGradientPass : StrobeGeneratorPass
{
    private readonly Func<float, float> easing;
    private readonly bool alternateColors;
    private readonly float precision;
    private int value;

    public StrobeStepGradientPass(int value, bool switchColors, float precision, Func<float, float> easing)
    {
        this.value = value;
        alternateColors = switchColors;
        this.precision = precision;
        this.easing = easing;
    }

    public override bool IsEventValidForPass(MapEvent @event) => !@event.IsUtilityEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type,
        EventsContainer.PropMode propMode, JSONNode propID)
    {
        var generatedObjects = new List<MapEvent>();

        var startTime = original.First().Time;
        var endTime = original.Last().Time;

        // Aggregate all colors points into a dictionary
        var colorPoints = new Dictionary<float, Color>();

        foreach (var e in original)
        {
            // Might as well be fancy and add support for Chroma 2.0 gradients
            if (e.LightGradient != null)
            {
                colorPoints.Add(e.Time, e.LightGradient.StartColor);
                colorPoints.Add(e.Time + e.LightGradient.Duration, e.LightGradient.EndColor);
            }
            else if (e.IsChromaEvent) // This already checks customData, so if this is true then customData exists.
            {
                colorPoints.Add(e.Time, e.CustomData["_color"]);
            }
            else if (e.Value == MapEvent.LightValueOff)
            {
                var lastColor = colorPoints.Where(x => x.Key < e.Time).LastOrDefault();

                colorPoints.Add(e.Time, !lastColor.Equals(default(KeyValuePair<float, Color>))
                    ? lastColor.Value.WithAlpha(0)
                    : new Color(0, 0, 0, 0));
            }
        }

        if (colorPoints.Count < 2) return Enumerable.Empty<MapEvent>();

        var distanceInBeats = endTime - startTime;

        var lastPoint = colorPoints.ElementAt(0);
        var nextPoint = colorPoints.ElementAt(1);

        // Because precision is still denominator, we simply multiply to get the steps needed
        // And to ensure we will always have enough steps, we will overestimate a little bit.
        var numberOfSteps = Mathf.CeilToInt(distanceInBeats * precision);

        // I'm getting tired of duplicate event issues so I'll do this all in one for loop.
        // Remove the jank.
        for (var i = 0; i < numberOfSteps + 1; i++)
        {
            var localDistance = Mathf.Clamp(i / precision, 0, distanceInBeats);
            var newTime = startTime + localDistance;

            var anyLast = colorPoints.Where(x => x.Key <= newTime).LastOrDefault();
            if (anyLast.Key != lastPoint.Key)
            {
                var nextPoints = colorPoints.Where(x => x.Key > newTime);

                // Don't progress if this is the last gradient
                if (nextPoints.Any())
                {
                    lastPoint = anyLast;
                    nextPoint = nextPoints.First();
                }
            }

            var lerp = easing(Mathf.InverseLerp(lastPoint.Key, nextPoint.Key, newTime));
            var color = Color.Lerp(lastPoint.Value, nextPoint.Value, lerp);

            var data = new MapEvent(newTime, type, value, new JSONObject());
            data.CustomData.Add("_color", color);

            if (propMode != EventsContainer.PropMode.Off)
            {
                data.CustomData.Add("_lightID", propID);
            }

            generatedObjects.Add(data);

            if (alternateColors) value = InvertColors(value);
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
