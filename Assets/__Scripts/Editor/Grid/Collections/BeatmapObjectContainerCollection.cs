using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using ZLinq;

public abstract class BeatmapObjectContainerCollection : MonoBehaviour
{
    public static readonly int ChunkSize = 5;

    public static float Epsilon = 0.001f;

    private static BookmarkManager bookmarkManager;

    private static BookmarkManager bookmarkManagerInstance =>
        bookmarkManager = bookmarkManager != null
            ? bookmarkManager
            : FindAnyObjectByType<BookmarkManager>();

    private static readonly Dictionary<ObjectType, BeatmapObjectContainerCollection> loadedCollections = new();

    public event Action<BaseObject> OnContainerSpawned;
    public event Action<BaseObject> OnContainerDespawned;

    [Header("Dependencies")] public BeatmapRuntimeContext BeatmapContext;
    public EditModeContext EditContext;
    public EditingMode ViewableMode = (EditingMode)byte.MaxValue;

    /// <summary>
    ///     Loaded objects in this collection.
    /// </summary>
    [Obsolete(
        "LoadedObjects allocates a copy of the backing list of objects. Please avoid this unless you absolutely cannot grab a more precise type.")]
    public abstract List<BaseObject> LoadedObjects { get; }

    // Visit backing objects in beat order without allocating the obsolete LoadedObjects copy.
    public abstract void ForEachObjectBetweenSongBpmTime(
        float start,
        float end,
        Action<BeatmapObjectContainerCollection, BaseObject> callback);

    public BeatmapObjectCallbackController SpawnCallbackController;
    public BeatmapObjectCallbackController DespawnCallbackController;

    public Transform TargetTransform;

    [Header("Chunk Loading")] public bool UseChunkLoadingWhenPlaying;
    public int ChunksLoadedWhilePlaying = 2;
    public bool IgnoreTrackFilter;

    private readonly Queue<ObjectContainer> pooledContainers = new();

    /// <summary>
    ///     A dictionary of all active BeatmapObjectContainers by the data they are attached to.
    /// </summary>
    // TODO(Caeden): Maybe rewrite this out? Have BaseObject -> ObjectContainer references in the BaseObject class.
    //   Reasoning: Half of CM's use here is iteration, which is slow with Dictionaries.
    //   The other half is to access containers by a BaseObject, which would be satisfied by storing that relation in the BaseObject class
    public Dictionary<BaseObject, ObjectContainer> LoadedContainers = new();

    public List<BaseObject> ObjectsWithContainers = new();

    private float previousAtscBeat = -1;
    private int previousChunk = -1;

    public static string TrackFilterID { get; private set; }

    public abstract ObjectType ContainerType { get; }

    private void Awake()
    {
        loadedCollections[ContainerType] = this;
        SubscribeToCallbacks();
    }

    private void Start()
    {
        UpdateEpsilon(Settings.Instance.TimeValueDecimalPrecision);
        Settings.NotifyBySettingName("TimeValueDecimalPrecision", UpdateEpsilon);
        EditContext.OnEditModeChanged += HandleEditModeChanged;
        HandleEditModeChanged(EditContext.EditingMode);
    }

