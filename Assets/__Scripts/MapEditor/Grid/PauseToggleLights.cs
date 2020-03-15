using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

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
        if (isPlaying)
        {
            for (int i = 0; i < 15; i++)
            {
                //Grab all the events of the type, and that are behind current beat
                IEnumerable<BeatmapEventContainer> lastEvents = events.LoadedContainers.Where(x =>
                (x.objectData as MapEvent)._type == i && x.objectData._time <= atsc.CurrentBeat)
                    .OrderByDescending(x => x.objectData._time).Cast<BeatmapEventContainer>();

                if (!lastEvents.Any())
                {
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0));
                    continue;
                }

                //Past the last event, or an Off event if theres none, it is a ring event, or if there is a fade
                MapEvent data = lastEvents.First().eventData;
                if (data._value != MapEvent.LIGHT_VALUE_BLUE_FADE && data._value != MapEvent.LIGHT_VALUE_RED_FADE &&
                    i != MapEvent.EVENT_TYPE_RINGS_ROTATE && i != MapEvent.EVENT_TYPE_RINGS_ZOOM) 
                    descriptor.EventPassed(false, 0, data);
                else if (i != MapEvent.EVENT_TYPE_RINGS_ZOOM && i != MapEvent.EVENT_TYPE_RINGS_ROTATE)
                    descriptor.EventPassed(false, 0, new MapEvent(0, i, 0)); //Make sure that light turn off

                if (!data.IsUtilityEvent)
                {
                    //Grab Chroma events and apply them if it exists, or reset the color if it doesn't.
                    IEnumerable<BeatmapEventContainer> lastChromaEvents = lastEvents.Where(x =>
                        (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET);
                    if (lastChromaEvents.Count() > 0) //Apply the last Chroma event.
                        descriptor.EventPassed(false, 0, (lastChromaEvents.First() as BeatmapEventContainer).eventData);
                    else
                        descriptor.EventPassed(false, 0, new MapEvent(0, i, ColourManager.RGB_RESET));
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
