using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static int ChunkSize = 5;
    public static string TrackFilterID { get; private set; } = null;

    private static Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection> loadedCollections = new Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection>();

    public AudioTimeSyncController AudioTimeSyncController;
    public List<BeatmapObjectContainer> LoadedContainers = new List<BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public bool UseChunkLoading = true;
    public bool IgnoreTrackFilter;
    private float previousATSCBeat = -1;
    private bool levelLoaded;

    public abstract BeatmapObject.Type ContainerType { get; }

    public static BeatmapObjectContainerCollection GetAnyCollection() => GetCollectionForType(BeatmapObject.Type.NOTE);

    public static BeatmapObjectContainerCollection GetCollectionForType(BeatmapObject.Type type)
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection;
    }

    private void OnEnable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += DeleteObject;
        LoadInitialMap.LevelLoadedEvent += LevelHasLoaded;
        loadedCollections.Add(ContainerType, this);
        SubscribeToCallbacks();
    }

    private void LevelHasLoaded()
    {
        levelLoaded = true;
    }

    public void RemoveConflictingObjects()
    {
        List<BeatmapObjectContainer> old = new List<BeatmapObjectContainer>(LoadedContainers);
        foreach (BeatmapObjectContainer stayedAlive in LoadedContainers.DistinctBy(x => x.objectData.ConvertToJSON()))
        {
            old.Remove(stayedAlive);
        }
        foreach (BeatmapObjectContainer conflicting in old)
        {
            DeleteObject(conflicting, false);
        }
        Debug.Log($"Removed {old.Count} conflicting objects.");
    }

    public void DeleteObject(BeatmapObjectContainer obj, bool triggersAction = true, string comment = "No comment.")
    {
        if (LoadedContainers.Contains(obj))
        {
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));
            LoadedContainers.Remove(obj);
            Destroy(obj.gameObject);
            SelectionController.RefreshMap();
        }
    }

    internal virtual void LateUpdate()
    {
        if (AudioTimeSyncController.IsPlaying || !UseChunkLoading || AudioTimeSyncController.CurrentBeat == previousATSCBeat || !levelLoaded) return;
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
            if (!enabled && BoxSelectionPlacementController.IsSelecting) continue;
            e.SafeSetActive(enabled);
        }
    }

    private void OnDisable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= DeleteObject;
        LoadInitialMap.LevelLoadedEvent -= LevelHasLoaded;
        loadedCollections.Remove(ContainerType);
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

    protected bool ConflictingByTrackIDs(BeatmapObject a, BeatmapObject b)
    {
        if (a._customData is null && b._customData is null) return true; //Both dont exist, they are conflicting (default track)
        if (a._customData is null || b._customData is null) return false; //One exists, but not other; they dont conflict
        if (a._customData["track"] is null && b._customData["track"] is null) return true; //Both dont exist, they are conflicting
        if (a._customData["track"] is null || b._customData["track"] is null) return false; //One exists, but not other
        return a._customData["track"].Value == b._customData["track"].Value; //If both exist, check string values.
    }

    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true);
    public BeatmapObjectContainer SpawnObject(BeatmapObject obj, bool removeConflicting = true, bool refreshMap = true) => SpawnObject(obj, out _, removeConflicting, refreshMap);
}
