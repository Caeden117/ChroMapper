using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static readonly int ChunkSize = 5;

    public static float Epsilon = 0.001f;
    public static float TranslucentCull = -0.001f;

    private void Start()
    {
        UpdateEpsilon(Settings.Instance.TimeValueDecimalPrecision);
        Settings.NotifyBySettingName("TimeValueDecimalPrecision", UpdateEpsilon);
        Settings.NotifyBySettingName("EditorScale", UpdateEpsilon);
    }

    private void UpdateEpsilon(object precision)
    {
        Epsilon = 1 / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        TranslucentCull = -Settings.Instance.EditorScale * Epsilon;
    }

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
    /// A dictionary of all active BeatmapObjectContainers by the data they are attached to.
    /// </summary>
    public Dictionary<BeatmapObject, BeatmapObjectContainer> LoadedContainers = new Dictionary<BeatmapObject, BeatmapObjectContainer>();
    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;
    public Transform GridTransform;
    public Transform PoolTransform;
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
            // Since ChunkDistance is the amount of total chunks, we divide by two so that the total amount of loaded chunks
            // both before and after the current one equal to the ChunkDistance setting
            int chunks = Mathf.RoundToInt(Settings.Instance.ChunkDistance / 2);
            RefreshPool((nearestChunk - chunks) * ChunkSize - epsilon,
                (nearestChunk + chunks) * ChunkSize + epsilon, forceRefresh);
        }
    }

    public SortedSet<BeatmapObject> GetBetween(float time, float time2) {
        // Events etc. can still have a sort order between notes
        var now = new BeatmapNote(time - 0.0000001f, 0, 0, 0, 0);
        var window = new BeatmapNote(time2 + 0.0000001f, 0, 0, 0, 0);
        return LoadedObjects.GetViewBetween(now, window);
    }

    /// <summary>
    /// Refreshes the pool with a defined lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Objects below this point in time will not be given a container.</param>
    /// <param name="upperBound">Objects above this point in time will not be given a container.</param>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        foreach (var obj in UnsortedObjects)
        //for (int i = 0; i < LoadedObjects.Count; i++)
        {
            if (forceRefresh)
            {
                RecycleContainer(obj);
            }
            if (obj._time >= lowerBound && obj._time <= upperBound)
            {
                if (!obj.HasAttachedContainer) CreateContainerFromPool(obj);
            }
            else if (obj.HasAttachedContainer)
            {
                if (obj is BeatmapObstacle obs && obs._time < lowerBound && obs._time + obs._duration >= lowerBound) continue;
                RecycleContainer(obj);
            }
            if (obj is BeatmapObstacle obst && obst._time < lowerBound && obst._time + obst._duration >= lowerBound)
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
        if (obj.HasAttachedContainer) return;
        //Debug.Log($"Creating container with hash code {obj.GetHashCode()}");
        if (!PooledContainers.Any())
        {
            CreateNewObject();
        }
        BeatmapObjectContainer dequeued = PooledContainers.Dequeue();
        dequeued.objectData = obj;
        dequeued.transform.localEulerAngles = Vector3.zero;
        dequeued.UpdateGridPosition();
        dequeued.SafeSetActive(true);
        UpdateContainerData(dequeued, obj);
        dequeued.OutlineVisible = SelectionController.IsObjectSelected(obj);
        PluginLoader.BroadcastEvent<ObjectLoadedAttribute, BeatmapObjectContainer>(dequeued);
        LoadedContainers.Add(obj, dequeued);
        obj.HasAttachedContainer = true;
        OnContainerSpawn(dequeued, obj);
    }

    /// <summary>
    /// Recycles the container belonging to a provided <see cref="BeatmapObject"/>, putting it back into the container pool for future use.
    /// </summary>
    /// <param name="obj">Object whose container will be recycled.</param>
    protected void RecycleContainer(BeatmapObject obj)
    {
        if (!obj.HasAttachedContainer) return;
        //Debug.Log($"Recycling container with hash code {obj.GetHashCode()}");
        BeatmapObjectContainer container = LoadedContainers[obj];
        container.objectData = null;
        container.SafeSetActive(false);
        //container.transform.SetParent(PoolTransform);
        LoadedContainers.Remove(obj);
        PooledContainers.Enqueue(container);
        OnContainerDespawn(container, obj);
        obj.HasAttachedContainer = false;
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
        conflicting = new List<BeatmapObject>();
        var conflictingInternal = new HashSet<BeatmapObject>();
        var newSet = new HashSet<BeatmapObject>(newObjects);
        foreach (BeatmapObject newObject in newObjects)
        {
            Debug.Log($"Performing conflicting check at {newObject._time}");

            var localWindow = GetBetween(newObject._time - 0.1f, newObject._time + 0.1f);
            var conflicts = localWindow.Where(x => x.IsConflictingWith(newObject) && !newSet.Contains(x)).ToList();
            foreach (var beatmapObject in conflicts)
            {
                conflictingInternal.Add(beatmapObject);
            }
        }

        foreach (BeatmapObject conflict in conflictingInternal) //Haha InvalidOperationException go brrrrrrrrr
        {
            DeleteObject(conflict, false, false);
        }
        Debug.Log($"Removed {conflictingInternal.Count} conflicting {ContainerType}s.");
        conflicting.AddRange(conflictingInternal);
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
        var removed = UnsortedObjects.Remove(obj);
        var removed2 = LoadedObjects.Remove(obj);

        if (removed && removed2)
        {
            //Debug.Log($"Deleting container with hash code {toDelete.GetHashCode()}");
            SelectionController.Deselect(obj, triggersAction);
            if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));
            RecycleContainer(obj);
            if (refreshesPool) RefreshPool();
            OnObjectDelete(obj);
        }
        else
        {
            // The objects are not in the collection, but are still being removed.
            // This could be because of ghost blocks, so let's try forcefully recycling that container.
            Debug.LogError($"Object could not be deleted, please report this ({removed}, {removed2})");
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
        //Debug.Log($"Spawning object with hash code {obj.GetHashCode()}");
        if (removeConflicting)
        {
            RemoveConflictingObjects(new[] { obj }, out conflicting);
        }
        else
        {
            conflicting = new List<BeatmapObject>() { };
        }
        LoadedObjects.Add(obj);
        UnsortedObjects.Add(obj);
        OnObjectSpawned(obj);
        //Debug.Log($"Total object count: {LoadedObjects.Count}");
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

    internal abstract void SubscribeToCallbacks();

    internal abstract void UnsubscribeToCallbacks();
}
