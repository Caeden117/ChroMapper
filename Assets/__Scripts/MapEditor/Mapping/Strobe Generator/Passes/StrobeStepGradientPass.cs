using System;
using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StrobeStepGradientPass : StrobeGeneratorPass
{
    private int value;
    private bool alternateColors;
    private float precision;
    private Func<float, float> _easing;

    public StrobeStepGradientPass(int value, bool switchColors, float precision, Func<float, float> easing)
    {
        this.value = value;
        alternateColors = switchColors;
        this.precision = precision;
        _easing = easing;
    }

    public override bool IsEventValidForPass(MapEvent @event) => !@event.IsUtilityEvent;

    public override IEnumerable<MapEvent> StrobePassForLane(IEnumerable<MapEvent> original, int type, EventsContainer.PropMode propMode, JSONNode propID)
    {
        List<MapEvent> generatedObjects = new List<MapEvent>();

        float startTime = original.First()._time;
        float endTime = original.Last()._time;

        // Aggregate all colors points into a dictionary
        Dictionary<float, Color> colorPoints = new Dictionary<float, Color>();

        foreach(MapEvent e in original)
        {
            // Might as well be fancy and add support for Chroma 2.0 gradients
            if (e._lightGradient != null)
            {
                colorPoints.Add(e._time, e._lightGradient.StartColor);
                colorPoints.Add(e._time + e._lightGradient.Duration, e._lightGradient.EndColor);
            }
            else if (e.IsChromaEvent) // This already checks customData, so if this is true then customData exists.
            {
                colorPoints.Add(e._time, e._customData["_color"]);
            }
        }

        float distanceInBeats = endTime - startTime;
        float originalDistance = distanceInBeats;

        if (colorPoints.Count < 2)
        {
            return Enumerable.Empty<MapEvent>();
        }

        KeyValuePair<float, Color> lastPoint = colorPoints.ElementAt(0);
        KeyValuePair<float, Color> nextPoint = colorPoints.ElementAt(1);

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

            float progress = (originalDistance - distanceInBeats) / originalDistance;
            float newTime = (progress * originalDistance) + startTime;

            var lerp = _easing(Mathf.InverseLerp(lastPoint.Key, nextPoint.Key, newTime));
            var color = Color.Lerp(lastPoint.Value, nextPoint.Value, lerp);

            MapEvent data = new MapEvent(newTime, type, value, new JSONObject());
            data._customData.Add("_color", color);
            if (propMode != EventsContainer.PropMode.Off)
            {
                if (value != MapEvent.LIGHT_VALUE_BLUE_ON && value != MapEvent.LIGHT_VALUE_RED_ON && value != MapEvent.LIGHT_VALUE_OFF)
                {
                    data._value = value < 5
                        ? MapEvent.LIGHT_VALUE_BLUE_ON
                        : MapEvent.LIGHT_VALUE_RED_ON;
                }

                data._customData.Add("_lightID", propID);
            }

            generatedObjects.Add(data);

            distanceInBeats -= 1 / precision;

            if (alternateColors)
            {
                value = InvertColors(value);
            }
        }

        return generatedObjects;
    }

    private int InvertColors(int colorValue)
    {
        switch (colorValue)
        {
            case MapEvent.LIGHT_VALUE_BLUE_ON: return MapEvent.LIGHT_VALUE_RED_ON;
            case MapEvent.LIGHT_VALUE_BLUE_FLASH: return MapEvent.LIGHT_VALUE_RED_FLASH;
            case MapEvent.LIGHT_VALUE_BLUE_FADE: return MapEvent.LIGHT_VALUE_RED_FADE;
            case MapEvent.LIGHT_VALUE_RED_ON: return MapEvent.LIGHT_VALUE_BLUE_ON;
            case MapEvent.LIGHT_VALUE_RED_FLASH: return MapEvent.LIGHT_VALUE_BLUE_FLASH;
            case MapEvent.LIGHT_VALUE_RED_FADE: return MapEvent.LIGHT_VALUE_BLUE_FADE;
            default: return MapEvent.LIGHT_VALUE_OFF;
        }
    }
}