    internal virtual void LateUpdate()
    {
        if ((BeatmapContext.Atsc.IsPlaying && !UseChunkLoadingWhenPlaying)
            || Mathf.Approximately(BeatmapContext.Atsc.CurrentSongBpmTime, previousAtscBeat))
            return;

        previousAtscBeat = BeatmapContext.Atsc.CurrentSongBpmTime;
        var nearestChunk = (int)Math.Round(previousAtscBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
        if (nearestChunk != previousChunk)
        {
            RefreshPool();
            previousChunk = nearestChunk;
        }
    }

    private void OnDestroy()
    {
        loadedCollections.Remove(ContainerType);
        UnsubscribeToCallbacks();
        EditContext.OnEditModeChanged -= HandleEditModeChanged;
    }

    private void HandleEditModeChanged(EditingMode mode) => enabled = ViewableMode.HasFlag(mode);

    private void UpdateEpsilon(object precision)
    {
        Epsilon = 1 / Mathf.Pow(10, (int)precision);
        JSONNumber.DecimalPrecision = (int)precision;
    }

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
    public static T GetCollectionForType<T, TBaseObject>()
        where T : BeatmapObjectContainerCollection where TBaseObject : BaseObject
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
            Type t when t == typeof(BaseNJSEvent) => ObjectType.NJSEvent,
            Type t when t == typeof(BaseEnvironmentEnhancement) => ObjectType.EnvironmentEnhancement,
            Type t when t == typeof(BaseLightColorEventBoxGroup) => ObjectType.GLSColor,
            Type t when t == typeof(BaseLightRotationEventBoxGroup) => ObjectType.GLSRotation,
            Type t when t == typeof(BaseLightTranslationEventBoxGroup) => ObjectType.GLSTranslation,
            Type t when t == typeof(BaseVfxEventEventBoxGroup) => ObjectType.GLSFloatFx,
            // imma be honest, idk if this actually ever needed
            Type t when t == typeof(BaseLightColorBase) => ObjectType.GLSEvent,
            Type t when t == typeof(BaseLightRotationBase) => ObjectType.GLSEvent,
            Type t when t == typeof(BaseLightTranslationBase) => ObjectType.GLSEvent,
            Type t when t == typeof(BaseFxEventFloat) => ObjectType.GLSEvent,
            Type t when t == typeof(BaseRotationEvent) => ObjectType.RotationEvent,
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
    ///     Updates the grid position of every container across all loaded <see cref="BeatmapObjectContainerCollection" />.
    /// </summary>
    public static void UpdateAllGridPositions()
    {
        foreach (var obj in loadedCollections
            .Values.AsValueEnumerable()
            .SelectMany(collection => collection.LoadedContainers.Values))
            obj.UpdateGridPosition();
    }

    /// <summary>
    ///     Refreshes the pool, with lower and upper bounds being automatically defined by chunks or spawn/despawn offsets.
    /// </summary>
    /// <param name="forceRefresh">All currently active containers will be recycled, even if they shouldn't be.</param>
    public virtual void RefreshPool(bool forceRefresh = false)
    {
        var epsilon = Mathf.Pow(10, -9);
        if (BeatmapContext.Atsc.IsPlaying)
        {
            var spawnOffset = UseChunkLoadingWhenPlaying
                ? ChunksLoadedWhilePlaying * ChunkSize
                : SpawnCallbackController.Offset;
            var despawnOffset = UseChunkLoadingWhenPlaying
                ? -ChunksLoadedWhilePlaying * ChunkSize
                : DespawnCallbackController.Offset;
            RefreshPool(
                BeatmapContext.Atsc.CurrentSongBpmTime + despawnOffset - epsilon,
                BeatmapContext.Atsc.CurrentSongBpmTime + spawnOffset + epsilon,
                forceRefresh);
        }
        else
        {
            var nearestChunk = (int)Math.Round(previousAtscBeat / (double)ChunkSize, MidpointRounding.AwayFromZero);
            // Since ChunkDistance is the amount of total chunks, we divide by two so that the total amount of loaded chunks
            // both before and after the current one equal to the ChunkDistance setting
            var chunks = Mathf.RoundToInt(Settings.Instance.ChunkDistance / 2);
            RefreshPool(
                ((nearestChunk - chunks) * ChunkSize) - epsilon,
                ((nearestChunk + chunks) * ChunkSize) + epsilon,
                forceRefresh);
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
        // This collection's dictionary is authoritative; the shared object flag can be stale after parent replacement.
        if (LoadedContainers.ContainsKey(obj))
        {
            obj.HasAttachedContainer = true;
            return;
        }
        obj.HasAttachedContainer = false;
        //Debug.Log($"Creating container with hash code {obj.GetHashCode()}");
        if (pooledContainers.Count == 0) CreateNewObject();
        var dequeued = pooledContainers.Dequeue();
        dequeued.ObjectData = obj;
        dequeued.transform.localEulerAngles = Vector3.zero;
        dequeued.UpdateGridPosition();
        dequeued.SafeSetActive(true);
        UpdateContainerData(dequeued, obj);
        dequeued.SetOutlineColor(SelectionController.SelectedColor);
        dequeued.Selected = SelectionController.IsObjectSelected(obj);
        PluginLoader.BroadcastEvent<ObjectLoadedAttribute, ObjectContainer>(dequeued);
        LoadedContainers.Add(obj, dequeued);
        ObjectsWithContainers.Add(obj);
        obj.HasAttachedContainer = true;
        HandleContainerSpawn(dequeued, obj);
        OnContainerSpawned?.Invoke(obj);
    }

    /// <summary>
    ///     Recycles the container belonging to a provided <see cref="BaseObject" />, putting it back into the container
    ///     pool for future use.
    /// </summary>
    /// <param name="obj">Object whose container will be recycled.</param>
    protected internal void RecycleContainer(BaseObject obj, int? indexInObjectsWithContainers = null)
    {
        // Recycle dictionary-owned visuals even when a stale object flag incorrectly says no container is attached.
        if (!LoadedContainers.TryGetValue(obj, out var container))
        {
            if (indexInObjectsWithContainers is not null)
                ObjectsWithContainers.RemoveAt(indexInObjectsWithContainers.Value);
            else // TODO O(N), and this is called in a loop
                ObjectsWithContainers.Remove(obj);
            obj.HasAttachedContainer = false;
            return;
        }
        //Debug.Log($"Recycling container with hash code {obj.GetHashCode()}");
        container.ObjectData = null;
        container.SafeSetActive(false);
        LoadedContainers.Remove(obj);

        if (indexInObjectsWithContainers is not null)
            ObjectsWithContainers.RemoveAt(indexInObjectsWithContainers.Value);
        else // TODO O(N), and this is called in a loop
            ObjectsWithContainers.Remove(obj);
        pooledContainers.Enqueue(container);
        HandleContainerDespawn(container, obj);
        obj.HasAttachedContainer = false;
        OnContainerDespawned?.Invoke(obj);
    }

    private void CreateNewObject()
    {
        var baseContainer = CreateContainer();
        baseContainer.gameObject.SetActive(false);
        baseContainer.Setup();
        baseContainer.transform.SetParent(TargetTransform);
        pooledContainers.Enqueue(baseContainer);
    }

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<BaseObject> newObjects) =>
        RemoveConflictingObjects(newObjects, out _);

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
    public void DeleteObject(
        ObjectContainer obj,
        bool triggersAction = true,
        string comment = "No comment.",
        bool inCollectionOfDeletes = false) =>
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
    public abstract void DeleteObject(
        BaseObject obj,
        bool triggersAction = true,
        bool refreshesPool = true,
        string comment = "No comment.",
        bool inCollectionOfDeletes = false,
        bool deselect = true,
        bool triggerHandle = true);

    // RefreshSpecialAngles mutates sort keys in place, so test teardown needs a collection-owned
    // tail deletion path that does not binary-search an intentionally stale ordering.
    public abstract void DeleteAllObjectsFromEnd(bool triggersAction = true);

    public abstract void SilentRemoveObject(BaseObject obj);

    protected void SetTrackFilter() =>
        PersistentUI.Instance.ShowInputBox(
            "Filter notes and obstacles shown while editing to a certain track ID.\n\n"
            + "If you dont know what you're doing, turn back now.",
            HandleTrackFilter);

    private void HandleTrackFilter(string res)
    {
        TrackFilterID = string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res) ? null : res;
        RefreshAllPools(true);
    }

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
    public void SpawnObject(
        BaseObject obj,
        bool removeConflicting = true,
        bool refreshesPool = true,
        bool inCollectionOfSpawns = false) =>
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
    public abstract void SpawnObject(
        BaseObject obj,
        out List<BaseObject> conflicting,
        bool removeConflicting = true,
        bool refreshesPool = true,
        bool inCollectionOfSpawns = false);

