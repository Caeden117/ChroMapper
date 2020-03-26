using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    private HashSet<int> eventTypesHash = new HashSet<int>();
    private List<BeatmapEventContainer> lastEvents = new List<BeatmapEventContainer>();
    private List<BeatmapEventContainer> lastChromaEvents = new List<BeatmapEventContainer>();

    private List<int> FilteredEventTypes = new List<int> { MapEvent.EVENT_TYPE_RINGS_ZOOM,
        MapEvent.EVENT_TYPE_RINGS_ROTATE, MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED, MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED};

    void Awake()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
        atsc.OnPlayToggle += PlayToggle;
    }

    private void PlatformLoaded(PlatformDescriptor platform)
    {
        descriptor = platform;
    }

    private void PlayToggle(bool isPlaying)
    {
        eventTypesHash.Clear();
        lastEvents.Clear();
        lastChromaEvents.Clear();
        if (isPlaying)
        {
            BeatmapEventContainer[] allEvents = events.LoadedContainers.Cast<BeatmapEventContainer>().ToArray();
            foreach(BeatmapEventContainer e in allEvents)
            {
                if (!e.eventData.IsChromaEvent && e.eventData._time <= atsc.CurrentBeat && eventTypesHash.Add(e.eventData._type))
                {
                    lastEvents.Add(e);
                }
            }

            foreach (BeatmapEventContainer e in allEvents)
            {
                if (e.eventData.IsChromaEvent && e.eventData._time <= atsc.CurrentBeat && eventTypesHash.Add(e.eventData._type))
                {
                    lastEvents.Add(e);
                }
            }
            for (int i = 0; i < 15; i++)
            {
                if (!eventTypesHash.Contains(i)) continue;
                //Grab all the events of the type, and that are behind current beat
                BeatmapEventContainer regular = lastEvents.Find(x => x.eventData._type == i);
                BeatmapEventContainer chroma = lastChromaEvents.Find(x => x.eventData._type == i);

                MapEvent regularData = regular?.eventData ?? null;
                MapEvent chromaData = chroma?.eventData ?? null;

                if (regular is null)
                {
                    if (i == MapEvent.EVENT_TYPE_RINGS_ZOOM || i == MapEvent.EVENT_TYPE_RINGS_ROTATE) continue;
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0));
                    continue;
                }

                //Past the last event, or an Off event if theres none, it is a ring event, or if there is a fade
                if (regularData._value != MapEvent.LIGHT_VALUE_BLUE_FADE && regularData._value != MapEvent.LIGHT_VALUE_RED_FADE &&
                    !regularData.IsRingEvent) 
                    descriptor.EventPassed(false, 0, regularData);
                else if (!regularData.IsRingEvent && !regularData.IsRotationEvent)
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0)); //Make sure that light turn off

                if (chromaData != null)
                    descriptor.EventPassed(false, 0, chromaData);
                else if (!regularData.IsUtilityEvent)
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, ColourManager.RGB_RESET));
            }
        }
        else descriptor.KillLights();
    }

    private void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        atsc.OnPlayToggle -= PlayToggle;
    }
}
