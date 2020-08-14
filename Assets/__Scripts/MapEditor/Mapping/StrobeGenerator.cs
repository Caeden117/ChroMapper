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
        List<BeatmapObject> oldEvents = new List<BeatmapObject>(); //For the Action
        List<MapEvent> allChromaEvents = new List<MapEvent>(); //To remove conflicting objects
        //Order by type, then by descending time
        var groupings = containers.GroupBy(x => x._type);
        foreach (var group in groupings)
        {
            int type = group.Key;
            if (group.Count() >= 2)
            {
                IEnumerable<MapEvent> ordered = group.OrderByDescending(x => x._time);
                MapEvent end = ordered.First();
                MapEvent start = ordered.Last();

                if (start.IsUtilityEvent) continue;

                IEnumerable<MapEvent> containersBetween = eventsContainer.LoadedObjects.Where(x =>
                (x as MapEvent)._type == type && //Grab all events between start and end point.
                x._time >= start._time && x._time <= end._time
                ).Cast<MapEvent>();
                oldEvents.AddRange(containersBetween);

                IEnumerable<MapEvent> regularEventData = containersBetween.Where(x =>
                (x._customData is null || (!x._customData.HasKey("_color") && x._lightGradient == null)) && x._time >= start._time && x._time <= end._time);

                IEnumerable<MapEvent> chromaEvents = containersBetween.Where(x => x._customData?.HasKey("_color") ?? false || x._lightGradient != null);

                if (eventsContainer.PropagationEditing && type == eventsContainer.EventTypeToPropagate)
                {
                    var ringPropGroup = regularEventData.Append(end).Append(start).GroupBy(x => (x._customData?.HasKey("_propID") ?? false) ? (int)x._customData["_propID"] : -1);
                    for (int j = 0; j < ringPropGroup.Count(); j++)
                    {
                        IEnumerable<MapEvent> propEvents = ringPropGroup.ElementAt(j).OrderByDescending(x => x._time);
                        if (propEvents.Count() >= 2)
                        {
                            yield return StartCoroutine(GenerateRegularStrobe(type, values, propEvents.First()._time, propEvents.Last()._time, swapColors, dynamic, interval, propEvents, easing, j));
                        }
                    }
                }
                else
                {
                    if (regular) yield return StartCoroutine(GenerateRegularStrobe(type, values, end._time, start._time, swapColors, dynamic, interval, regularEventData, easing, null));
                    if (chroma && chromaEvents.Count() > 0) yield return StartCoroutine(GenerateChromaStrobe(chromaEvents, easing));
                }
                allChromaEvents.AddRange(chromaEvents);
            }
        }
        generatedObjects.OrderBy(x => x._time);
        //Delete conflicting vanilla events
        foreach (MapEvent e in oldEvents)
        {
            eventsContainer.DeleteObject(e, false, false);
        }
        //Spawn objects that were generated
        foreach (MapEvent data in generatedObjects)
        {
            eventsContainer.SpawnObject(data, false, false);
        }
        //Remove objects conflicting with chroma events
        eventsContainer.RemoveConflictingObjects(allChromaEvents);
        //The chroma events get removed, so we just add them back.
        foreach (MapEvent e in allChromaEvents.ToList())
        {
            eventsContainer.SpawnObject(e, false, false);
        }
        eventsContainer.RefreshPool(true);
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects.ToArray(), oldEvents.ToArray()));
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

        Func<float, float> easingFunc = Easing.named(easing);

        yield return new WaitForEndOfFrame(); //Again, this could literally be a void but whatever
        while (distanceInBeats >= 0)
        {
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
            typeIndex++;
            distanceInBeats -= 1 / (float)precision;
        }
    }

    private IEnumerator GenerateChromaStrobe(IEnumerable<MapEvent> containersBetween, string easing)
    {
        yield return new WaitForEndOfFrame(); //This could literally be a void but whatever
        IEnumerable<MapEvent> nonGradients = containersBetween.Where(x => x._lightGradient == null);
        for (int i = 0; i < nonGradients.Count() - 1; i++)
        {
            MapEvent currentChroma = nonGradients.ElementAt(i);
            MapEvent nextChroma = nonGradients.ElementAt(i + 1);

            MapEvent generated = BeatmapObject.GenerateCopy(currentChroma);
            generated._lightGradient = new MapEvent.ChromaGradient(
                currentChroma._customData["_color"], //Start color
                nextChroma._customData["_color"], //End color
                nextChroma._time - currentChroma._time, //Duration
                easing); //Duration
            generated._customData.Remove("_color");

            generatedObjects.Add(generated);
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
