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
    /// <summary>
    /// A HashSet of all BeatmapObjects that currently have a container attached to them.
    /// </summary>
    public HashSet<BeatmapObject> ObjectsWithLoadedContainers = new HashSet<BeatmapObject>();
    /// <summary>
    /// A dictionary of all active BeatmapObjectContainers by the data they are attached to.
    /// </summary>
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
    /// Refreshes pools of all active <see cref="BeatmapObjectContainerCollection"/>
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

    /// <summary>
    /// Refreshes the pool, with lower and upper bounds being automatically defined by chunks or spawn/despawn offsets.
    /// </summary>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public void RefreshPool(bool forceRefresh = false)
    {
        float epsilon = Mathf.Pow(10, -9);
        if (AudioTimeSyncController.IsPlaying)
        {
            float spawnOffset = UseChunkLoadingWhenPlaying ? (2 * ChunkSize) : SpawnCallbackController.offset;
            float despawnOffset = UseChunkLoadingWhenPlaying ? (-2 * ChunkSize) : DespawnCallbackController.offset;
            RefreshPool(AudioTimeSyncController.CurrentBeat + despawnOffset - epsilon,
                AudioTimeSyncController.CurrentBeat + spawnOffset + epsilon, forceRefresh);
        }
        else
        {
            int nearestChunk = (int)Math.Round(previousATSCBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
            int chunks = Settings.Instance.ChunkDistance;
            RefreshPool((nearestChunk - chunks) * ChunkSize - epsilon,
                (nearestChunk + chunks) * ChunkSize + epsilon, forceRefresh);
        }
    }

    /// <summary>
    /// Refreshes the pool with a defined lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Objects below this point in time will not be given a container.</param>
    /// <param name="upperBound">Objects above this point in time will not be given a container.</param>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        if (UnsortedObjects.Count() != LoadedObjects.Count())
        {
            UnsortedObjects = LoadedObjects.ToList();
        }
        if (forceRefresh)
        {
            foreach (var obj in UnsortedObjects)
            {
                RecycleContainer(obj);
            }
        }
        foreach (var obj in ObjectsWithLoadedContainers.ToList())
        {
            if (obj._time < lowerBound)
            {
                if (obj is BeatmapObstacle obst && obst._time + obst._duration >= lowerBound) continue;
                RecycleContainer(obj);
            }
            else if (obj._time > upperBound)
            {
                RecycleContainer(obj);
            }
        }
        foreach (var obj in UnsortedObjects)
        //for (int i = 0; i < LoadedObjects.Count; i++)
        {
            if (obj._time >= lowerBound && obj._time <= upperBound && !ObjectsWithLoadedContainers.Contains(obj))
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    /// <summary>
    /// Dequeues a container from the pool and attaches it to a provided <see cref="BeatmapObject"/>
    /// </summary>
    /// <param name="obj">Object to store within the container.</param>
    protected void CreateContainerFromPool(BeatmapObject obj)
    {
        if (!PooledContainers.Any())
        {
            CreateNewObject();
        }
        BeatmapObjectContainer dequeued = PooledContainers.Dequeue();
        dequeued.objectData = obj;
        dequeued.transform.localEulerAngles = Vector3.zero;
        //dequeued.transform.SetParent(GridTransform);
        dequeued.UpdateGridPosition();
        dequeued.SafeSetActive(true);
        UpdateContainerData(dequeued, obj);
        dequeued.OutlineVisible = SelectionController.IsObjectSelected(obj);
        LoadedContainers.Add(obj, dequeued);
        ObjectsWithLoadedContainers.Add(obj);
    }

    /// <summary>
    /// Recycles the container belonging to a provided <see cref="BeatmapObject"/>, putting it back into the container pool for future use.
    /// </summary>
    /// <param name="obj">Object whose container will be recycled.</param>
    protected void RecycleContainer(BeatmapObject obj)
    {
        if (ObjectsWithLoadedContainers.Contains(obj))
        {
            BeatmapObjectContainer container = LoadedContainers[obj];
            container.objectData = null;
            container.SafeSetActive(false);
            //container.transform.SetParent(PoolTransform);
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
        //baseContainer.transform.SetParent(PoolTransform);
        baseContainer.transform.SetParent(GridTransform);
        PooledContainers.Enqueue(baseContainer);
    }

    /// <summary>
    /// Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects) => RemoveConflictingObjects(newObjects, out _);

    /// <summary>
    /// Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    /// <param name="conflicting">Enumerable of all existing objects that were deleted as a conflict.</param>
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

    /// <summary>
    /// Given a <see cref="BeatmapObjectContainer"/>, delete its attached object.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction"/></param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    public void DeleteObject(BeatmapObjectContainer obj, bool triggersAction = true, string comment = "No comment.")
    {
        DeleteObject(obj.objectData, triggersAction, true, comment);
    }

    /// <summary>
    /// Deletes a <see cref="BeatmapObject"/>.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction"/></param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed as a result of this deletion.</param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    public void DeleteObject(BeatmapObject obj, bool triggersAction = true, bool refreshesPool = true, string comment = "No comment.")
    {
        if (LoadedObjects.Remove(obj))
        {
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));
            RecycleContainer(obj);
            if (refreshesPool) RefreshPool();
            OnObjectDelete(obj);
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
    }

    /// <summary>
    /// Spawns an object into the collection.
    /// </summary>
    /// <param name="obj">To spawn.</param>
    /// <param name="removeConflicting">Whether or not <see cref="RemoveConflictingObjects(IEnumerable{BeatmapObject})"/> will be called.</param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed.</param>
    public void SpawnObject(BeatmapObject obj, bool removeConflicting = true, bool refreshesPool = true) => SpawnObject(obj, out _, removeConflicting, refreshesPool);

    /// <summary>
    /// SSpawns an object into the collection.
    /// </summary>
    /// <param name="obj">To spawn.</param>
    /// <param name="conflicting">An enumerable of all objects that were deleted as a conflict.</param>
    /// <param name="removeConflicting">Whether or not <see cref="RemoveConflictingObjects(IEnumerable{BeatmapObject}, out IEnumerable{BeatmapObject})"/> will be called.</param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed.</param>
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
        OnObjectSpawned(obj);
        if (refreshesPool)
        {
            RefreshPool();
        }
    }

    protected virtual void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj) { }

    protected virtual void OnObjectDelete(BeatmapObject obj) { }

    protected virtual void OnObjectSpawned(BeatmapObject obj) { }

    protected abstract bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b);
    internal abstract void SubscribeToCallbacks();
    internal abstract void UnsubscribeToCallbacks();
    public abstract void SortObjects();
    public abstract BeatmapObjectContainer CreateContainer();
}
