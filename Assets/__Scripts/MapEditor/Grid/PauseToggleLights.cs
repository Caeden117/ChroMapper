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
                List<BeatmapObjectContainer> lastEvent = events.LoadedContainers.Where(x =>
                (x.objectData as MapEvent)._type == i && x.objectData._time <= atsc.CurrentBeat)
                    .OrderByDescending(x => x.objectData._time).ToList();
                if (lastEvent.Count > 0)
                    descriptor.EventPassed(false, 0, (lastEvent.First() as BeatmapEventContainer).eventData);
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
