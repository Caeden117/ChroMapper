using System.Collections.Generic;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class TracksManager : MonoBehaviour
{
    [SerializeField] private Track trackPrefab;
    [SerializeField] private Transform tracksParent;
    [SerializeField] private RotationEventGridContainer rotationEventGridContainer;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private VariableNJSProvider vNjsProvider;

    public CameraManager CameraManager => cameraManager;

    private readonly Stack<Track> trackPool = new();
    private readonly Dictionary<Vector3, Track> loadedTracks = new();
    private readonly Dictionary<string, TrackAnimator> animationTracks = new();

    private readonly List<BeatmapObjectContainerCollection> objectContainerCollections = new();

    private float position;

    public bool IsV2Map { get; set; }

    private float lowestRotation;
    private float highestRotation;

    // Bind scene-owned camera and NJS references once so base-provider evaluation can use them without per-frame scene searches.
    private void Awake() => BaseProviderManager.BindSceneReferences(cameraManager, vNjsProvider);

    private void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note));
        objectContainerCollections.Add(
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain));
    }

    private Track GetOrCreateTrack()
    {
        var track = trackPool.Count > 0 ? trackPool.Pop() : Instantiate(trackPrefab);
        track.gameObject.SetActive(true);
        track.SelfTransform.SetParent(tracksParent, false);
        return track;
    }

    public void Remove(Track track)
    {
        track.gameObject.SetActive(false);
        track.SelfTransform.SetParent(tracksParent, false);
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

        track = GetOrCreateTrack();
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

    public TrackAnimator GetAnimationTrack(string name)
    {
        if (animationTracks.TryGetValue(name, out var animator)) return animator;

        var track = GetOrCreateTrack();
        track.gameObject.name = name;

        animator = track.gameObject.GetOrAddComponent<TrackAnimator>();
        animator.enabled = false;
        animator.Atsc = atsc;
        animator.Track = track;
        animator.Track.vNjsProvider = vNjsProvider;
        animator.Track.enabled = true;
        atsc.OnTimeChangedEarly += animator.PushOnStoppedTimeChanged;

        animationTracks.Add(name, animator);
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
        if (!animationTracks.TryGetValue(name, out var track))
            return;
        var fog = track.GetComponent<FogAnimator>();
        if (fog != null)
            fog.BindFogComponentTarget();
    }

    private void OnDestroy()
    {
        if (atsc == null) return;
        foreach (var animator in animationTracks.Values)
        {
            // Track GameObjects can be destroyed before this managers OnDestroy. Skip destroyed animators
            // before accessing their components.
            if (animator == null)
                continue;

            atsc.OnTimeChangedEarly -= animator.PushOnStoppedTimeChanged;
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
        var track = GetOrCreateTrack();
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
}
