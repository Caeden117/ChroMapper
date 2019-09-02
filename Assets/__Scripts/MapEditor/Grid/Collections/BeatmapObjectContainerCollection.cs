using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static int ChunkRenderDistance = 5;
    public static int ChunkSize = 5;

    public AudioTimeSyncController AudioTimeSyncController;
    public List<BeatmapObjectContainer> LoadedContainers = new List<BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public bool UseChunkLoading { get; internal set; } = false;
    private float previousATSCBeat;

    private void OnEnable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += CreateActionThenDelete;
        SubscribeToCallbacks();
    }

    private void CreateActionThenDelete(BeatmapObjectContainer obj)
    {
        BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj));
        DeleteObject(obj);
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

    internal void Update()
    {
        if (AudioTimeSyncController.IsPlaying || !UseChunkLoading || AudioTimeSyncController.CurrentBeat == previousATSCBeat)
            return;
        previousATSCBeat = AudioTimeSyncController.CurrentBeat;
        int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        foreach (BeatmapObjectContainer e in LoadedContainers)
        {
            bool enabled = e.ChunkID < nearestChunk + ChunkRenderDistance && e.ChunkID >= nearestChunk - ChunkRenderDistance;
            if (e.PreviousActiveState != enabled) e.gameObject.SetActive(enabled);
            e.PreviousActiveState = enabled;
        }
    }

    private void OnDisable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= CreateActionThenDelete;
        UnsubscribeToCallbacks();
    }

    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer SpawnObject(BeatmapObject obj);
}
