﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Serialization;

public class TracksManager : MonoBehaviour
{
    [FormerlySerializedAs("TrackPrefab")] [SerializeField] private GameObject trackPrefab;
    [FormerlySerializedAs("TracksParent")] [SerializeField] private Transform tracksParent;
    [SerializeField] private EventsContainer events;
    [SerializeField] private AudioTimeSyncController atsc;

    private readonly Dictionary<Vector3, Track> loadedTracks = new Dictionary<Vector3, Track>();

    private readonly List<BeatmapObjectContainerCollection> objectContainerCollections =
        new List<BeatmapObjectContainerCollection>();

    private float position;

    public float LowestRotation { get; private set; }
    public float HighestRotation { get; private set; }

    // Start is called before the first frame update
    private void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Note));
        objectContainerCollections.Add(
            BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle));
        BeatmapObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
    }

    private void OnDestroy() => BeatmapObjectContainer.FlaggedForDeletionEvent -= FlaggedForDeletion;

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Discarding multiple variables")]
    private void FlaggedForDeletion(BeatmapObjectContainer obj, bool _, string __)
    {
        if (obj is BeatmapEventContainer)
        {
            var e = obj.ObjectData as MapEvent;
            if (e.Type == MapEvent.EventTypeEarlyRotation || e.Type == MapEvent.EventTypeLateRotation)
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

    public Track GetTrackAtTime(float beatInSongBpm)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        float rotation = 0;
        foreach (var rotationEvent in events.AllRotationEvents)
        {
            if (rotationEvent.Time > beatInSongBpm + 0.001f) continue;
            if (Mathf.Approximately(rotationEvent.Time, beatInSongBpm) &&
                rotationEvent.Type == MapEvent.EventTypeLateRotation)
            {
                continue;
            }

            rotation += rotationEvent.GetRotationDegreeFromValue() ?? 0;
            if (rotation < LowestRotation) LowestRotation = rotation;
            if (rotation > HighestRotation) HighestRotation = rotation;
        }

        return CreateTrack(rotation);
    }

    public void RefreshTracks()
    {
        foreach (var collection in objectContainerCollections)
        {
            foreach (var container in collection.LoadedContainers.Values)
            {
                if (container is BeatmapObstacleContainer obstacle && obstacle.IsRotatedByNoodleExtensions) continue;
                var track = GetTrackAtTime(container.ObjectData.Time);
                track.AttachContainer(container);
                //container.UpdateGridPosition();
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
