using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public void GenerateStrobe(int valueA, int valueB, bool regular, bool chroma, bool dynamic, bool swapColors, int interval, int chromaOffset)
    {
        StartCoroutine(GenerateStrobeCoroutine(valueA, valueB, regular, chroma, dynamic, swapColors, interval, chromaOffset));
    }

    private IEnumerator GenerateStrobeCoroutine(int valueA, int valueB, bool regular, bool chroma, bool dynamic, bool swapColors, int interval, int chromaOffset)
    {
        generatedObjects.Clear();
        //yield return PersistentUI.Instance.FadeInLoadingScreen();
        IEnumerable<MapEvent> containers = SelectionController.SelectedObjects.Where(x => x is MapEvent).Cast<MapEvent>(); //Grab selected objects
        List<BeatmapObject> conflictingObjects = new List<BeatmapObject>(); //For the Action
        //Order by type, then by descending time
        containers = containers.OrderBy(x => x._type).ThenByDescending(x => x._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            if (containers.Count(x => x._type == i) >= 2)
            {
                IEnumerable<MapEvent> filteredContainers = containers.Where(x => x._type == i);
                MapEvent end = filteredContainers.First();
                MapEvent start = filteredContainers.Last();

                IEnumerable<MapEvent> containersBetween = eventsContainer.LoadedObjects.Where(x =>
                (x as MapEvent)._type == i && //Grab all events between start and end point.
                x._time >= start._time && x._time <= end._time
                ).Cast<MapEvent>();

                //TODO convert to Chroma 2.0 events
                IEnumerable<MapEvent> regularEventData = containersBetween.Where(x => x._customData is null || !x._customData.HasKey("_color"));

                IEnumerable<MapEvent> chromaEvents = containersBetween.Where(x => x._customData?.HasKey("_color") ?? false);

                if (regular) yield return StartCoroutine(GenerateRegularStrobe(i, valueA, valueB, end._time, start._time, swapColors, dynamic, interval, regularEventData));
                if (chroma && chromaEvents.Count() > 0) yield return StartCoroutine(GenerateChromaStrobe(i, end._time, start._time, interval, 1f / chromaOffset, chromaEvents));
            }
        }
        generatedObjects.OrderBy(x => x._time);
        SelectionController.RefreshMap();
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects = new HashSet<BeatmapObject>(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects, conflictingObjects));
        generatedObjects.Clear();
    }

    private IEnumerator GenerateRegularStrobe(int type, int ValueA, int ValueB, float endTime, float startTime, bool alternateColors, bool dynamic, int precision, IEnumerable<MapEvent> containersBetween)
    {
        List<int> alternatingTypes = new List<int>() { ValueA, ValueB };
        int typeIndex = 0;
        if (alternateColors)
        {
            if (ValueA > 4 && ValueA < 8) alternatingTypes.Add(ValueA - 4); //Adds inverse colors (Red -> Blue) for both values if we're alternating
            else if (ValueA > 0) alternatingTypes.Add(ValueA + 4);
            else alternatingTypes.Add(ValueA);
            if (ValueB > 4 && ValueB < 8) alternatingTypes.Add(ValueB - 4);
            else if (ValueB > 0) alternatingTypes.Add(ValueB + 4);
            else alternatingTypes.Add(ValueB);
        }
        alternatingTypes = alternatingTypes.Distinct().ToList();
        float distanceInBeats = endTime - startTime;
        float originalDistance = distanceInBeats;

        while (distanceInBeats >= 0)
        {
            yield return new WaitForEndOfFrame();
            if (typeIndex >= alternatingTypes.Count) typeIndex = 0;
            IEnumerable<MapEvent> any = containersBetween.Where(x => x._time <= endTime - distanceInBeats && x != containersBetween.First() && x != containersBetween.Last());
            if (any.Any() && dynamic)
            {
                alternatingTypes[0] = any.Last()._value;
                if (alternateColors && alternatingTypes.Count <= 4)
                {
                    if (alternatingTypes.Count == 1) alternatingTypes.Add(ValueB);
                    if (alternatingTypes.Count > 3)
                    {
                        if (alternatingTypes[0] > 4 && alternatingTypes[0] < 8) alternatingTypes[3] = alternatingTypes[0] - 4;
                        else if (alternatingTypes[0] > 0) alternatingTypes[3] = alternatingTypes[0] + 4;
                    }
                }
            }
            int value = alternatingTypes[typeIndex];
            typeIndex++;
            MapEvent data = new MapEvent(endTime - distanceInBeats, type, value);
            eventsContainer.SpawnObject(data);
            generatedObjects.Add(data);
            distanceInBeats -= 1 / (float)precision;
        }
    }

    private IEnumerator GenerateChromaStrobe(int type, float endTime, float startTime, int precision, float distanceOffset, IEnumerable<MapEvent> containersBetween)
    {
        Dictionary<float, Color> chromaColors = new Dictionary<float, Color>();
        foreach (MapEvent e in containersBetween)
        {
            if (e is null) continue;
            if (e == containersBetween.First()) chromaColors.Add(startTime - distanceOffset, e._customData["_color"]);
            if (!chromaColors.ContainsKey(e._time))
                chromaColors.Add(e._time, e._customData["_color"]);
        }
        float distanceInBeats = endTime - startTime;
        while (distanceInBeats >= 0)
        {
            yield return new WaitForEndOfFrame();
            float time = endTime - distanceInBeats;
            float latestPastChromaTime = FindLastChromaTime(time, chromaColors, out float nextChromaTime);
            int color = ColourManager.ColourToInt(chromaColors.First().Value);
            if (nextChromaTime != -1 && latestPastChromaTime != -1)
            {
                Color from = chromaColors[latestPastChromaTime];
                Color to = chromaColors[nextChromaTime];
                float distanceBetweenTimes = nextChromaTime - latestPastChromaTime;
                color = ColourManager.ColourToInt(Color.Lerp(from, to, (time - latestPastChromaTime) / distanceBetweenTimes));
            }
            else if (time >= chromaColors.Last().Key) color = ColourManager.ColourToInt(chromaColors.Last().Value);
            MapEvent chromaData = new MapEvent(time  - distanceOffset, type, color);
            eventsContainer.SpawnObject(chromaData, out _);
            generatedObjects.Add(chromaData);
            distanceInBeats -= 1 / (float)precision;
        }
    }

    private float FindLastChromaTime(float t, Dictionary<float, Color> colors, out float nextTime)
    {
        List<float> times = colors.Keys.OrderBy(x => x).ToList();
        float last = times.First();
        nextTime = -1;
        for (int i = 0; i < colors.Count; i++)
        {
            if (i + 1 >= colors.Count) break;
            if (times[i] <= t && times[i + 1] >= t)
            {
                last = times[i];
                nextTime = times[i + 1];
                break;
            }
        }
        return last;
    }
}