    /// <summary>
    /// Returns <c>true</c> if the given object exists within this collection, and <c>false</c> otherwise.
    /// </summary>
    public abstract bool ContainsObject(BaseObject obj);

    public static void RefreshFutureObjectsPosition(float jsonTime)
    {
        // we have to refresh bpm events FIRST, and only then can we refresh other objects
        var objectTypes = new List<ObjectType>
        {
            ObjectType.BpmChange,
            ObjectType.Note,
            ObjectType.Event,
            ObjectType.Obstacle,
            ObjectType.CustomNote,
            ObjectType.CustomEvent,
            ObjectType.Arc,
            ObjectType.Chain,
            ObjectType.Bookmark,
            ObjectType.Waypoint,
            ObjectType.NJSEvent,
            ObjectType.RotationEvent
        };

        foreach (var objectType in objectTypes)
        {
            var collection = GetCollectionForType(objectType);
            if (collection == null) continue;
            // REVIEW: not sure if allocation is avoidable
            foreach (var obj in collection.LoadedObjects)
            {
                if (obj.JsonTime > jsonTime)
                    obj.RecomputeSongBpmTime();
                else
                {
                    switch (collection)
                    {
                        case ChainGridContainer:
                        case ArcGridContainer:
                            {
                                if ((obj as BaseSlider).TailJsonTime > jsonTime) obj.RecomputeSongBpmTime();
                                break;
                            }
                        case ObstacleGridContainer:
                            {
                                if ((obj as BaseObstacle).Duration + obj.JsonTime > jsonTime)
                                    obj.RecomputeSongBpmTime();
                                break;
                            }
                    }
                }
            }

            foreach (var container in collection.LoadedContainers)
            {
                if (container.Key.JsonTime > jsonTime)
                    container.Value.UpdateGridPosition();
                else if (collection is ObstacleGridContainer)
                {
                    if (container.Key.JsonTime + (container.Key as BaseObstacle).Duration > jsonTime)
                        container.Value.UpdateGridPosition();
                }
                else if (collection is ChainGridContainer || collection is ArcGridContainer)
                    if ((container.Key as BaseSlider).TailJsonTime > jsonTime)
                        container.Value.UpdateGridPosition();
            }
        }

        // Bookmarks aren't in the ContainerCollection yet so we have this
        foreach (var bookmark in bookmarkManagerInstance.bookmarkContainers)
            if (bookmark.Data.JsonTime > jsonTime)
                bookmark.Data.RecomputeSongBpmTime();

        bookmarkManagerInstance.RefreshBookmarkTimelinePositions();
        bookmarkManagerInstance.RefreshBookTooltips();
    }

