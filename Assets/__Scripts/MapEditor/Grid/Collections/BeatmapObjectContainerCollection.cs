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

    /// <summary>
    /// Grab a <see cref="BeatmapObjectContainerCollection"/> whose <see cref="ContainerType"/> matches the given type.
    /// To grab an inherited class, consider using <see cref="GetCollectionForType{T}(BeatmapObject.Type)"/>.
    /// </summary>
    /// <param name="type">The specific type of <see cref="BeatmapObject"/> that the collection must contain.</param>
    /// <returns>A generic <see cref="BeatmapObjectContainerCollection"/>.</returns>
    public static BeatmapObjectContainerCollection GetCollectionForType(BeatmapObject.Type type)
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection;
    }

    /// <summary>
    /// Grab a <see cref="BeatmapObjectContainerCollection"/> whose <see cref="ContainerType"/> matches the given type.
    /// </summary>
    /// <typeparam name="T">A specific inheriting class to cast to.</typeparam>
    /// <param name="type">The specific type of <see cref="BeatmapObject"/> that the collection must contain.</param>
    /// <returns>A casted <see cref="BeatmapObjectContainerCollection"/>.</returns>
    public static T GetCollectionForType<T>(BeatmapObject.Type type) where T : BeatmapObjectContainerCollection
    {
        loadedCollections.TryGetValue(type, out BeatmapObjectContainerCollection collection);
        return collection as T;
    }

    /// <summary>
    /// Refreshes pools of all active <see cref="BeatmapObjectContainerCollection"/>
    /// </summary>
    /// <param name="forceRefresh">Whether or not to forcefully recycle all containers and spawn them again. This will cause quite a bit of lag.</param>
    public static void RefreshAllPools(bool forceRefresh = false)
    {
        foreach (BeatmapObjectContainerCollection collection in loadedCollections.Values)
        {
            collection.RefreshPool(forceRefresh);
        }
    }

    private void Awake()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent += DeleteObject;
        if (loadedCollections.ContainsKey(ContainerType))
        {
            loadedCollections[ContainerType] = this;
        }
        else
        {
            loadedCollections.Add(ContainerType, this);
        }
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
        foreach (var obj in ObjectsWithLoadedContainers.ToList())
        {
            if (forceRefresh)
            {
                RecycleContainer(obj);
                continue;
            }
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
        PluginLoader.BroadcastEvent<ObjectLoadedAttribute, BeatmapObjectContainer>(dequeued);
        LoadedContainers.Add(obj, dequeued);
        ObjectsWithLoadedContainers.Add(obj);
        OnContainerSpawn(dequeued, obj);
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
            OnContainerDespawn(container, obj);
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
    public void RemoveConflictingObjects(IEnumerable<BeatmapObject> newObjects, out List<BeatmapObject> conflicting)
    {
        int conflictingObjects = 0;
        //Due to floating point precision errors, I have set epsilon to be equal to the decimal precision of the user.
        //With the default value of 3 (+/- 0.001), this should work just fine. 2 and below, you might run into some issues.
        //There's a reason why that value is still in Experimental settings.
        float epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        //Here we create dummy objects that will share the same time, but slightly different.
        //With the BeatmapObjectComparer, it does not care what type these are, it only compares time.
        BeatmapObject dummyA = new MapEvent(0, 0, 0);
        BeatmapObject dummyB = new MapEvent(0, 0, 0);
        conflicting = new List<BeatmapObject>();
        foreach (BeatmapObject newObject in newObjects)
        {
            //dummyA._time = newObject._time - epsilon;
            //dummyB._time = newObject._time + epsilon;
            Debug.Log($"Performing conflicting check at {newObject._time} with bounds {dummyA._time} to {dummyB._time}");
            /*foreach (BeatmapObject toCheck in LoadedObjects.GetViewBetween(dummyA, dummyB))
            {
                if (AreObjectsAtSameTimeConflicting(newObject, toCheck))
                {
                    conflicting.Add(toCheck);
                    conflictingObjects++;
                }
            }*/
            // Floating point precision fails here, and GetViewBetween throws a fit about it.
            // To solve that, we compare time values with very big integer numbers.
            long dummyAComparison = BigTimeComparison(newObject._time) - 1;
            long dummyBComparison = BigTimeComparison(newObject._time) + 1;
            foreach (BeatmapObject toCheck in LoadedObjects.Where(x => BigTimeComparison(x._time) > dummyAComparison &&
                BigTimeComparison(x._time) < dummyBComparison))
            {
                if (AreObjectsAtSameTimeConflicting(newObject, toCheck))
                {
                    conflicting.Add(toCheck);
                    conflictingObjects++;
                }
            }
        }
        foreach (BeatmapObject conflict in conflicting) //Haha InvalidOperationException go brrrrrrrrr
        {
            DeleteObject(conflict, false, false);
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
        float epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision + 1);
        BeatmapObject toDelete = LoadedObjects.FirstOrDefault(x => x.Equals(obj));
        if (toDelete != null && LoadedObjects.Remove(toDelete))
        {
            SelectionController.Deselect(toDelete);
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(toDelete, comment));
            RecycleContainer(toDelete);
            if (refreshesPool) RefreshPool();
            OnObjectDelete(toDelete);
        }
        else
        {
            Debug.Log("Could not locate requested to be deleted object");
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

    private void OnDestroy()
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

    private long BigTimeComparison(float time)
    {
        // Round our time value to the user's decimal precision, then put it as a string.
        string timeAsString = time.ToString($"F{Settings.Instance.TimeValueDecimalPrecision}");
        // Little bit janky, but we remove the decimal place, then parse it as a long
        // Why long? Because sometimes the numbers this outputs get so large that an "int" isn't enough
        return long.Parse(string.Join("", timeAsString.Split('.')));
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
    public void SpawnObject(BeatmapObject obj, out List<BeatmapObject> conflicting, bool removeConflicting = true, bool refreshesPool = true)
    {
        if (removeConflicting)
        {
            RemoveConflictingObjects(new[] { obj }, out conflicting);
        }
        else
        {
            conflicting = new List<BeatmapObject>() { };
        }
        LoadedObjects.Add(obj);
        OnObjectSpawned(obj);
        Debug.Log($"Total object count: {LoadedObjects.Count}");
        if (refreshesPool)
        {
            RefreshPool();
        }
    }

    /// <summary>
    /// Grabs <see cref="LoadedObjects"/> with other potential orderings added in. 
    /// This should not be used unless saving into a map file. Use <see cref="LoadedObjects"/> instead.
    /// </summary>
    /// <returns>A list of sorted objects</returns>
    public virtual IEnumerable<BeatmapObject> GrabSortedObjects() => LoadedObjects;

    protected virtual void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj) { }

    protected virtual void OnObjectDelete(BeatmapObject obj) { }

    protected virtual void OnObjectSpawned(BeatmapObject obj) { }

    protected virtual void OnContainerSpawn(BeatmapObjectContainer container, BeatmapObject obj) { }

    protected virtual void OnContainerDespawn(BeatmapObjectContainer container, BeatmapObject obj) { }

    public abstract BeatmapObjectContainer CreateContainer();

    protected abstract bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b);

    internal abstract void SubscribeToCallbacks();

    internal abstract void UnsubscribeToCallbacks();
}
