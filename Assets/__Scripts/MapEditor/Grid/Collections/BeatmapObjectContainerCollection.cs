using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static int ChunkSize = 5;

    public AudioTimeSyncController AudioTimeSyncController;
    public List<BeatmapObjectContainer> LoadedContainers = new List<BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public bool UseChunkLoading = false;
    private float previousATSCBeat = -1;
    private bool levelLoaded = false;

    private void OnEnable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += CreateActionThenDelete;
        LoadInitialMap.LevelLoadedEvent += LevelHasLoaded;
        SubscribeToCallbacks();
    }

    private void CreateActionThenDelete(BeatmapObjectContainer obj)
    {
        BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj));
        DeleteObject(obj);
    }

    private void LevelHasLoaded()
    {
        levelLoaded = true;
    }

    public void DeleteObject(BeatmapObjectContainer obj)
    {
        if (LoadedContainers.Contains(obj))
        {
            LoadedContainers.Remove(obj);
            Destroy(obj.gameObject);
            SelectionController.RefreshMap();
        }
    }

    internal virtual void LateUpdate()
    {
        if (AudioTimeSyncController.IsPlaying || !UseChunkLoading || AudioTimeSyncController.CurrentBeat == previousATSCBeat
            || !levelLoaded) return;
        previousATSCBeat = AudioTimeSyncController.CurrentBeat;
        int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        foreach (BeatmapObjectContainer e in LoadedContainers)
        {
            bool enabled = e.ChunkID < nearestChunk + Settings.Instance.ChunkDistance &&
                e.ChunkID >= nearestChunk - Settings.Instance.ChunkDistance;
            e.SafeSetActive(enabled);
        }
    }

    private void OnDisable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= CreateActionThenDelete;
        LoadInitialMap.LevelLoadedEvent -= LevelHasLoaded;
        UnsubscribeToCallbacks();
    }

    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting);
}
