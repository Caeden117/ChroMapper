using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventsContainer : MonoBehaviour {

    [SerializeField] AudioTimeSyncController audioTimeSyncController;

    //An easy way to edit notes
    [SerializeField] public List<BeatmapObjectContainer> loadedEvents = new List<BeatmapObjectContainer>();

    [SerializeField] BeatmapObjectCallbackController spawnCallbackController;
    [SerializeField] BeatmapObjectCallbackController despawnCallbackController;

    private void OnEnable()
    {
        //audioTimeSyncController.OnPlayToggle += OnPlayToggle;

        spawnCallbackController.EventPassedThreshold += SpawnCallback;
        spawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        despawnCallbackController.EventPassedThreshold += DespawnCallback;
    }

    private void OnDisable()
    {
        //audioTimeSyncController.OnPlayToggle -= OnPlayToggle;

        spawnCallbackController.EventPassedThreshold -= SpawnCallback;
        spawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        despawnCallbackController.EventPassedThreshold -= DespawnCallback;
    }

    public void SortEvents()
    {
        loadedEvents = loadedEvents.OrderBy(x => x.objectData._time).ToList();
    }

    //We don't need to check index as that's already done further up the chain
    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            loadedEvents[index].gameObject.SetActive(true);
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            loadedEvents[index].gameObject.SetActive(false);
    }

    void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            for (int i = 0; i < loadedEvents.Count; i++)
            {
                loadedEvents[i].gameObject.SetActive(i < spawnCallbackController.NextEventIndex && i >= despawnCallbackController.NextEventIndex);
            }
        }
        else
        {
            for (int i = 0; i < loadedEvents.Count; i++)
            {
                loadedEvents[i].gameObject.SetActive(true);
            }
        }
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        if (audioTimeSyncController.IsPlaying)
        {
            for (int i = 0; i < loadedEvents.Count; i++)
            {
                loadedEvents[i].gameObject.SetActive(i < spawnCallbackController.NextEventIndex && i >= despawnCallbackController.NextEventIndex);
            }
        }
        else
        {
            for (int i = 0; i < loadedEvents.Count; i++)
            {
                loadedEvents[i].gameObject.SetActive(true);
            }
        }
    }
}
