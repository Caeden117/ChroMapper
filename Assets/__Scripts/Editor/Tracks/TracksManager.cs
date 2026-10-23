using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class TracksManager : MonoBehaviour
{
    [SerializeField] private Track trackPrefab;
    [SerializeField] private Transform tracksParent;
    [SerializeField] private ModelUpdateQueueManager modelUpdateQueueManager;
    [SerializeField] private RotationEventGridContainer rotationEventGridContainer;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    public CameraManager CameraManager => cameraManager;
    private bool noodleObjectCallbacks;
    private bool colorBirthCacheSubscribed;

    internal float GetColorBirthTime(BaseGrid obj)
    {
        if (!colorBirthCacheSubscribed)
        {
            vNjsProvider.SpawnAheadTimeChanged += RefreshColorBirthTimes;
            colorBirthCacheSubscribed = true;
        }
        var ahead = noodleObjectCallbacks
            ? obj.HalfJumpDuration + atsc.GetBeatFromSeconds(0.5f)
            : atsc.GetBeatFromSeconds(vNjsProvider.SpawnAheadTime);
        return obj.SongBpmTime - ahead;
    }

    private void RefreshColorBirthTimes()
    {
        foreach (var collection in objectContainerCollections)
        {
            if (collection is not ObstacleGridContainer and not ArcGridContainer)
                continue;

            foreach (var container in collection.LoadedContainers.Values)
                container.Animator.RefreshColorBirthTime((BaseGrid)container.ObjectData);

        }
    }

    private readonly Stack<Track> trackPool = new();
    private readonly Dictionary<Vector3, Track> loadedTracks = new();
    private readonly Dictionary<string, TrackAnimator> animationTracks = new();
    public readonly Dictionary<string, List<ParametricBloomFogLightController>> TrackToBloomFogLights = new();
    private readonly Dictionary<Transform, ObjectAnimator> environmentAnimators = new();
    internal readonly ObjectPathAnimationRegistry PathAnimations = new();

    public readonly HashSet<string> FogComponentTracks = new();
    public int ComponentRevision { get; private set; }
    public Dictionary<string, List<BaseCustomEvent>> CustomEventsByTrack = new();
    public IEnumerable<TrackAnimator> AnimationTracks => animationTracks.Values;
    public int AnimationRevision { get; private set; }

    private void OnAnimationChildrenChanged() => AnimationRevision++;

    private readonly List<BeatmapObjectContainerCollection> objectContainerCollections = new();

    private float position;

    public bool IsV2Map { get; set; }

    private float lowestRotation;
    private float highestRotation;

    // Bind scene-owned camera and NJS references once so base-provider evaluation can use them without per-frame scene searches.
    private void Awake()
    {
        if (cameraManager == null) cameraManager = FindFirstObjectByType<CameraManager>();
        BaseProviderManager.BindSceneReferences(cameraManager, vNjsProvider, atsc);
    }

    private void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note));
        objectContainerCollections.Add(
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain));
    }

    private Track GetOrCreateTrack(Transform parent)
    {
        var track = trackPool.Count > 0 ? trackPool.Pop() : Instantiate(trackPrefab);
        track.gameObject.SetActive(true);
        track.SelfTransform.SetParent(parent, false);
        return track;
    }

    public void Remove(Track track)
    {
        track.gameObject.SetActive(false);
        track.SelfTransform.SetParent(null, false);
        track.ResetData();
        trackPool.Push(track);
    }

    /// <summary>
    ///     Create a new <see cref="Track" /> with the specified global rotation. If a track already exists with that rotation,
    ///     it will simply return that track.
    /// </summary>
    /// <param name="rotation">Global euler rotation</param>
    /// <returns></returns>
    public Track CreateTrack(Vector3 rotation)
    {
        if (loadedTracks.TryGetValue(rotation, out var track)) return track;

        track = GetOrCreateTrack(tracksParent);
        track.gameObject.name = $"Track [{rotation.x}, {rotation.y}, {rotation.z}]";

        track.vNjsProvider = vNjsProvider;
        track.enabled = true;
        track.AssignRotationValue(rotation);
        track.UpdatePosition(position);

        loadedTracks.Add(rotation, track);
        return track;
    }

    /// <summary>
    ///     Create a new <see cref="Track" /> with the specified rotation around the Y axis.
    ///     It simply calls <see cref="CreateTrack(Vector3)" /> with a Vector3 of (0, <paramref name="rotation" />, 0)/>
    /// </summary>
    /// <param name="rotation">Y-axis rotation.</param>
    public Track CreateTrack(float rotation)
    {
        var roundedRotation = FloatModulo(rotation, 360);
        var vectorRotation = new Vector3(0, roundedRotation, 0);
        return CreateTrack(vectorRotation);
    }

    public bool TryGetAnimationTrack(string name, out TrackAnimator track) =>
        animationTracks.TryGetValue(name, out track);

    internal ObjectAnimator CreateEnvironmentAnimator(GameObject owner, Transform target)
    {
        if (environmentAnimators.TryGetValue(target, out var previous))
        {
            // Rebinding replaces the old property writers without resetting the enhanced transform.
            previous.enabled = false;
            Destroy(previous);
        }
        var animator = owner.AddComponent<ObjectAnimator>();
        animator.TracksManager = this;
        environmentAnimators[target] = animator;
        return animator;
    }

    public TrackAnimator GetAnimationTrack(string name)
    {
        if (animationTracks.TryGetValue(name, out var animator)) return animator;

        var track = GetOrCreateTrack(null);
        track.gameObject.name = name;

        animator = track.gameObject.GetOrAddComponent<TrackAnimator>();
        animator.enabled = false;
        animator.Atsc = atsc;
        animator.Track = track;
        animator.Track.vNjsProvider = vNjsProvider;
        animator.Track.enabled = true;
        animator.ChildrenChanged += OnAnimationChildrenChanged;

        animationTracks.Add(name, animator);
        AnimationRevision++;
        return animator;
    }

    public void PushHeldValuesToChild(JSONNode customTrack, ObjectAnimator child)
    {
        switch (customTrack)
        {
            case JSONString name:
                if (animationTracks.TryGetValue(name.Value, out var animator))
                    animator.PushToChild(child);
                break;
            case JSONArray tracks:
                foreach (var node in tracks.Children)
                {
                    if (animationTracks.TryGetValue((string)node, out var multi))
                        multi.PushToChild(child);
                }
                break;
        }
    }

    // Resolve fog component ownership when environment enhancements attach their tracks so unrelated tracks cannot animate fog.
    public void BindFogComponentTarget(string name)
    {
        if (FogComponentTracks.Add(name)) ComponentRevision++;
    }

    public void RegisterBloomFogLights(string name, IEnumerable<ParametricBloomFogLightController> lights)
    {
        if (!TrackToBloomFogLights.TryGetValue(name, out var targets))
            TrackToBloomFogLights.Add(name, targets = new());
        foreach (var light in lights)
        {
            if (light == null || targets.Contains(light)) continue;
            targets.Add(light);
            ComponentRevision++;
        }
    }

    public void ClearComponentTargets()
    {
        FogComponentTracks.Clear();
        TrackToBloomFogLights.Clear();
        ComponentRevision++;
    }

    private void OnDestroy()
    {
        if (colorBirthCacheSubscribed && vNjsProvider != null)
            vNjsProvider.SpawnAheadTimeChanged -= RefreshColorBirthTimes;

        if (atsc == null) return;
        foreach (var animator in animationTracks.Values)
        {
            // Track GameObjects can be destroyed before this managers OnDestroy. Skip destroyed animators
            // before accessing their components.
            if (animator == null)
                continue;

            animator.ChildrenChanged -= OnAnimationChildrenChanged;
            var fogAnimator = animator.GetComponent<FogAnimator>();
            if (fogAnimator != null)
            {
                atsc.OnTimeChangedEarly -= fogAnimator.PushOnStoppedTimeChanged;
            }

            var tubeBloomAnimator = animator.GetComponent<TubeBloomAnimator>();
            if (tubeBloomAnimator != null)
            {
                atsc.OnTimeChangedEarly -= tubeBloomAnimator.PushOnStoppedTimeChanged;
            }
        }
    }

    // Used for world rotation
    public Track CreateIndividualTrack(BaseGrid obj)
    {
        // TODO: This is the same math used for 90/360 tacks, but does it actually handle BPM changes?
        var pos = -1 * obj.JsonTime * EditorScaleController.EditorScale;
        var track = GetOrCreateTrack(tracksParent);
        track.gameObject.name = $"Track Object {obj.JsonTime}";

        track.vNjsProvider = vNjsProvider;
        track.enabled = true;
        track.UpdatePosition(pos);

        var rotation = BeatSaberSongContainer.Instance.Map.MajorVersion == 4
            ? obj.Rotation
            : GetRotationAtTime(obj.SongBpmTime);
        track.AssignRotationValue(obj.CustomWorldRotation ?? new Vector3(0, rotation, 0));
        return track;
    }

    // Track parenting moves enhancement targets into the mapper scene, where environment unload leaves them
    // alive. Destroy them before ResetAnimationTracks clears the Children lists that hold their references.
    public void DestroyTrackBoundEnvironmentObjects()
    {
        foreach (var animator in animationTracks.Values)
        {
            animator.DestroyTrackBoundEnvironmentObjects();
        }
    }

    // Reset transforms, point definitions, and parent links before loading new custom events and enhancements
    // so reused tracks start with fresh map state.
    public void ResetAnimationTracks()
    {
        var difficulty = BeatSaberSongContainer.Instance.MapDifficultyInfo;
        noodleObjectCallbacks = difficulty.CustomRequirements.Contains("Noodle Extensions");
        environmentAnimators.Clear();
        PathAnimations.Reset();
        ClearComponentTargets();
        CustomEventsByTrack.Clear();
        AnimationRevision++;
        // Reset shared smoothed-base state even when the incoming map has no named animation tracks.
        BaseProviderManager.ResetForMapLoad();

        foreach (var animator in animationTracks.Values)
        {
            animator.ResetForMapLoad();

            if (animator.Animator != null)
            {
                animator.Animator.SetTrackParentMapVersion(IsV2Map);
            }

            // Reset component point definitions, targets, and captured baselines with their named track so
            // the next map cannot restore the previous map's animated state.
            var fogAnimator = animator.GetComponent<FogAnimator>();
            if (fogAnimator != null)
            {
                fogAnimator.ResetForMapLoad();
            }

            var tubeBloomAnimator = animator.GetComponent<TubeBloomAnimator>();
            if (tubeBloomAnimator != null)
            {
                tubeBloomAnimator.ResetForMapLoad();
            }
        }
    }

    public void ResetTrackParent(string name)
    {
        var track = GetAnimationTrack(name);
        foreach (var parent in track.Parents)
            parent.RemoveChild(track.Animator);

        track.Parents.Clear();
        if (track.Animator != null)
        {
            track.Animator.ResetData();
            track.Animator.enabled = false;
        }

        track.Track.SelfTransform.SetParent(tracksParent, false);
        track.Track.SelfTransform.localPosition = Vector3.zero;
        track.Track.SelfTransform.localRotation = Quaternion.identity;
        track.Track.SelfTransform.localScale = Vector3.one;
        track.Track.ObjectParentTransform.localPosition = Vector3.zero;
        track.Track.ObjectParentTransform.localRotation = Quaternion.identity;
        track.Track.ObjectParentTransform.localScale = Vector3.one;
        foreach (var child in track.Children)
            child.ClearDirectTrackParent();

        track.OnChildrenChanged();
    }

    public void RefreshBpmTiming(float jsonTime)
    {
        PathAnimations.RefreshBpmTiming();
        var changedSongTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
        foreach (var collection in objectContainerCollections)
        {
            foreach (var pair in collection.LoadedContainers)
            {
                var data = (BaseGrid)pair.Key;
                if (pair.Value.Animator.enabled
                    && (data.JsonTime >= jsonTime || data.DespawnSongBpmTime >= changedSongTime))
                {
                    pair.Value.Animator.AttachToObject(data);
                }
            }
        }

        foreach (var track in animationTracks.Values)
        {
            foreach (var property in track.AnimatedProperties.Values)
            {
                property.RefreshBpmTiming();
            }

            track.PushOnStoppedTimeChanged();
            var fog = track.GetComponent<FogAnimator>();
            if (fog != null)
            {
                fog.RefreshBpmTiming();
            }

            var tubeBloom = track.GetComponent<TubeBloomAnimator>();
            if (tubeBloom != null)
            {
                tubeBloom.RefreshBpmTiming();
            }
        }
    }

    public Track GetTrackAtTime(float beatInSongBpm, int rotation)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        var rot = BeatSaberSongContainer.Instance.Map.MajorVersion == 4
            ? rotation
            : GetRotationAtTime(beatInSongBpm);

        return CreateTrack(rot);
    }

    public Track GetTrackAtTime(float beatInSongBpm)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        var rot = GetRotationAtTime(beatInSongBpm);

        return CreateTrack(rot);
    }

    public float GetRotationAtTime(float beatInSongBpm)
    {
        float rotation = 0;
        foreach (var rotationEvent in rotationEventGridContainer.MapObjects)
        {
            if (rotationEvent.SongBpmTime > beatInSongBpm + 0.001f) continue;
            if (Mathf.Approximately(rotationEvent.SongBpmTime, beatInSongBpm)
                && rotationEvent.Type == (int)EventTypeValue.LateRotationEventType)
                continue;

            rotation += rotationEvent.Rotation;
            if (rotation < lowestRotation) lowestRotation = rotation;
            if (rotation > highestRotation) highestRotation = rotation;
        }

        return rotation;
    }

    public void RefreshTracks()
    {
        foreach (var collection in objectContainerCollections)
        {
            foreach (var container in collection.LoadedContainers.Values)
            {
                if (container is ObstacleContainer obstacle && obstacle.IsRotatedByNoodleExtensions) continue;
                if (container.Animator != null && container.Animator.AnimatedTrack) continue;
                var track = GetTrackAtTime(
                    container.ObjectData.SongBpmTime,
                    container.ObjectData is BaseGrid grid ? grid.Rotation : 0);
                track.AttachContainer(container);
                container.UpdateGridPosition();
            }
        }
    }

    private float FloatModulo(float x, float m) =>
        //float largestFactor = Mathf.Floor(x / m); //Same functionality as x % m but with floats cuz fuck you
        //float regularModulo = x - largestFactor * m;

        //float moduloAddBase = regularModulo + m;
        //float betterLargestFactor = Mathf.Floor(moduloAddBase / m);
        //float betterModulo = moduloAddBase - betterLargestFactor * m;
        x - (Mathf.Floor(x / m) * m) + m - (Mathf.Floor((x - (Mathf.Floor(x / m) * m) + m) / m) * m);

    //Take our position from AudioTimeSyncController and broadcast that to every track.
    public void UpdatePosition(float position)
    {
        this.position = position;
        foreach (var track in loadedTracks.Values) track.UpdatePosition(position);
    }

    public void FlushModelUpdates()
    {
        if (modelUpdateQueueManager != null) modelUpdateQueueManager.Flush();
    }

    public void QueueModelUpdate(string track)
    {
        if (!animationTracks.TryGetValue(track, out var animator)) return;
        for (var i = 0; i < animator.Children.Count; i++)
        {
            var animationTrack = animator.Children[i].AnimationTrack;
            if (animationTrack != null && animationTrack.GridContainer != null)
                modelUpdateQueueManager.Add(animationTrack.GridContainer);
        }
    }
}