    protected virtual void UpdateContainerData(ObjectContainer con, BaseObject obj) { }

    protected virtual void HandleObjectDelete(BaseObject obj, bool inCollection = false) { }

    protected virtual void HandleObjectSpawned(BaseObject obj, bool inCollection = false) { }

    protected virtual void HandleContainerSpawn(ObjectContainer container, BaseObject obj) { }

    protected virtual void HandleContainerDespawn(ObjectContainer container, BaseObject obj) { }

    public virtual void DoPostObjectsSpawnedWorkflow() { }

    public virtual void DoPostObjectsDeleteWorkflow() => RefreshPool();

    public abstract ObjectContainer CreateContainer();

    internal abstract void SubscribeToCallbacks();

    internal abstract void UnsubscribeToCallbacks();
}

public abstract class BeatmapObjectContainerCollection<T> : BeatmapObjectContainerCollection where T : BaseObject
{
    public event Action<T> OnObjectSpawned;
    public event Action<T> OnObjectDeleted;

    [Obsolete(
        "LoadedObjects allocates a copy of the backing list of objects. Please avoid this unless you absolutely cannot grab a more precise type.")]
    public override List<BaseObject> LoadedObjects => MapObjects.ConvertAll(it => it as BaseObject);

    public List<T> MapObjects = new();

    // Reuse GetBetween for the normal range so selection does not maintain a second binary-search implementation.
    public override void ForEachObjectBetweenSongBpmTime(
        float start,
        float end,
        Action<BeatmapObjectContainerCollection, BaseObject> callback)
    {
        if (MapObjects.Count == 0 || callback == null)
            return;

        var epsilon = Epsilon;
        var lowerBeat = start - epsilon;
        var upperBeat = end + epsilon;

        // Convert visual beat bounds back to the JsonTime ordering used by GetBetween and MapObjects.
        var map = BeatSaberSongContainer.Instance.Map;
        var lowerJsonTime = (float)map.SongBpmTimeToJsonTime(lowerBeat);
        var upperJsonTime = (float)map.SongBpmTimeToJsonTime(upperBeat);
        var objectsBetween = GetBetween(lowerJsonTime, upperJsonTime);

        // If we want to be able to select the back end of walls / arcs to add them to selection (rather than
        // selecting the very front of them), we'd need to implement a data-only interval index keyed by object
        // start and end times instead of scanning the full map or using loaded containers as a bounded proxy.
        // Keep the original open SongBpmTime bounds after conversion to avoid admitting epsilon-edge objects.
        for (var index = 0; index < objectsBetween.Length; index++)
        {
            var obj = objectsBetween[index];
            if (obj.SongBpmTime <= lowerBeat || obj.SongBpmTime >= upperBeat)
                continue;

            callback(this, obj);
        }
    }

