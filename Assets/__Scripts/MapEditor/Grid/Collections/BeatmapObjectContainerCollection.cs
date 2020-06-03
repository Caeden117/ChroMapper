using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static readonly int ChunkSize = 5;

    public static string TrackFilterID { get; private set; } = null;

    private static Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection> loadedCollections = new Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection>();

    public AudioTimeSyncController AudioTimeSyncController;
    /// <summary>
    /// A sorted set of loaded BeatmapObjects that is garaunteed to be sorted by time.
    /// </summary>
    public SortedSet<BeatmapObject> LoadedObjects = new SortedSet<BeatmapObject>(new BeatmapObjectComparer());
    /// <summary>
    /// A list of unsorted BeatmapObjects. Recommended only for fast iteration.
    /// </summary>
    public List<BeatmapObject> UnsortedObjects = new List<BeatmapObject>();
    public HashSet<BeatmapObject> ObjectsWithLoadedContainers = new HashSet<BeatmapObject>();
    public Dictionary<BeatmapObject, BeatmapObjectContainer> LoadedContainers = new Dictionary<BeatmapObject, BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public Transform PoolTransform;
    public bool UseChunkLoading = true;
    public bool UseChunkLoadingWhenPlaying = false;
    public bool IgnoreTrackFilter;

    private Queue<BeatmapObjectContainer> PooledContainers = new Queue<BeatmapObjectContainer>();
    private float previousATSCBeat = -1;
    private int previousChunk = -1;

    public abstract BeatmapObject.Type ContainerType { get; }

    public static BeatmapObjectContainerCollection GetAnyCollection() => GetCollectionForType<NotesContainer>(BeatmapObject.Type.NOTE);

    public static BeatmapObjectContainerCollection GetCollectionForType(BeatmapObject.Type type)
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection;
    }

    public static T GetCollectionForType<T>(BeatmapObject.Type type) where T : BeatmapObjectContainerCollection
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection as T;
    }

    /// <summary>
    /// Pretty taxing. Use sparringly.
    /// </summary>
    public static void RefreshAllPools()
    {
        foreach (BeatmapObjectContainerCollection collection in loadedCollections.Values)
        {
            collection.RefreshPool();
        }
    }

    private void OnEnable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += DeleteObject;
        loadedCollections.Add(ContainerType, this);
        SubscribeToCallbacks();
    }

    public void PopulatePool()
    {
        for (int i = 0; i < Settings.Instance.InitialLoadBatchSize; i++)
        {
            CreateNewObject();
        }
    }

    public void RefreshPool()
    {
        float epsilon = Mathf.Pow(10, -9);
        if (AudioTimeSyncController.IsPlaying)
        {
            float spawnOffset = UseChunkLoadingWhenPlaying ? (2 * ChunkSize) : SpawnCallbackController.offset;
            float despawnOffset = UseChunkLoadingWhenPlaying ? (-2 * ChunkSize) : DespawnCallbackController.offset;
            RefreshPool(AudioTimeSyncController.CurrentBeat + despawnOffset - epsilon,
                AudioTimeSyncController.CurrentBeat + spawnOffset + epsilon);
        }
        else
        {
            int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
            int chunks = Settings.Instance.ChunkDistance;
            RefreshPool((nearestChunk - chunks) * ChunkSize - epsilon,
                (nearestChunk + chunks) * ChunkSize + epsilon);
        }
    }

    public void RefreshPool(float lowerBound, float upperBound)
    {
        if (UnsortedObjects.Count() != LoadedObjects.Count())
        {
            UnsortedObjects = LoadedObjects.ToList();
        }
        foreach (var obj in UnsortedObjects)
        //for (int i = 0; i < LoadedObjects.Count; i++)
        {
            bool hasContainer = ObjectsWithLoadedContainers.Contains(obj);
            //BeatmapObject obj = LoadedObjects.ElementAt(i);
            if (obj._time < lowerBound && hasContainer)
            {
                if (obj is BeatmapObstacle obst && obst._time + obst._duration >= lowerBound) continue;
                RecycleContainer(obj);
            }
            else if (obj._time > upperBound && hasContainer)
            {
                RecycleContainer(obj);
            }
            else if (obj._time >= lowerBound && obj._time <= upperBound && !hasContainer)
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    protected void CreateContainerFromPool(BeatmapObject obj)
    {
        if (!PooledContainers.Any()) PopulatePool();
        BeatmapObjectContainer dequeued = PooledContainers.Dequeue();
        dequeued.objectData = obj;
        dequeued.transform.localEulerAngles = Vector3.zero;
        dequeued.transform.SetParent(GridTransform);
        dequeued.UpdateGridPosition();
        dequeued.SafeSetActive(true);
        UpdateContainerData(dequeued, obj);
        dequeued.OutlineVisible = SelectionController.IsObjectSelected(obj);
        LoadedContainers.Add(obj, dequeued);
        ObjectsWithLoadedContainers.Add(obj);
    }

    protected void RecycleContainer(BeatmapObject obj)
    {
        if (ObjectsWithLoadedContainers.Contains(obj))
        {
            BeatmapObjectContainer container = LoadedContainers[obj];
            container.objectData = null;
            container.SafeSetActive(false);
            container.transform.SetParent(PoolTransform);
            LoadedContainers.Remove(obj);
            PooledContainers.Enqueue(container);
            ObjectsWithLoadedContainers.Remove(obj);
        }
    }

    private void CreateNewObject()
    {
        BeatmapObjectContainer baseContainer = CreateContainer();
        baseContainer.gameObject.SetActive(false);
        baseContainer.Setup();
        baseContainer.transform.SetParent(PoolTransform);
        PooledContainers.Enqueue(baseContainer);
    }

    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects) => RemoveConflictingObjects(newObjects, out _);

    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects, out IEnumerable<BeatmapObject> conflicting)
    {
        int conflictingObjects = 0;
        float epsilon = Mathf.Pow(10, -5) * 2;
        //Here we create dummy objects that will share the same time, but slightly different.
        //With the BeatmapObjectComparer, it does not care what type these are, it only compares time.
        BeatmapObject dummyA = new MapEvent(0, 0, 0);
        BeatmapObject dummyB = new MapEvent(0, 0, 0);
        conflicting = new BeatmapObject[] { };
        foreach (BeatmapObject newObject in newObjects)
        {
            dummyA._time = newObject._time - epsilon;
            dummyB._time = newObject._time + epsilon;
            Debug.Log($"Performing conflicting check at {newObject._time} with bounds {dummyA._time} to {dummyB._time}");
            foreach (BeatmapObject toCheck in LoadedObjects.GetViewBetween(dummyA, dummyB))
            {
                if (AreObjectsAtSameTimeConflicting(newObject, toCheck))
                {
                    conflicting.Append(toCheck);
                    conflictingObjects++;
                }
            }
        }
        foreach (BeatmapObject conflict in conflicting) //Haha InvalidOperationException go brrrrrrrrr
        {
            DeleteObject(conflict);
        }
        Debug.Log($"Removed {conflictingObjects} conflicting {ContainerType}s.");
    }

    public void DeleteObject(BeatmapObjectContainer obj, bool triggersAction = true, string comment = "No comment.")
    {
        DeleteObject(obj.objectData, triggersAction, true, comment);
    }

    public void DeleteObject(BeatmapObject obj, bool triggersAction = true, bool refreshesPool = true, string comment = "No comment.")
    {
        if (LoadedObjects.Remove(obj))
        {
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));
            RecycleContainer(obj);
            if (refreshesPool) RefreshPool();
        }
    }

    internal virtual void LateUpdate()
    {
        if ((AudioTimeSyncController.IsPlaying && !UseChunkLoadingWhenPlaying)
            || AudioTimeSyncController.CurrentBeat == previousATSCBeat) return;
        previousATSCBeat = AudioTimeSyncController.CurrentBeat;
        int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        if (nearestChunk != previousChunk)
        {
            RefreshPool();
            previousChunk = nearestChunk;
        }
    }

    private void OnDisable()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= DeleteObject;
        loadedCollections.Remove(ContainerType);
        UnsubscribeToCallbacks();
    }

    protected void SetTrackFilter()
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

    public void SpawnObject(BeatmapObject obj, bool removeConflicting = true, bool refreshesPool = true) => SpawnObject(obj, out _, removeConflicting, refreshesPool);

    public void SpawnObject(BeatmapObject obj, out IEnumerable<BeatmapObject> conflicting, bool removeConflicting = true, bool refreshesPool = true)
    {
        if (removeConflicting)
        {
            RemoveConflictingObjects(new[] { obj }, out conflicting);
        }
        else
        {
            conflicting = new BeatmapObject[] { };
        }
        LoadedObjects.Add(obj);
        if (refreshesPool)
        {
            RefreshPool();
        }
    }

    protected virtual void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj) { }

    protected abstract bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b);
    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer CreateContainer();
}
