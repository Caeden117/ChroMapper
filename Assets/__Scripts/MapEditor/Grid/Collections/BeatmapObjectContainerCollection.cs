using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static int ChunkSize = 5;
    public static string TrackFilterID { get; private set; } = null;

    public AudioTimeSyncController AudioTimeSyncController;
    public List<BeatmapObjectContainer> LoadedContainers = new List<BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public bool UseChunkLoading = false;
    public bool IgnoreTrackFilter = false;
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
        UpdateChunks();
    }

    private void UpdateChunks()
    {
        int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        foreach (BeatmapObjectContainer e in LoadedContainers)
        {
            bool enabled = e.ChunkID < nearestChunk + Settings.Instance.ChunkDistance &&
                e.ChunkID >= nearestChunk - Settings.Instance.ChunkDistance &&
                (TrackFilterID == null || (e.objectData._customData?["track"] ?? "") == TrackFilterID || IgnoreTrackFilter);
            e.SafeSetActive(enabled);
        }
    }

    private void OnDisable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= CreateActionThenDelete;
        LoadInitialMap.LevelLoadedEvent -= LevelHasLoaded;
        UnsubscribeToCallbacks();
    }

    public void SetTrackFilter()
    {
        PersistentUI.Instance.ShowInputBox("Filter notes and obstacles shown while editing to a certain track ID.\n\n" +
            "If you dont know what you're doing, turn back now.", HandleTrackFilter);
    }

    private void HandleTrackFilter(string res)
    {
        TrackFilterID = (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) ? null : res;
        SendMessage("UpdateChunks");
    }

    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting);
}