    public Span<T> GetBetween(float jsonTime, float jsonTime2)
    {
        if (MapObjects.Count == 0) return Span<T>.Empty;

        // Considering we're only concerned with time, we'll use a time-based comparer here.
        var span = MapObjects.AsSpan();
        // ContainerCollectionTest.GetBetween needs inclusive stacked endpoints without a linear duplicate-time walk.
        var startIdx = span.LowerBoundBy(jsonTime, obj => obj.JsonTime);
        var endIdx = span.UpperBoundBy(jsonTime2, obj => obj.JsonTime);

        var length = endIdx - startIdx;

        return length > 0
            ? span.Slice(startIdx, length)
            : Span<T>.Empty;
    }

    /// <summary>
    ///     Given a list of objects, remove all existing ones that conflict.
    /// </summary>
    /// <param name="newObjects">Enumerable of new objects</param>
    public void RemoveConflictingObjects(IEnumerable<T> newObjects) => RemoveConflictingObjects(newObjects, out _);

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
    public virtual void RemoveConflictingObjects(IEnumerable<T> newObjects, out List<T> conflicting)
    {
        conflicting = new List<T>();

        foreach (var newObject in newObjects)
        {
            var localWindow = GetBetween(newObject.JsonTime - 0.1f, newObject.JsonTime + 0.1f);

            for (var i = 0; i < localWindow.Length; i++)
            {
                var obj = localWindow[i];

                if (obj.IsConflictingWith(newObject) && newObject != obj) conflicting.Add(obj);
            }
        }

        conflicting.ForEach(conflict => DeleteObject(conflict, false, false));
    }

    /// <inheritdoc/>
    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        var span = MapObjects.AsSpan();


        // Easier to process recyclings at the beginning, rather than try to deal with it later.
        if (forceRefresh)
            while (ObjectsWithContainers.Count > 0)
                RecycleContainer(ObjectsWithContainers[ObjectsWithContainers.Count - 1], indexInObjectsWithContainers: ObjectsWithContainers.Count - 1); // Clearing list from index 0 made this O(N^2) including N^2/2 position shifts.... clearing from the back to front is O(N) if we dont scan with .Remove
        else
        {
            var containersSpan = ObjectsWithContainers.AsSpan();

            // We need to go backwards since *technically* modifying spans in iteration is unsafe.
            for (var i = containersSpan.Length - 1; i >= 0; i--)
            {
                var obj = containersSpan[i];

                switch (obj)
                {
                    case BaseObstacle obs
                        when obs.SongBpmTime > upperBound || obs.SongBpmTime + obs.Duration < lowerBound:
                    case BaseSlider slider when slider.SongBpmTime > upperBound || slider.TailSongBpmTime < lowerBound:
                    case not null when (obj.SongBpmTime > upperBound || obj.SongBpmTime < lowerBound)
                        && !ShouldRetainContainerOutsideBounds(obj, lowerBound, upperBound):
                    case not null when !obj.HasMatchingTrack(TrackFilterID):
                        RecycleContainer(obj);
                        break;
                    default:
                        continue;
                }
            }
        }

        // lmao why do anything if we dont have objects to create containers for
        if (span.Length == 0) return;

        // RefreshPool_Window* tests require exact inclusive bounds so distant neighbors never leak into the visual pool.
        var startIdx = span.LowerBoundBy(lowerBound, obj => obj.SongBpmTime);
        var endIdx = span.UpperBoundBy(upperBound, obj => obj.SongBpmTime);

        var length = endIdx - startIdx;

        // All objects in our window defaultly get a container
        var windowSpan = span.Slice(startIdx, length);
        for (var i = 0; i < windowSpan.Length; i++)
        {
            var obj = windowSpan[i];

            if (obj.HasMatchingTrack(TrackFilterID)) CreateContainerFromPool(obj);
        }

