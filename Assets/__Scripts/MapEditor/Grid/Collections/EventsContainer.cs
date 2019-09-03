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
        SpawnCallbackController.RecursiveEventCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold += DespawnCallback;
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks() {
        SpawnCallbackController.EventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished -= RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
    }

    public override void SortObjects()
    {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ToList();
        StartCoroutine(WaitUntilChunkLoad());
    }

    //Because BeatmapEventContainers need to modify materials, we need to wait before we load by chunks.
    private IEnumerator WaitUntilChunkLoad()
    {
        yield return new WaitForSeconds(0.5f);
        UseChunkLoading = true;
    }

    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        LoadedContainers[index]?.gameObject?.SetActive(true);
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        LoadedContainers[index]?.gameObject?.SetActive(false);
    }

    void OnPlayToggle(bool playing)
    {
        if (playing) {
            foreach (BeatmapObjectContainer e in LoadedContainers)
            {
                bool enabled = e.objectData._time < AudioTimeSyncController.CurrentBeat + SpawnCallbackController.offset
                    && e.objectData._time >= AudioTimeSyncController.CurrentBeat + DespawnCallbackController.offset;
                if (e.PreviousActiveState != enabled) e.gameObject.SetActive(enabled);
                e.PreviousActiveState = enabled;
            }
        }
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj)
    {
        UseChunkLoading = false;
        BeatmapEventContainer beatmapEvent = BeatmapEventContainer.SpawnEvent(obj as MapEvent, ref eventPrefab, ref eventAppearanceSO);
        beatmapEvent.transform.SetParent(GridTransform);
        beatmapEvent.UpdateGridPosition();
        LoadedContainers.Add(beatmapEvent);
        return beatmapEvent;
    }
}
