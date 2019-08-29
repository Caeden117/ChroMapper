using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EventsContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold += DespawnCallback;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveNoteCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold -= DespawnCallback;
    }

    public override void SortObjects()
    {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
    }

    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            LoadedContainers[index].gameObject.SetActive(true);
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (index >= 0)
            LoadedContainers[index].gameObject.SetActive(false);
    }

    void OnPlayToggle(bool playing)
    {
        if (playing)
            foreach (BeatmapObjectContainer container in LoadedContainers)
            {
                BeatmapEventContainer e = container as BeatmapEventContainer;
                container.gameObject.SetActive(e.objectData._time < AudioTimeSyncController.CurrentBeat + SpawnCallbackController.offset
                    && e.objectData._time >= AudioTimeSyncController.CurrentBeat - DespawnCallbackController.offset);
            }
        else
            for (int i = 0; i < LoadedContainers.Count; i++)
                LoadedContainers[i].gameObject.SetActive(true);
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj)
    {
        BeatmapEventContainer beatmapEvent = BeatmapEventContainer.SpawnEvent(obj as MapEvent, ref eventPrefab, ref eventAppearanceSO);
        beatmapEvent.transform.SetParent(GridTransform);
        beatmapEvent.UpdateGridPosition();
        LoadedContainers.Add(beatmapEvent);
        return beatmapEvent;
    }
}
