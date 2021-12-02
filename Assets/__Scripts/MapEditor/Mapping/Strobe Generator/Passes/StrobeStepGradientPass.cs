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

        var distanceInBeats = endTime - startTime;
        var originalDistance = distanceInBeats;

        if (colorPoints.Count < 2) return Enumerable.Empty<MapEvent>();

        var lastPoint = colorPoints.ElementAt(0);
        var nextPoint = colorPoints.ElementAt(1);

        while (distanceInBeats >= -0.01f)
        {
            var anyLast = colorPoints.Where(x => x.Key <= endTime - distanceInBeats).LastOrDefault();
            if (anyLast.Key != lastPoint.Key)
            {
                var nextPoints = colorPoints.Where(x => x.Key > endTime - distanceInBeats);

                // Don't progress if this is the last gradient
                if (nextPoints.Any())
                {
                    lastPoint = anyLast;
                    nextPoint = nextPoints.First();
                }
            }

            var progress = (originalDistance - distanceInBeats) / originalDistance;
            var newTime = (progress * originalDistance) + startTime;

            var lerp = easing(Mathf.InverseLerp(lastPoint.Key, nextPoint.Key, newTime));
            var color = Color.Lerp(lastPoint.Value, nextPoint.Value, lerp);

            var data = new MapEvent(newTime, type, value, new JSONObject());
            data.CustomData.Add("_color", color);
            if (propMode != EventsContainer.PropMode.Off)
            {
                if (value != MapEvent.LightValueBlueON && value != MapEvent.LightValueRedON &&
                    value != MapEvent.LightValueOff)
                {
                    data.Value = value < 5
                        ? MapEvent.LightValueBlueON
                        : MapEvent.LightValueRedON;
                }

                data.CustomData.Add("_lightID", propID);
            }

            generatedObjects.Add(data);

            distanceInBeats -= 1 / precision;

            if (alternateColors) value = InvertColors(value);
        }

        if (distanceInBeats < -0.01f)
        {
            var lastEvent = new MapEvent(endTime, type, value, new JSONObject());
            lastEvent.CustomData.Add("_color", colorPoints.OrderByDescending(x => x.Key).First().Value);
            
            if (propMode != EventsContainer.PropMode.Off)
            {
                if (value != MapEvent.LightValueBlueON && value != MapEvent.LightValueRedON &&
                    value != MapEvent.LightValueOff)
                {
                    lastEvent.Value = value < 5
                        ? MapEvent.LightValueBlueON
                        : MapEvent.LightValueRedON;
                }

                lastEvent.CustomData.Add("_lightID", propID);
            }

            generatedObjects.Add(lastEvent);
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
