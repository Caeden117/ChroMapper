using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class StrobeGenerator : MonoBehaviour {

    private static readonly float MaxDistanceBetweenEventsInBeats = 10;

    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private StrobeGeneratorUIDropdown ui;
    private List<BeatmapObject> generatedObjects = new List<BeatmapObject>();

    public void ToggleUI()
    {
        ui.ToggleDropdown(!StrobeGeneratorUIDropdown.IsActive);
    }

    public void GenerateStrobe(IEnumerable<int> values, bool regular, bool chroma, bool dynamic, bool swapColors, int interval, string easing)
    {
        StartCoroutine(GenerateStrobeCoroutine(values, regular, chroma, dynamic, swapColors, interval, easing));
    }

    private IEnumerator GenerateStrobeCoroutine(IEnumerable<int> values, bool regular, bool chroma, bool dynamic, bool swapColors, int interval, string easing)
    {
        generatedObjects.Clear();
        //yield return PersistentUI.Instance.FadeInLoadingScreen();
        IEnumerable<MapEvent> containers = SelectionController.SelectedObjects.Where(x => x is MapEvent).Cast<MapEvent>(); //Grab selected objects
        List<BeatmapObject> conflictingObjects = new List<BeatmapObject>(); //For the Action
        //Order by type, then by descending time
        containers = containers.OrderBy(x => x._type).ThenByDescending(x => x._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            //Alright, time to disect.
            if (containers.Count(x => x._type == i) >= 2)
            {
                IEnumerable<MapEvent> filteredContainers = containers.Where(x => x._type == i);
                MapEvent end = filteredContainers.First();
                MapEvent start = filteredContainers.Last();

                if (start.IsUtilityEvent) continue;

                IEnumerable<MapEvent> containersBetween = eventsContainer.LoadedObjects.Where(x =>
                (x as MapEvent)._type == i && //Grab all events between start and end point.
                x._time >= start._time && x._time <= end._time
                ).Cast<MapEvent>();

                IEnumerable<MapEvent> regularEventData = containersBetween.Where(x => 
                (x._customData is null || !x._customData.HasKey("_color")) && x._time > start._time && x._time < end._time);
                conflictingObjects.AddRange(regularEventData);

                IEnumerable<MapEvent> chromaEvents = containersBetween.Where(x => x._customData?.HasKey("_color") ?? false);

                if (eventsContainer.PropagationEditing && i == eventsContainer.EventTypeToPropagate)
                {
                    for (int j = 0; j < eventsContainer.EventTypePropagationSize; j++)
                    {
                        IEnumerable<MapEvent> propEvents = regularEventData.Where(x => x._customData?.HasKey("_propID") ?? false && x._customData["_propID"] == j);
                        if (propEvents.Count() >= 2)
                        {
                            yield return StartCoroutine(GenerateRegularStrobe(i, values, propEvents.Last()._time, propEvents.First()._time, swapColors, dynamic, interval, new List<MapEvent>() { }, easing, j));
                        }
                    }
                }
                else
                {
                    if (regular) yield return StartCoroutine(GenerateRegularStrobe(i, values, end._time, start._time, swapColors, dynamic, interval, regularEventData, easing, null));
                    if (chroma && chromaEvents.Count() > 0) yield return StartCoroutine(GenerateChromaStrobe(chromaEvents, easing));
                }
            }
        }
        generatedObjects.OrderBy(x => x._time);
        foreach (MapEvent e in conflictingObjects)
        {
            eventsContainer.DeleteObject(e, false);
        }
        eventsContainer.RefreshPool();
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects, conflictingObjects));
        generatedObjects.Clear();
    }

    private IEnumerator GenerateRegularStrobe(int type, IEnumerable<int> values, float endTime, float startTime, bool alternateColors, bool dynamic, int precision, IEnumerable<MapEvent> containersBetween, string easing, int? propID)
    {
        List<int> alternatingTypes = new List<int>(values);
        int typeIndex = 0;
        if (alternateColors)
        {
            for (int i = 0; i < values.Count(); i++)
            {
                alternatingTypes.Add(InvertColors(alternatingTypes[i]));
            }
        }
        float distanceInBeats = endTime - startTime;
        float originalDistance = distanceInBeats;
        MapEvent lastPassed = null;
        List<MapEvent> generated = new List<MapEvent>();

        Func<float, float> easingFunc = Easing.named(easing);

        yield return new WaitForEndOfFrame(); //Again, this could literally be a void but whatever
        while (distanceInBeats >= 0)
        {
            if (distanceInBeats == endTime - startTime)
            {
                typeIndex++;
                distanceInBeats -= 1 / (float)precision;
            }
            if (typeIndex >= alternatingTypes.Count) typeIndex = 0;

            MapEvent any = containersBetween.LastOrDefault(x => x._time <= endTime - distanceInBeats);
            if (any != lastPassed && dynamic && (MapEvent.IsBlueEventFromValue(any._value) != MapEvent.IsBlueEventFromValue(alternatingTypes[typeIndex])))
            {
                lastPassed = any;
                for (int i = 0; i < alternatingTypes.Count; i++)
                {
                    alternatingTypes[i] = InvertColors(alternatingTypes[i]);
                }
            }

            int value = alternatingTypes[typeIndex];
            float progress = (originalDistance - distanceInBeats) / originalDistance;
            float newTime = (easingFunc(progress) * originalDistance) + startTime;
            MapEvent data = new MapEvent(newTime, type, value);
            if (propID != null)
            {
                data._customData = new JSONObject();
                data._customData.Add("_propID", propID.Value);
            }
            generatedObjects.Add(data);
            generated.Add(data);
            typeIndex++;
            distanceInBeats -= 1 / (float)precision;
        }
        eventsContainer.RemoveConflictingObjects(generated);
        foreach (MapEvent data in generated)
        {
            eventsContainer.SpawnObject(data, false, false);
        }
    }

    private IEnumerator GenerateChromaStrobe(IEnumerable<MapEvent> containersBetween, string easing)
    {
        yield return new WaitForEndOfFrame(); //This could literally be a void but whatever
        for (int i = 0; i < containersBetween.Count() - 1; i++)
        {
            MapEvent currentChroma = containersBetween.ElementAt(i);
            MapEvent nextChroma = containersBetween.ElementAt(i + 1);
            //currentChroma._value = 1; //Aeroluna please document your shit.
            //nextChroma._value = 1;
            currentChroma._lightGradient = new MapEvent.ChromaGradient(
                currentChroma._customData["_color"], //Start color
                nextChroma._customData["_color"], //End color
                nextChroma._time - currentChroma._time,
                easing); //Duration
            currentChroma._customData.Remove("_color");
        }
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
