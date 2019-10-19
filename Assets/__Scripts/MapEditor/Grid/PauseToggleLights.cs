using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PauseToggleLights : MonoBehaviour
{
    private PlatformDescriptor descriptor;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

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
                List<BeatmapObjectContainer> lastEvents = events.LoadedContainers.Where(x =>
                (x.objectData as MapEvent)._type == i && x.objectData._time <= atsc.CurrentBeat)
                    .OrderByDescending(x => x.objectData._time).ToList();
                if (lastEvents.Count > 0) //Past the last event, or an Off event if theres none
                    descriptor.EventPassed(false, 0, (lastEvents.First() as BeatmapEventContainer).eventData);
                else descriptor.EventPassed(false, 0, new MapEvent(0, i, 0)); //Make sure that light turn off

                List<BeatmapObjectContainer> lastChromaEvents = lastEvents.Where(x => //Grab Chroma events from this list
                    (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET).ToList();
                if (lastChromaEvents.Count > 0) //Apply the last Chroma event, or reset colors if theres none.
                    descriptor.EventPassed(false, 0, (lastChromaEvents.First() as BeatmapEventContainer).eventData);
                else descriptor.EventPassed(false, 0, new MapEvent(0, i, ColourManager.RGB_RESET));
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