        // this is a bit of a dirty check but i'd like this early return
        if (span[0] is not (BaseObstacle or BaseSlider)) return;

        // Handle special cases for certain objects that exist over a period of time (need startIdx here)
        for (var i = 0; i < startIdx; i++)
        {
            var obj = span[i];

            if (!obj.HasMatchingTrack(TrackFilterID)) continue;

            if (obj is BaseObstacle obs && obs.SongBpmTime < lowerBound && obs.SongBpmTime + obs.Duration >= lowerBound)
                CreateContainerFromPool(obj);
            else if (obj is BaseSlider slider
                && slider.SongBpmTime < lowerBound
                && slider.TailSongBpmTime >= lowerBound)
                CreateContainerFromPool(obj);
        }
    }

    // Let duration-owning collections retain an offscreen source whose rendered interval still intersects the pool window.
    //  EG transition ribbon endpoints should not unload while the ribbon is still visible.
    protected virtual bool ShouldRetainContainerOutsideBounds(BaseObject obj, float lowerBound, float upperBound)
        => false;

    /// <inheritdoc/>
    public override void DeleteObject(
        BaseObject obj,
        bool triggersAction = true,
        bool refreshesPool = true,
        string comment = "No comment.",
        bool inCollectionOfDeletes = false,
        bool deselect = true,
        bool triggerHandle = true)
    {
        if (obj is not T localObj) return;

        DeleteObject(localObj, triggersAction, refreshesPool, comment, inCollectionOfDeletes, deselect, triggerHandle);
    }

    /// <inheritdoc/>
    // TODO(Caeden): Overload to delete/spawn without recycling or creating a container
    public virtual void DeleteObject(
        T obj,
        bool triggersAction = true,
        bool refreshesPool = true,
        string comment = "No comment.",
        bool inCollectionOfDeletes = false,
        bool deselect = true,
        bool triggerHandle = true)
    {
        if (!TryBinarySearch(obj, out var index))
        {
            // Does not appear to be hit anymore, but keeping just in case.
            // Remove an orphaned visual even when rapid conflict replacement already removed its backing map object.
            if (obj.HasAttachedContainer && LoadedContainers.ContainsKey(obj))
            {
                Debug.LogError(
                    $"[BeatmapObjectCollection] Orphaned {ContainerType} container at beat {obj.JsonTime}.");
            }
            return;
        }

        // RefreshSpecialAngles teardown shares the indexed deletion workflow so ordinary deletion
        // and direct tail cleanup retain identical recycling, action, index, and callback behavior.
        DeleteObjectAt(
            index,
            triggersAction,
            refreshesPool,
            comment,
            inCollectionOfDeletes,
            deselect,
            triggerHandle);
    }

    // RefreshSpecialAngles requires direct index removal when its deliberately mutated note keys leave MapObjects unsorted.
    protected virtual void DeleteObjectAt(
        int index,
        bool triggersAction,
        bool refreshesPool,
        string comment,
        bool inCollectionOfDeletes,
        bool deselect,
        bool triggerHandle)
    {
        var deletedObj = MapObjects[index];

        RecycleContainer(deletedObj);

        MapObjects.RemoveAt(index);

        if (deselect) SelectionController.Deselect(deletedObj, triggersAction);

        if (triggersAction) BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(deletedObj, comment));

        // Update collection-owned indexes before pooling so dependent visuals rebuild from the post-deletion data.
        // If this is below RefreshPool, then boost event deletions don't update the lightshow preview until a time change recalculates.
        if (triggerHandle) HandleObjectDelete(deletedObj, inCollectionOfDeletes);

        if (refreshesPool) RefreshPool();

        OnObjectDeleted?.Invoke(deletedObj);
    }

    // RefreshSpecialAngles and UpToNRandomMapsInDefaultSongLocationsLoadWithoutExceptions require
    // authoritative tail removal plus one final pool refresh, avoiding binary search and repeated O(n) list shifts.
    public override void DeleteAllObjectsFromEnd(bool triggersAction = true)
    {
        while (MapObjects.Count > 0)
        {
            DeleteObjectAt(
                MapObjects.Count - 1,
                triggersAction,
                refreshesPool: false,
                "Delete all objects from end.",
                inCollectionOfDeletes: true,
                deselect: true,
                triggerHandle: true);
        }

        DoPostObjectsDeleteWorkflow();
    }

    // Removes object from MapObjects while retaining container and data in it
    public override void SilentRemoveObject(BaseObject obj)
    {
        if (obj is not T tObj) return;

        if (!TryBinarySearch(tObj, out var search)) return;

        MapObjects.RemoveAt(search);
    }

    protected bool TryBinarySearch(T tObj, out int index)
    {
        index = MapObjects.BinarySearch(tObj);

        // Unhappy path: Binary Search returns negative number
        if (index < 0)
        {
            // The objects are not in the collection, but are still being removed.
            // This could be because of ghost blocks, so let's try forcefully recycling that container.
            Debug.LogError($"This object at beat {tObj.JsonTime} is not in the collection and appears to be a ghost. Please report this.");
            return false;
        }

        // Happy path
        if (MapObjects[index] == tObj)
            return true;

        // United Mapper packets obviously cannot send the object reference so check comparison equality.
        if (MapObjects[index].CompareTo(tObj) == 0)
            return true;

        // Potentially unhappy path: Binary Search returns an object, but turns out to be the incorrect object.
        // We assume this is only going to happen for stacked objects so we march indexes to see if we can find it.
        var forwardIndex = index + 1;
        // Search every equal-time neighbor, including the first and final collection entries.
        while (forwardIndex < MapObjects.Count && MapObjects[forwardIndex].JsonTime <= tObj.JsonTime)
        {
            if (MapObjects[forwardIndex].CompareTo(tObj) == 0)
            {
                index = forwardIndex;
                return true;
            }

            forwardIndex++;
        }

        var backwardIndex = index - 1;
        while (backwardIndex >= 0 && MapObjects[backwardIndex].JsonTime >= tObj.JsonTime)
        {
            if (MapObjects[backwardIndex].CompareTo(tObj) == 0)
            {
                index = backwardIndex;
                return true;
            }

            backwardIndex--;
        }

        Debug.LogError("Binary Search returned no matching object. Please report this.");
        return false;
    }

    /// <inheritdoc/>
    public override void SpawnObject(
        BaseObject obj,
        out List<BaseObject> conflicting,
        bool removeConflicting = true,
        bool refreshesPool = true,
        bool inCollectionOfSpawns = false)
    {
        conflicting = new List<BaseObject>();

        if (obj is not T localObj) return;

        SpawnObject(localObj, out var localConflicting, removeConflicting, refreshesPool, inCollectionOfSpawns);

        for (var i = 0; i < localConflicting.Count; i++) conflicting.Add(localConflicting[i]);
    }

    /// <inheritdoc/>
    public void SpawnObject(
        T obj,
        bool removeConflicting = true,
        bool refreshesPool = true,
        bool inCollectionOfSpawns = false) =>
        SpawnObject(obj, out _, removeConflicting, refreshesPool, inCollectionOfSpawns);

    /// <inheritdoc/>
    // TODO(Caeden): Overload to delete/spawn without recycling or creating a container
    public void SpawnObject(
        T obj,
        out List<T> conflicting,
        bool removeConflicting = true,
        bool refreshesPool = true,
        bool inCollectionOfSpawns = false)
    {
        // Ensure customData is up to date before spawning
        obj.WriteCustom();

        //Debug.Log($"Spawning object with hash code {obj.GetHashCode()}");
        if (removeConflicting)
            RemoveConflictingObjects(new T[] { obj }, out conflicting);
        else
            conflicting = new List<T>();

        var search = MapObjects.BinarySearch(obj);
        var insertIdx = search >= 0 ? search : ~search;
        MapObjects.Insert(insertIdx, obj);

        HandleObjectSpawned(obj, inCollectionOfSpawns);
        OnObjectSpawned?.Invoke(obj);

        //Debug.Log($"Total object count: {LoadedObjects.Count}");
        if (refreshesPool) RefreshPool();
    }

    /// <inheritdoc/>
    public override bool ContainsObject(BaseObject obj) => obj is T localObj && ContainsObject(localObj);

    /// <inheritdoc/>
    public bool ContainsObject(T obj) => MapObjects.BinarySearch(obj) >= 0;
}
