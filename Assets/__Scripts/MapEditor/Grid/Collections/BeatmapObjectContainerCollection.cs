using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Comparers;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Shared;
using Beatmap.V2;
using UnityEngine;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static readonly int ChunkSize = 5;

    public static float Epsilon = 0.001f;
    public static float TranslucentCull = -0.001f;

    private static BookmarkManager bookmarkManager;
    private static BookmarkManager bookmarkManagerInstance
        => bookmarkManager = bookmarkManager != null
            ? bookmarkManager
            : FindObjectOfType<BookmarkManager>();

    private static readonly Dictionary<ObjectType, BeatmapObjectContainerCollection> loadedCollections =
        new Dictionary<ObjectType, BeatmapObjectContainerCollection>();

    public event Action<BaseObject> ContainerSpawnedEvent;
    public event Action<BaseObject> ContainerDespawnedEvent;
    public AudioTimeSyncController AudioTimeSyncController;

    /// <summary>
    ///     Loaded objects in this collection.
    /// </summary>
    [Obsolete("LoadedObjects allocates a copy of the backing list of objects. Please avoid this unless you absolutely cannot grab a more precise type.")]
    public abstract List<BaseObject> LoadedObjects { get; }

    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;

    public Transform GridTransform;
    public Transform PoolTransform;
    public bool UseChunkLoadingWhenPlaying;
    public int ChunksLoadedWhilePlaying = 2;
    public bool IgnoreTrackFilter;

    private readonly Queue<ObjectContainer> pooledContainers = new Queue<ObjectContainer>();

    /// <summary>
    ///     A dictionary of all active BeatmapObjectContainers by the data they are attached to.
    /// </summary>
    // TODO(Caeden): Maybe rewrite this out? Have BaseObject -> ObjectContainer references in the BaseObject class.
    //   Reasoning: Half of CM's use here is iteration, which is slow with Dictionaries.
    //   The other half is to access containers by a BaseObject, which would be satisfied by storing that relation in the BaseObject class
    public Dictionary<BaseObject, ObjectContainer> LoadedContainers =
        new Dictionary<BaseObject, ObjectContainer>();

    public List<BaseObject> ObjectsWithContainers = new List<BaseObject>();

    private float previousAtscBeat = -1;
    private int previousChunk = -1;

    public static string TrackFilterID { get; private set; }

    public abstract ObjectType ContainerType { get; }

    private void Awake()
    {
        ObjectContainer.FlaggedForDeletionEvent += DeleteObject;
        if (loadedCollections.ContainsKey(ContainerType))
            loadedCollections[ContainerType] = this;
        else
            loadedCollections.Add(ContainerType, this);
        SubscribeToCallbacks();
    }

    private void Start()
    {
        UpdateEpsilon(Settings.Instance.TimeValueDecimalPrecision);
        Settings.NotifyBySettingName("TimeValueDecimalPrecision", UpdateEpsilon);
        EditorScaleController.EditorScaleChangedEvent += UpdateTranslucentCull;
    }

    internal virtual void LateUpdate()
    {
        if ((AudioTimeSyncController.IsPlaying && !UseChunkLoadingWhenPlaying)
            || AudioTimeSyncController.CurrentSongBpmTime == previousAtscBeat)
        {
            return;
        }

        previousAtscBeat = AudioTimeSyncController.CurrentSongBpmTime;
        var nearestChunk = (int)Math.Round(previousAtscBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        if (nearestChunk != previousChunk)
        {
            RefreshPool();
            previousChunk = nearestChunk;
        }
    }

    private void OnDestroy()
    {
        ObjectContainer.FlaggedForDeletionEvent -= DeleteObject;
        loadedCollections.Remove(ContainerType);
        UnsubscribeToCallbacks();
    }

    private void UpdateEpsilon(object precision)
    {
        Epsilon = 1 / Mathf.Pow(10, (int)precision);
        UpdateTranslucentCull(EditorScaleController.EditorScale);
    }

    // TODO(Caeden): Remove (unneeded)
    private void UpdateTranslucentCull(float editorScale) => TranslucentCull = -editorScale * Epsilon;

    /// <summary>
    ///     Grab a <see cref="BeatmapObjectContainerCollection" /> whose <see cref="ContainerType" /> matches the given type.
    ///     To grab an inherited class, consider using <see cref="GetCollectionForType{T}(BaseObject.ObjectType)" />.
    /// </summary>
    /// <param name="type">The specific type of <see cref="BaseObject" /> that the collection must contain.</param>
    /// <returns>A generic <see cref="BeatmapObjectContainerCollection" />.</returns>
    public static BeatmapObjectContainerCollection GetCollectionForType(ObjectType type)
    {
        loadedCollections.TryGetValue(type, out var collection);
        return collection;
    }

    /// <summary>
    ///     Grab a <see cref="BeatmapObjectContainerCollection" /> whose <see cref="ContainerType" /> matches the given type.
    /// </summary>
    /// <typeparam name="T">A specific inheriting class to cast to.</typeparam>
    /// <param name="type">The specific type of <see cref="BaseObject" /> that the collection must contain.</param>
    /// <returns>A casted <see cref="BeatmapObjectContainerCollection" />.</returns>
    public static T GetCollectionForType<T>(ObjectType type) where T : BeatmapObjectContainerCollection
    {
        loadedCollections.TryGetValue(type, out var collection);
        return collection as T;
    }

    /// <summary>
    ///     Grab a <see cref="BeatmapObjectContainerCollection" /> whose <see cref="ContainerType" /> matches the given type.
    /// </summary>
    /// <typeparam name="T">A specific inheriting class to cast to.</typeparam>
    /// <param name="type">The specific type of <see cref="BaseObject" /> that the collection must contain.</param>
    /// <returns>A casted <see cref="BeatmapObjectContainerCollection" />.</returns>
    public static T GetCollectionForType<T, TBaseObject>() where T : BeatmapObjectContainerCollection where TBaseObject : BaseObject
    {
        // god C# please let us switch directly by types instead of this garbage workaround
        var type = typeof(TBaseObject) switch
        {
            Type t when t == typeof(BaseNote) => ObjectType.Note,
            Type t when t == typeof(BaseObstacle) => ObjectType.Obstacle,
            Type t when t == typeof(BaseEvent) => ObjectType.Event,
            Type t when t == typeof(BaseArc) => ObjectType.Arc,
            Type t when t == typeof(BaseChain) => ObjectType.Chain,
            Type t when t == typeof(BaseBpmEvent) => ObjectType.BpmChange,
            Type t when t == typeof(BaseCustomEvent) => ObjectType.CustomEvent,
            Type t when t == typeof(BaseBookmark) => ObjectType.Bookmark,
            _ => throw new ArgumentException(nameof(TBaseObject))
        };

        loadedCollections.TryGetValue(type, out var collection);
        return collection as T;
    }

    /// <summary>
    ///     Refreshes pools of all active <see cref="BeatmapObjectContainerCollection" />
    /// </summary>
    /// <param name="forceRefresh">
    ///     Whether or not to forcefully recycle all containers and spawn them again. This will cause
    ///     quite a bit of lag.
    /// </param>
    public static void RefreshAllPools(bool forceRefresh = false)
    {
        foreach (var collection in loadedCollections.Values) collection.RefreshPool(forceRefresh);
    }

    /// <summary>
    ///     Refreshes the pool, with lower and upper bounds being automatically defined by chunks or spawn/despawn offsets.
    /// </summary>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public virtual void RefreshPool(bool forceRefresh = false)
    {
        var epsilon = Mathf.Pow(10, -9);
        if (AudioTimeSyncController.IsPlaying)
        {
            var spawnOffset = UseChunkLoadingWhenPlaying
                ? ChunksLoadedWhilePlaying * ChunkSize
                : SpawnCallbackController.Offset;
            var despawnOffset = UseChunkLoadingWhenPlaying
                ? -ChunksLoadedWhilePlaying * ChunkSize
                : DespawnCallbackController.Offset;
            RefreshPool(AudioTimeSyncController.CurrentSongBpmTime + despawnOffset - epsilon,
                AudioTimeSyncController.CurrentSongBpmTime + spawnOffset + epsilon, forceRefresh);
        }
        else
        {
            var nearestChunk = (int)Math.Round(previousAtscBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
            // Since ChunkDistance is the amount of total chunks, we divide by two so that the total amount of loaded chunks
            // both before and after the current one equal to the ChunkDistance setting
            var chunks = Mathf.RoundToInt(Settings.Instance.ChunkDistance / 2);
            RefreshPool(((nearestChunk - chunks) * ChunkSize) - epsilon,
                ((nearestChunk + chunks) * ChunkSize) + epsilon, forceRefresh);
        }
    }

    /// <summary>
    ///     Refreshes the pool with a defined lower and upper bound.
    /// </summary>
    /// <param name="lowerBound">Objects below this point in time will not be given a container.</param>
    /// <param name="upperBound">Objects above this point in time will not be given a container.</param>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public abstract void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false);

    /// <summary>
    ///     Dequeues a container from the pool and attaches it to a provided <see cref="BaseObject" />
    /// </summary>
    /// <param name="obj">Object to store within the container.</param>
    protected void CreateContainerFromPool(BaseObject obj)
    {
        if (obj.HasAttachedContainer) return;
        //Debug.Log($"Creating container with hash code {obj.GetHashCode()}");
        if (!pooledContainers.Any()) CreateNewObject();
        var dequeued = pooledContainers.Dequeue();
        dequeued.ObjectData = obj;
        dequeued.transform.localEulerAngles = Vector3.zero;
        dequeued.UpdateGridPosition();
        dequeued.SafeSetActive(true);
        UpdateContainerData(dequeued, obj);
        dequeued.OutlineVisible = SelectionController.IsObjectSelected(obj);
        PluginLoader.BroadcastEvent<ObjectLoadedAttribute, ObjectContainer>(dequeued);
        LoadedContainers.Add(obj, dequeued);
        ObjectsWithContainers.Add(obj);
        obj.HasAttachedContainer = true;
        OnContainerSpawn(dequeued, obj);
        ContainerSpawnedEvent?.Invoke(obj);
    }

    /// <summary>
    ///     Recycles the container belonging to a provided <see cref="BaseObject" />, putting it back into the container
    ///     pool for future use.
    /// </summary>
    /// <param name="obj">Object whose container will be recycled.</param>
    protected internal void RecycleContainer(BaseObject obj)
    {
        if (!obj.HasAttachedContainer) return;
        //Debug.Log($"Recycling container with hash code {obj.GetHashCode()}");
        var container = LoadedContainers[obj];
        container.ObjectData = null;
        container.SafeSetActive(false);
        //container.transform.SetParent(PoolTransform);
        LoadedContainers.Remove(obj);
        ObjectsWithContainers.Remove(obj);
        pooledContainers.Enqueue(container);
        OnContainerDespawn(container, obj);
        obj.HasAttachedContainer = false;
        ContainerDespawnedEvent?.Invoke(obj);
    }

    private void CreateNewObject()
    {
        var baseContainer = CreateContainer();
        baseContainer.gameObject.SetActive(false);
        baseContainer.Setup();
        //baseContainer.transform.SetParent(PoolTransform);
        baseContainer.transform.SetParent(GridTransform);
        pooledContainers.Enqueue(baseContainer);
    }

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<BaseObject> newObjects) => RemoveConflictingObjects(newObjects, out _);

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    /// <param name="conflicting">Enumerable of all existing objects that were deleted as a conflict.</param>
    public abstract void RemoveConflictingObjects(IEnumerable<BaseObject> newObjects, out List<BaseObject> conflicting);

    /// <summary>
    ///     Given a <see cref="ObjectContainer" />, delete its attached object.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction" /></param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    /// <param name="inCollectionOfDeletes">
    ///     Whether or not spawning is part of a collection of spawns
    ///     Set to true and call <see cref="DoPostObjectsDeleteWorkflow()" /> after to optimise spawning many objects
    ///</param>
    public void DeleteObject(ObjectContainer obj, bool triggersAction = true, string comment = "No comment.", bool inCollectionOfDeletes = false) =>
        DeleteObject(obj.ObjectData, triggersAction, true, comment);

    // Identical to above but I need this as the action doesn't work with option parameters
    public void DeleteObject(ObjectContainer obj, bool triggersAction = true, string comment = "No comment.") =>
        DeleteObject(obj.ObjectData, triggersAction, true, comment);

    /// <summary>
    ///     Deletes a <see cref="BaseObject" />.
    /// </summary>
    /// <param name="obj">To delete.</param>
    /// <param name="triggersAction">Whether or not it triggers a <see cref="BeatmapObjectDeletionAction" /></param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed as a result of this deletion.</param>
    /// <param name="comment">A comment that provides further description on why it was deleted.</param>
    /// <param name="inCollectionOfDeletes">
    ///     Whether or not spawning is part of a collection of spawns
    ///     Set to true and call <see cref="DoPostObjectsDeleteWorkflow()" /> after to optimise spawning many objects
    ///</param>
    ///<param name="deselect">Whether or not this object is immediately deselected upon deletion.</param>
    public abstract void DeleteObject(BaseObject obj, bool triggersAction = true, bool refreshesPool = true,
        string comment = "No comment.", bool inCollectionOfDeletes = false, bool deselect = true);

    public abstract void SilentRemoveObject(BaseObject obj);
    
    protected void SetTrackFilter() =>
        PersistentUI.Instance.ShowInputBox("Filter notes and obstacles shown while editing to a certain track ID.\n\n" +
                                           "If you dont know what you're doing, turn back now.", HandleTrackFilter);

    private void HandleTrackFilter(string res) =>
        TrackFilterID = string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res) ? null : res;

    /// <summary>
    ///     Spawns an object into the collection.
    /// </summary>
    /// <param name="obj">To spawn.</param>
    /// <param name="removeConflicting">
    ///     Whether or not <see cref="RemoveConflictingObjects(IEnumerable{BaseObject})" /> will
    ///     be called.
    /// </param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed.</param>
    /// <param name="inCollectionOfSpawns">Whether OnObjectSpawned will be called.</param>
    /// <param name="inCollectionOfSpawns">
    ///     Whether or not spawning is part of a collection of spawns
    ///     Set to true and call <see cref="DoPostObjectsSpawnedWorkflow()" /> after to optimise spawning many objects
    ///</param>
    public void SpawnObject(BaseObject obj, bool removeConflicting = true, bool refreshesPool = true, bool inCollectionOfSpawns = false) =>
        SpawnObject(obj, out _, removeConflicting, refreshesPool, inCollectionOfSpawns);

    /// <summary>
    ///     Spawns an object into the collection.
    /// </summary>
    /// <param name="obj">To spawn.</param>
    /// <param name="conflicting">An enumerable of all objects that were deleted as a conflict.</param>
    /// <param name="removeConflicting">
    ///     Whether or not
    ///     <see cref="RemoveConflictingObjects(IEnumerable{BaseObject}, out IEnumerable{BaseObject})" /> will be called.
    /// </param>
    /// <param name="refreshesPool">Whether or not the pool will be refreshed.</param>
    /// <param name="inCollectionOfSpawns">
    ///     Whether or not spawning is part of a collection of spawns.
    ///     Set to true and call <see cref="DoPostObjectsSpawnedWorkflow()" /> after to optimise spawning many objects
    ///</param>
    public abstract void SpawnObject(BaseObject obj, out List<BaseObject> conflicting, bool removeConflicting = true,
        bool refreshesPool = true, bool inCollectionOfSpawns = false);

    /// <summary>
    /// Returns <c>true</c> if the given object exists within this collection, and <c>false</c> otherwise.
    /// </summary>
    public abstract bool ContainsObject(BaseObject obj);

    public static void RefreshFutureObjectsPosition(float jsonTime)
    {
        foreach (var objectType in System.Enum.GetValues(typeof(Beatmap.Enums.ObjectType)))
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType((Beatmap.Enums.ObjectType)objectType);
            if (collection == null) continue;
            // REVIEW: not sure if allocation is avoidable
            foreach (var obj in collection.LoadedObjects)
            {
                if (obj.JsonTime > jsonTime)
                {
                    obj.RecomputeSongBpmTime();
                }
                else if (collection is ChainGridContainer || collection is ArcGridContainer)
                {
                    if ((obj as BaseSlider).TailJsonTime > jsonTime)
                    {
                        obj.RecomputeSongBpmTime();
                    }
                }
            }
            foreach (var container in collection.LoadedContainers)
            {
                if (container.Key.JsonTime > jsonTime)
                {
                    container.Value.UpdateGridPosition();
                }
                else if (collection is ObstacleGridContainer)
                {
                    if (container.Key.JsonTime + (container.Key as BaseObstacle).Duration > jsonTime)
                    {
                        container.Value.UpdateGridPosition();
                    }
                }
                else if (collection is ChainGridContainer || collection is ArcGridContainer)
                {
                    if ((container.Key as BaseSlider).TailJsonTime > jsonTime)
                    {
                        container.Value.UpdateGridPosition();
                    }
                }
            }
        }

        // Bookmarks aren't in the ContainerCollection yet so we have this
        foreach (var bookmark in bookmarkManagerInstance.bookmarkContainers)
        {
            if (bookmark.Data.JsonTime > jsonTime)
            {
                bookmark.Data.RecomputeSongBpmTime();
            }
        }
        bookmarkManagerInstance.RefreshBookmarkTimelinePositions();
    }

    protected virtual void UpdateContainerData(ObjectContainer con, BaseObject obj) { }

    protected virtual void OnObjectDelete(BaseObject obj, bool inCollection = false) { }

    protected virtual void OnObjectSpawned(BaseObject obj, bool inCollection = false) { }

    protected virtual void OnContainerSpawn(ObjectContainer container, BaseObject obj) { }

    protected virtual void OnContainerDespawn(ObjectContainer container, BaseObject obj) { }

    public virtual void DoPostObjectsSpawnedWorkflow() { }

    public virtual void DoPostObjectsDeleteWorkflow() { }

    public abstract ObjectContainer CreateContainer();

    internal abstract void SubscribeToCallbacks();

    internal abstract void UnsubscribeToCallbacks();
}

