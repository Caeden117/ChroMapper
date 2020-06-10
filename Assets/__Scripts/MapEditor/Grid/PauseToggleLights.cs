using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    private HashSet<int> eventTypesHash = new HashSet<int>();
    private List<MapEvent> lastEvents = new List<MapEvent>();
    private List<MapEvent> lastChromaEvents = new List<MapEvent>();

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
            IEnumerable<MapEvent> allEvents = events.LoadedObjects.Cast<MapEvent>().Reverse();
            foreach(MapEvent e in allEvents)
            {
                if (!e.IsChromaEvent && e._time <= atsc.CurrentBeat && eventTypesHash.Add(e._type))
                {
                    lastEvents.Add(e);
                }
            }

            foreach (MapEvent e in allEvents)
            {
                if (e.IsChromaEvent && e._time <= atsc.CurrentBeat && eventTypesHash.Contains(e._type))
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
                MapEvent regular = lastEvents.Find(x => x._type == i);
                MapEvent chroma = lastChromaEvents.Find(x => x._type == i);

                MapEvent regularData = regular ?? null;
                MapEvent chromaData = chroma ?? null;

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
