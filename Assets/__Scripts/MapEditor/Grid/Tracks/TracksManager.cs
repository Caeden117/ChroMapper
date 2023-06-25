using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class TracksManager : MonoBehaviour
{
    [FormerlySerializedAs("TrackPrefab")][SerializeField] private GameObject trackPrefab;
    [FormerlySerializedAs("TracksParent")][SerializeField] private Transform tracksParent;
    [FormerlySerializedAs("events")][SerializeField] private EventGridContainer eventGrid;
    [SerializeField] private AudioTimeSyncController atsc;

    private readonly Dictionary<Vector3, Track> loadedTracks = new Dictionary<Vector3, Track>();
    private readonly Dictionary<string, TrackAnimator> animationTracks = new Dictionary<string, TrackAnimator>();

    private readonly List<BeatmapObjectContainerCollection> objectContainerCollections =
        new List<BeatmapObjectContainerCollection>();

    private float position;

    public float LowestRotation { get; private set; }
    public float HighestRotation { get; private set; }

    // Start is called before the first frame update
    private void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note));
        objectContainerCollections.Add(
            BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle));
        if (Settings.Instance.Load_MapV3)
        {
            objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc));
            objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain));
        }
        ObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
    }

    private void OnDestroy()
    {
        ObjectContainer.FlaggedForDeletionEvent -= FlaggedForDeletion;
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void FlaggedForDeletion(ObjectContainer obj, bool _, string __)
    {
        if (obj is EventContainer)
        {
            var e = obj.ObjectData as BaseEvent;
            if (e.IsLaneRotationEvent())
            {
                foreach (var collection in objectContainerCollections)
                    collection.RefreshPool();
            }
        }
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

        track = Instantiate(trackPrefab, tracksParent).GetComponent<Track>();
        track.gameObject.name = $"Track [{rotation.x}, {rotation.y}, {rotation.z}]";
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

    public TrackAnimator CreateAnimationTrack(string name)
    {
        if (animationTracks.TryGetValue(name, out var animator)) return animator;

        var obj = Instantiate(trackPrefab, tracksParent);
        obj.name = name;
        animator = obj.AddComponent<TrackAnimator>();
        animator.enabled = false;
        animator.Atsc = atsc;
        animator.track = obj.GetComponent<Track>();
        animationTracks.Add(name, animator);
        return animator;
    }

    public void ResetAnimationTracks()
    {
        foreach (var at in animationTracks)
        {
            at.Value.SetEvents(new List<BaseCustomEvent>());
            if (at.Value.animator != null)
            {
                GameObject.Destroy(at.Value.animator);
                at.Value.animator = null;
            }
        }
    }

    // Used for world rotation
    public Track CreateIndividualTrack(BaseGrid obj)
    {
        // TODO: This is the same math used for 90/360 tacks, but does it actually handle BPM changes?
        var potition = -1 * obj.JsonTime * EditorScaleController.EditorScale;
        var track = Instantiate(trackPrefab, tracksParent).GetComponent<Track>();
        track.UpdatePosition(potition);

        float rotation = GetRotationAtTime(obj.SongBpmTime);
        track.AssignRotationValue(obj.CustomWorldRotation ?? new Vector3(0, rotation, 0));
        track.gameObject.name = $"Track Object {obj.JsonTime}";
        return track;
    }

    public Track GetTrackAtTime(float beatInSongBpm)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        float rotation = GetRotationAtTime(beatInSongBpm);

        return CreateTrack(rotation);
    }

    public float GetRotationAtTime(float beatInSongBpm)
    {
        float rotation = 0;
        foreach (var rotationEvent in eventGrid.AllRotationEvents)
        {
            if (rotationEvent.SongBpmTime > beatInSongBpm + 0.001f) continue;
            if (Mathf.Approximately(rotationEvent.SongBpmTime, beatInSongBpm) &&
                rotationEvent.Type == (int)EventTypeValue.LateLaneRotation)
            {
                continue;
            }

            rotation += rotationEvent.GetRotationDegreeFromValue() ?? 0;
            if (rotation < LowestRotation) LowestRotation = rotation;
            if (rotation > HighestRotation) HighestRotation = rotation;
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
                if (container.Animator?.AnimatedTrack ?? false) continue;
                var track = GetTrackAtTime(container.ObjectData.SongBpmTime);
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

    public void
        UpdatePosition(
            float position) //Take our position from AudioTimeSyncController and broadcast that to every track.
    {
        this.position = position;
        foreach (var track in loadedTracks.Values) track.UpdatePosition(position);
    }
}
