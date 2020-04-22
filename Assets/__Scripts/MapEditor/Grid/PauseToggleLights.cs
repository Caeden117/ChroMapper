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
            BeatmapEventContainer[] allEvents = events.LoadedContainers.Cast<BeatmapEventContainer>().Reverse().ToArray();
            foreach(BeatmapEventContainer e in allEvents)
            {
                if (!e.eventData.IsChromaEvent && e.eventData._time <= atsc.CurrentBeat && eventTypesHash.Add(e.eventData._type))
                {
                    lastEvents.Add(e);
                }
            }

            foreach (BeatmapEventContainer e in allEvents)
            {
                if (e.eventData.IsChromaEvent && e.eventData._time <= atsc.CurrentBeat && eventTypesHash.Contains(e.eventData._type))
                {
                    lastChromaEvents.Add(e);
                }
            }
            MapEvent blankEvent = new MapEvent(0, 0, 0);
            for (int i = 0; i < 16; i++)
            {
                if (!eventTypesHash.Contains(i))
                {
                    blankEvent._type = i;
                    if (blankEvent.IsRingEvent || blankEvent.IsRotationEvent) continue;
                    descriptor.EventPassed(false, 0, blankEvent);
                    continue;
                }

                //Grab all the events of the type, and that are behind current beat
                BeatmapEventContainer regular = lastEvents.Find(x => x.eventData._type == i);
                BeatmapEventContainer chroma = lastChromaEvents.Find(x => x.eventData._type == i);

                MapEvent regularData = regular?.eventData ?? null;
                MapEvent chromaData = chroma?.eventData ?? null;

                //Past the last event, or an Off event if theres none, it is a ring event, or if there is a fade
                if (regularData._value != MapEvent.LIGHT_VALUE_BLUE_FADE && regularData._value != MapEvent.LIGHT_VALUE_RED_FADE &&
                    !regularData.IsRingEvent) 
                    descriptor.EventPassed(false, 0, regularData);
                else if (!regularData.IsRingEvent && !regularData.IsRotationEvent)
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0)); //Make sure that light turn off

                if (!regularData.IsUtilityEvent)
                {
                    if (chromaData != null)
                        descriptor.EventPassed(false, 0, chromaData);
                    else descriptor.EventPassed(false, 0, new MapEvent(0, i, ColourManager.RGB_RESET));
                }
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
