using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StrobeGenerator : MonoBehaviour {

    private static readonly float MaxDistanceBetweenEventsInBeats = 10;

    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventPlacement eventPreview;
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
        containers = containers.Where((BeatmapObjectContainer x) => (x is BeatmapEventContainer)).ToList(); //Filter Event containers
        //Order by type, then by descending time
        containers = containers.OrderBy(x => (x.objectData as MapEvent)._type).ThenByDescending(x => x.objectData._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            if (containers.Count(x => (x.objectData as MapEvent)._type == i) >= 2)
                enabled = true;
        }
        button.interactable = enabled;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectSelected;
    }

    public void GenerateStrobe()
    {
        StartCoroutine(GenerateStrobeCoroutine());
    }

    public IEnumerator GenerateStrobeCoroutine()
    {
        generatedObjects.Clear();
        if (atsc.gridMeasureSnapping >= 32)
            PersistentUI.Instance.DisplayMessage("This could take a while!", PersistentUI.DisplayMessageType.BOTTOM);
        //yield return PersistentUI.Instance.FadeInLoadingScreen();
        List<BeatmapObjectContainer> containers = SelectionController.SelectedObjects; //Grab selected objects
        List<BeatmapObjectContainer> notGeneratedObjects = new List<BeatmapObjectContainer>(); //For the Action
        containers = containers.Where((BeatmapObjectContainer x) => (x is BeatmapEventContainer)).ToList(); //Filter Event containers
        //Order by type, then by descending time
        containers = containers.OrderBy(x => (x.objectData as MapEvent)._type).ThenByDescending(x => x.objectData._time).ToList();
        for (var i = 0; i < 15; i++)
        {
            if (containers.Count(x => (x.objectData as MapEvent)._type == i) >= 2)
            {
                List<BeatmapObjectContainer> filteredContainers = containers.Where(x => (x.objectData as MapEvent)._type == i).ToList();
                BeatmapEventContainer end = filteredContainers.First() as BeatmapEventContainer;
                BeatmapEventContainer start = filteredContainers.Last() as BeatmapEventContainer;
                List<BeatmapObjectContainer> containersBetween = eventsContainer.LoadedContainers.Where(x =>
                (x.objectData as MapEvent)._type == i && //Grab all events between start and end point.
                x.objectData._time >= start.objectData._time && x.objectData._time <= end.objectData._time
                ).OrderBy(x => x.objectData._time).ToList();
                containersBetween.Add(FindAttachedChromaEvent(start)); //Add the first Chroma RGB event so gradients can work
                notGeneratedObjects.Add(filteredContainers.First()); //for the love of god please work
                notGeneratedObjects.AddRange(containersBetween); //Add this to our list of objects that are here from the start.
                yield return StartCoroutine(GenerateOneStrobe(start.eventData._type, eventPreview.queuedData._value,
                        end.objectData._time, start.objectData._time, containersBetween));
            }
        }
        generatedObjects.OrderBy(x => x.objectData._time);
        generatedObjects.RemoveAll(x => notGeneratedObjects.Contains(x));
        SelectionController.RefreshMap();
        //yield return PersistentUI.Instance.FadeOutLoadingScreen();
        SelectionController.DeselectAll();
        SelectionController.SelectedObjects.AddRange(generatedObjects);
        SelectionController.SelectedObjects.AddRange(notGeneratedObjects);
        SelectionController.RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new StrobeGeneratorGenerationAction(generatedObjects, notGeneratedObjects));
        generatedObjects.Clear();
    }

    private BeatmapEventContainer FindAttachedChromaEvent(BeatmapEventContainer container)
    {
        return eventsContainer.LoadedContainers.Where((BeatmapObjectContainer x) =>
        (x.objectData as MapEvent)._type == container.eventData._type && //Ensure same type
        !(x.objectData as MapEvent).IsUtilityEvent() && //And that they are not utility
        x.objectData._time >= container.eventData._time - (1f / 16f) && //They are close enough behind said container
        x.objectData._time <= container.eventData._time && //dont forget to make sure they're actually BEHIND a container.
        (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET //And they be a Chroma event.
        ).FirstOrDefault() as BeatmapEventContainer;
    }

    private IEnumerator GenerateOneStrobe(int type, int RepeatingValue, float endTime, float startTime, List<BeatmapObjectContainer> containersBetween)
    {
        bool alternateValue = false;
        float distanceInBeats = endTime - startTime;
        float originalDistance = distanceInBeats;

        int alternateValueType = MapEvent.LIGHT_VALUE_OFF;
        if (RepeatingValue == MapEvent.LIGHT_VALUE_RED_FADE || RepeatingValue == MapEvent.LIGHT_VALUE_RED_FLASH || RepeatingValue == MapEvent.LIGHT_VALUE_RED_ON)
            alternateValueType = MapEvent.LIGHT_VALUE_RED_ON;
        else alternateValueType = MapEvent.LIGHT_VALUE_BLUE_ON;

        Dictionary<float, Color> chromaColors = new Dictionary<float, Color>();
        Dictionary<float, int> eventValues = new Dictionary<float, int>();
        eventValues.Add(startTime, RepeatingValue);
        foreach(BeatmapObjectContainer container in containersBetween)
        {
            BeatmapEventContainer e = (container as BeatmapEventContainer);
            if (e.eventData._value < ColourManager.RGB_INT_OFFSET)
            {
                if (e.eventData._value != eventValues.Last().Value)
                {
                    if (!eventValues.ContainsKey(e.eventData._time))
                        eventValues.Add(e.eventData._time, e.eventData._value);
                }
            }
            else
            {
                if (!chromaColors.ContainsKey(e.eventData._time))
                    chromaColors.Add(e.eventData._time, ColourManager.ColourFromInt(e.eventData._value));
            }
        }

        while (distanceInBeats >= (1 / (float)atsc.gridMeasureSnapping))
        {
            yield return new WaitForEndOfFrame();
            distanceInBeats -= (1 / (float)atsc.gridMeasureSnapping);
            float latestPastValueTime = eventValues.Keys.Aggregate((x, y) => x >= (endTime - distanceInBeats) &&
            y <= (endTime - distanceInBeats) ? x : startTime);
            int value = alternateValue ? alternateValueType : eventValues[latestPastValueTime];
            if (distanceInBeats == 0)
                value = eventValues.Last().Value;
            MapEvent data = new MapEvent(endTime - distanceInBeats, type, value);
            if (alternateValueType != MapEvent.LIGHT_VALUE_OFF && eventValues[latestPastValueTime] != MapEvent.LIGHT_VALUE_OFF)
            {
                BeatmapEventContainer eventContainer = eventsContainer.SpawnObject(data) as BeatmapEventContainer;
                generatedObjects.Add(eventContainer);
            }
            if (chromaColors.Count >= 2 && distanceInBeats >= (1 / (float)atsc.gridMeasureSnapping))
            {
                float nextChromaTime;
                float latestPastChromaTime = FindLastChromaTime(data._time, chromaColors, out nextChromaTime);
                int color = -1;
                if (nextChromaTime != -1 && latestPastChromaTime != -1)
                {
                    Color from = chromaColors[latestPastChromaTime];
                    Color to = chromaColors[nextChromaTime];
                    float distanceBetweenTimes = nextChromaTime - latestPastChromaTime;
                    color = ColourManager.ColourToInt(Color.Lerp(from, to, (data._time - latestPastChromaTime) / (distanceBetweenTimes)));
                }
                else if (latestPastChromaTime != -1 && nextChromaTime == -1)
                    color = ColourManager.ColourToInt(chromaColors[latestPastChromaTime]);
                else continue;
                MapEvent chromaData = new MapEvent(data._time - (1f / 64f), type, color);
                BeatmapEventContainer chromaContainer = eventsContainer.SpawnObject(chromaData) as BeatmapEventContainer;
                generatedObjects.Add(chromaContainer);
            }
            alternateValue = !alternateValue;
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