public abstract class BeatmapObjectContainerCollection<T> : BeatmapObjectContainerCollection where T : BaseObject
{
    public event Action<T> ObjectSpawnedEvent;
    public event Action<T> ObjectDeletedEvent;

    [Obsolete("LoadedObjects allocates a copy of the backing list of objects. Please avoid this unless you absolutely cannot grab a more precise type.")]
    public override List<BaseObject> LoadedObjects => MapObjects.ConvertAll(it => it as BaseObject);

    public List<T> MapObjects = new();

    public Span<T> GetBetween(float jsonTime, float jsonTime2)
    {
        if (MapObjects.Count == 0) return Span<T>.Empty;
        
        // Considering we're only concerned with time, we'll use a time-based comparer here.
        var span = MapObjects.AsSpan();
        var startIdx = span.BinarySearchBy(jsonTime, obj => obj.JsonTime);
        var endIdx = span.BinarySearchBy(jsonTime2, obj => obj.JsonTime);

        if (startIdx < 0) startIdx = ~startIdx;
        if (endIdx < 0) endIdx = ~endIdx;

        // March indexes in case of same time with different properties
        while (startIdx > 0 && span[startIdx].JsonTime >= jsonTime) startIdx--;
        if (span[startIdx].JsonTime < jsonTime) startIdx++;
        
        while (endIdx < span.Length && span[endIdx].JsonTime <= jsonTime2) endIdx++;

        var length = endIdx - startIdx;

        return length > 0
            ? span.Slice(startIdx, length)
            : Span<T>.Empty;
    }

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<T> newObjects) =>
        RemoveConflictingObjects(newObjects, out _);

    /// <inheritdoc/>
    public override void RemoveConflictingObjects(IEnumerable<BaseObject> newObjects, out List<BaseObject> conflicting)
    {
        RemoveConflictingObjects(newObjects.OfType<T>(), out var localConflicting);

        conflicting = localConflicting.ConvertAll(it => it as BaseObject);
    }

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    /// <param name="conflicting">Enumerable of all existing objects that were deleted as a conflict.</param>
    public void RemoveConflictingObjects(IEnumerable<T> newObjects, out List<T> conflicting)
    {
        conflicting = new List<T>();

        foreach (var newObject in newObjects)
        {
            Debug.Log($"Performing conflicting check at {newObject.JsonTime} for {newObject}");

            var localWindow = GetBetween(newObject.JsonTime - 0.1f, newObject.JsonTime + 0.1f);

            Debug.Log($"LocalWindow {string.Join("\n", localWindow.ToArray().ToList())}");
            
            for (var i = 0; i < localWindow.Length; i++)
            {
                var obj = localWindow[i];

                if (obj.IsConflictingWith(newObject) && newObject != obj) conflicting.Add(obj);
                
                Debug.Log($"Obj {obj} | newObj {newObject} | Res - {obj.IsConflictingWith(newObject)} | Equality - {newObject != obj}");
            }
        }

        conflicting.ForEach(conflict => DeleteObject(conflict, false, false));

        Debug.Log($"Removed {conflicting.Count} conflicting {ContainerType}s.");
    }

    /// <inheritdoc/>
    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        var span = MapObjects.AsSpan();

        // lmao why do anything if we dont have objects to recycle or create containers for
        if (span.Length == 0) return;

        // Easier to process recyclings at the beginning, rather than try to deal with it later.
        if (forceRefresh)
        {
            while (ObjectsWithContainers.Count > 0)
            {
                RecycleContainer(ObjectsWithContainers[0]);
            }
        }
        else
        {
            var containersSpan = ObjectsWithContainers.AsSpan();

            // We need to go backwards since *technically* modifying spans in iteration is unsafe.
            for (var i = containersSpan.Length - 1; i >= 0; i--)
            {
                var obj = containersSpan[i];

                switch (obj)
                {
                    case BaseObstacle obs when obs.SongBpmTime > upperBound || obs.SongBpmTime + obs.Duration < lowerBound:
                    case BaseSlider slider when slider.SongBpmTime > upperBound || slider.TailSongBpmTime < lowerBound:
                    case not null when obj.SongBpmTime > upperBound || obj.SongBpmTime < lowerBound:
                        RecycleContainer(obj);
                        break;
                    default: continue;
                }
            }
        }

        // We need to copy GetBetween implementation:
        //   - We are binary searching by SongBpmTime, not JsonTime (this should still be sorted since MapObjects is always sorted by JsonTime)
        //   - We need access to startIdx later in this method
        var startIdx = span.BinarySearchBy(lowerBound, obj => obj.SongBpmTime);
        var endIdx = span.BinarySearchBy(upperBound, obj => obj.SongBpmTime);

        if (startIdx < 0) startIdx = ~startIdx;
        if (endIdx < 0) endIdx = ~endIdx;

        while (endIdx < span.Length && span[endIdx].SongBpmTime <= upperBound) endIdx++;

        var length = endIdx - startIdx;

        // All objects in our window defaultly get a container
        var windowSpan = span.Slice(startIdx, length);
        for (var i = 0; i < windowSpan.Length; i++)
        {
            var obj = windowSpan[i];

            CreateContainerFromPool(obj);
        }

        // this is a bit of a dirty check but i'd like this early return
        if (span[0] is not (BaseObstacle or BaseSlider)) return;

        // Handle special cases for certain objects that exist over a period of time (need startIdx here)
        for (var i = 0; i < startIdx; i++)
        {
            var obj = span[i];

            if (obj is BaseObstacle obs && obs.SongBpmTime < lowerBound && obs.SongBpmTime + obs.Duration >= lowerBound)
            {
                CreateContainerFromPool(obj);
            }
            else if (obj is BaseSlider slider && slider.SongBpmTime < lowerBound && slider.TailSongBpmTime >= lowerBound)
            {
                CreateContainerFromPool(obj);
            }
        }
    }

    /// <inheritdoc/>
    public override void DeleteObject(BaseObject obj, bool triggersAction = true, bool refreshesPool = true,
        string comment = "No comment.", bool inCollectionOfDeletes = false, bool deselect = true)
    {
        if (obj is not T localObj) return;

        DeleteObject(localObj, triggersAction, refreshesPool, comment, inCollectionOfDeletes, deselect);
    }

    /// <inheritdoc/>
    // TODO(Caeden): Overload to delete/spawn without recycling or creating a container
    public void DeleteObject(T obj, bool triggersAction = true, bool refreshesPool = true,
        string comment = "No comment.", bool inCollectionOfDeletes = false, bool deselect = true)
    {
        var search = MapObjects.BinarySearch(obj);
        
        if (!HasFoundCorrectObject(search, obj)) return;
        
        RecycleContainer(obj);
        
        MapObjects.RemoveAt(search);

        if (deselect) SelectionController.Deselect(obj, triggersAction);

        if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(obj, comment));

        if (refreshesPool) RefreshPool();

        OnObjectDelete(obj, inCollectionOfDeletes);
        ObjectDeletedEvent?.Invoke(obj);
    }
    
    // Removes object from MapObjects while retaining container and data in it
    public override void SilentRemoveObject(BaseObject obj)
    {
        if (obj is not T tObj) return;
        
        var search = MapObjects.BinarySearch(tObj);

        if (!HasFoundCorrectObject(search, tObj)) return;
        
        MapObjects.RemoveAt(search);
    }

    private bool HasFoundCorrectObject(int search, BaseObject obj)
    {
        // Unhappy path: Binary Search returns negative number
        if (search < 0)
        {
            // The objects are not in the collection, but are still being removed.
            // This could be because of ghost blocks, so let's try forcefully recycling that container.
            Debug.LogError($"This object is not in the collection and appears to be a ghost. Please report this.");
            
            return false;
        }

        // Unhappy path: Binary Search returns an object, but turns out to be the incorrect object.
        if (MapObjects[search] != obj)
        {
            // Binary Search returned a value, but this value is not the object we're looking to delete.
            Debug.LogError("Binary Search returned incorrect object. Please report this.");

            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public override void SpawnObject(BaseObject obj, out List<BaseObject> conflicting, bool removeConflicting = true,
        bool refreshesPool = true, bool inCollectionOfSpawns = false)
    {
        conflicting = new List<BaseObject>();

        if (obj is not T localObj) return;

        SpawnObject(localObj, out var localConflicting, removeConflicting, refreshesPool, inCollectionOfSpawns);

        for (var i = 0; i < localConflicting.Count; i++)
        {
            conflicting.Add(localConflicting[i]);
        }
    }

    /// <inheritdoc/>
    public void SpawnObject(T obj, bool removeConflicting = true, bool refreshesPool = true, bool inCollectionOfSpawns = false) =>
        SpawnObject(obj, out _, removeConflicting, refreshesPool, inCollectionOfSpawns);

    /// <inheritdoc/>
    // TODO(Caeden): Overload to delete/spawn without recycling or creating a container
    public void SpawnObject(T obj, out List<T> conflicting, bool removeConflicting = true,
        bool refreshesPool = true, bool inCollectionOfSpawns = false)
    {
        //Debug.Log($"Spawning object with hash code {obj.GetHashCode()}");
        if (removeConflicting)
        {
            RemoveConflictingObjects(new T[] { obj }, out conflicting);
        }
        else
        {
            conflicting = new List<T>();
        }

        var search = MapObjects.BinarySearch(obj);
        var insertIdx = search >= 0 ? search : ~search;
        MapObjects.Insert(insertIdx, obj);

        OnObjectSpawned(obj, inCollectionOfSpawns);
        ObjectSpawnedEvent?.Invoke(obj);

        //Debug.Log($"Total object count: {LoadedObjects.Count}");
        if (refreshesPool) RefreshPool();
    }

    /// <inheritdoc/>
    public override bool ContainsObject(BaseObject obj) => obj is T localObj && ContainsObject(localObj);

    /// <inheritdoc/>
    public bool ContainsObject(T obj) => MapObjects.BinarySearch(obj) >= 0;
}
