using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StrobeGenerator : MonoBehaviour {

    private static readonly float MaxDistanceBetweenEventsInBeats = 10;

    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private StrobeGeneratorUIDropdown ui;
    private Button button;
    private List<BeatmapObjectContainer> generatedObjects = new List<BeatmapObjectContainer>();

	// Use this for initialization
	void Start () {
        button = GetComponent<Button>();
        SelectionController.ObjectWasSelectedEvent += ObjectSelected;
	}
	
	void ObjectSelected (BeatmapObjectContainer container) {
        bool enabled = false;
        List<BeatmapObjectContainer> containers = new List<BeatmapObjectContainer>(SelectionController.SelectedObjects); //Grab selected objects
        containers = containers.Where(x => x is BeatmapEventContainer).ToList(); //Filter Event containers
        //Order by type, then by descending time
        containers = containers.OrderBy(x => (x.objectData as MapEvent)._type).ThenByDescending(x => x.objectData._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            if (containers.Count(x => (x.objectData as MapEvent)._type == i) >= 2)
                enabled = true;
        }
        button.interactable = enabled;
        if (!enabled) ui.ToggleDropdown(false);
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectSelected;
    }

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
        List<BeatmapEventContainer> containers = SelectionController.SelectedObjects.Where(x => x is BeatmapEventContainer).Cast<BeatmapEventContainer>().ToList(); //Grab selected objects
        List<BeatmapObject> conflictingObjects = new List<BeatmapObject>(); //For the Action
        //Order by type, then by descending time
        containers = containers.OrderBy(x => x.eventData._type).ThenByDescending(x => x.eventData._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            if (containers.Count(x => (x.objectData as MapEvent)._type == i) >= 2)
            {
                List<BeatmapEventContainer> filteredContainers = containers.Where(x => x.eventData._type == i).ToList();
                BeatmapEventContainer end = filteredContainers.First();
                BeatmapEventContainer start = filteredContainers.Last();

                List<BeatmapEventContainer> containersBetween = eventsContainer.LoadedContainers.Where(x =>
                (x.objectData as MapEvent)._type == i && //Grab all events between start and end point.
                x.objectData._time >= start.objectData._time && x.objectData._time <= end.objectData._time
                ).OrderBy(x => x.objectData._time).Cast<BeatmapEventContainer>().ToList();

                List<MapEvent> regularEventData = containersBetween.Select(x => x.eventData).Where(x => x._value < ColourManager.RGB_INT_OFFSET).ToList();

                List<MapEvent> chromaEvents = new List<MapEvent>() { FindAttachedChromaEvent(start)?.eventData };
                chromaEvents.AddRange(containersBetween.Where(x => x.eventData._value >= ColourManager.RGB_INT_OFFSET).Select(x => x.eventData));
                chromaEvents = chromaEvents.Where(x => x != null).DistinctBy(x => x._time).ToList();

                conflictingObjects.AddRange(chromaEvents.Concat(regularEventData));

                foreach (BeatmapEventContainer e in containersBetween) eventsContainer.DeleteObject(e);

                if (regular) yield return StartCoroutine(GenerateRegularStrobe(i, valueA, valueB, end.eventData._time, start.eventData._time, swapColors, dynamic, interval, regularEventData));
                if (chroma && chromaEvents.Count > 0) yield return StartCoroutine(GenerateChromaStrobe(i, end.eventData._time, start.eventData._time, interval, 1f / chromaOffset, chromaEvents));
            }
        }
        generatedObjects.OrderBy(x => x.objectData._time);
        SelectionController.RefreshMap();
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects.AddRange(generatedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects, conflictingObjects));
        generatedObjects.Clear();
    }

    private BeatmapEventContainer FindAttachedChromaEvent(BeatmapEventContainer container)
    {
        return eventsContainer.LoadedContainers.Where((BeatmapObjectContainer x) =>
        (x.objectData as MapEvent)._type == container.eventData._type && //Ensure same type
        !(x.objectData as MapEvent).IsUtilityEvent && //And that they are not utility
        x.objectData._time <= container.eventData._time && //dont forget to make sure they're actually BEHIND a container.
        (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET //And they be a Chroma event.
        ).OrderByDescending(x => x.objectData._time).FirstOrDefault() as BeatmapEventContainer;
    }

    private IEnumerator GenerateRegularStrobe(int type, int ValueA, int ValueB, float endTime, float startTime, bool alternateColors, bool dynamic, int precision, List<MapEvent> containersBetween)
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
                    if (alternatingTypes[0] > 4 && alternatingTypes[0] < 8) alternatingTypes[3] = alternatingTypes[0] - 4;
                    else if (alternatingTypes[0] > 0) alternatingTypes[3] = alternatingTypes[0] + 4;
                }
            }
            int value = alternatingTypes[typeIndex];
            typeIndex++;
            MapEvent data = new MapEvent(endTime - distanceInBeats, type, value);
            BeatmapEventContainer eventContainer = eventsContainer.SpawnObject(data, out _, false) as BeatmapEventContainer;
            generatedObjects.Add(eventContainer);
            distanceInBeats -= 1 / (float)precision;
        }
    }

    private IEnumerator GenerateChromaStrobe(int type, float endTime, float startTime, int precision, float distanceOffset, List<MapEvent> containersBetween)
    {
        Dictionary<float, Color> chromaColors = new Dictionary<float, Color>();
        foreach (MapEvent e in containersBetween)
        {
            if (e is null) continue;
            if (e == containersBetween.First()) chromaColors.Add(startTime - distanceOffset, ColourManager.ColourFromInt(e._value));
            if (!chromaColors.ContainsKey(e._time))
                chromaColors.Add(e._time, ColourManager.ColourFromInt(e._value));
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
            BeatmapEventContainer chromaContainer = eventsContainer.SpawnObject(chromaData, out _) as BeatmapEventContainer;
            generatedObjects.Add(chromaContainer);
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
