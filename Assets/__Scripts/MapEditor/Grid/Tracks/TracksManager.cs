using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TracksManager : MonoBehaviour
{
    [SerializeField] private GameObject TrackPrefab;
    [SerializeField] private Transform TracksParent;
    [SerializeField] private EventsContainer events;
    [SerializeField] private AudioTimeSyncController atsc;

    private Dictionary<float, Track> loadedTracks = new Dictionary<float, Track>();
    private List<BeatmapObjectContainerCollection> objectContainerCollections = new List<BeatmapObjectContainerCollection>();

    // Start is called before the first frame update
    void Start()
    {
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE));
        objectContainerCollections.Add(BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE ));
        BeatmapObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
    }

    private void FlaggedForDeletion(BeatmapObjectContainer obj, bool _, string __)
    {
        if (obj is BeatmapEventContainer)
        {
            MapEvent e = obj.objectData as MapEvent;
            if (e._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || e._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
            {
                foreach (BeatmapObjectContainerCollection collection in objectContainerCollections)
                {
                    collection.RefreshPool();
                }
            }
        }
    }

    private void OnDestroy()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= FlaggedForDeletion;
    }

    /// <summary>
    /// Create a new <see cref="Track"/> with the specified local rotation. If a track already exists there, it will simply return that track.
    /// </summary>
    /// <param name="rotation">A local rotation from 0-359 degrees.</param>
    /// <returns>A newly created track at the specified local rotation. If any track already exists with that local rotation, it returns that instead.</returns>
    public Track CreateTrack(float rotation)
    {
        if (loadedTracks.TryGetValue(rotation, out Track track))
        {
            return track;
        }
        else
        {
            track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
            track.gameObject.name = $"Track {rotation}";
            track.AssignRotationValue(rotation);
            loadedTracks.Add(rotation, track);
            return track;
        }
    }

    public Track GetTrackAtTime(float beatInSongBPM)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        float rotation = 0;
        foreach (BeatmapObject obj in events.UnsortedObjects)
        {
            if (!(obj is MapEvent rotationEvent)) continue;
            if (rotationEvent._time > beatInSongBPM) break;
            if (!rotationEvent.IsRotationEvent) continue;
            if (rotationEvent._time == beatInSongBPM && rotationEvent._type == MapEvent.EVENT_TYPE_LATE_ROTATION) continue;
            
            rotation += rotationEvent.GetRotationDegreeFromValue() ?? 0;
        }
        return CreateTrack(rotation);
    }

    public void RefreshTracks()
    {
        foreach (BeatmapObjectContainerCollection collection in objectContainerCollections)
        {
            foreach (BeatmapObjectContainer container in collection.LoadedContainers.Values)
            {
                if (container is BeatmapObstacleContainer obstacle && obstacle.IsRotatedByNoodleExtensions) continue;
                Track track = GetTrackAtTime(container.objectData._time);
                track.AttachContainer(container);
                container.UpdateGridPosition();
            }
        }
    }
    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow
    public void UpdatePosition(float position) //Take our position from AudioTimeSyncController and broadcast that to every track.
    {
        foreach (Track track in loadedTracks.Values) track.UpdatePosition(position);
    }

    /// <summary>
    /// Grab a <see cref="Track"/> with the specific rotation.
    /// </summary>
    /// <param name="rotation">Local Rotation.</param>
    /// <returns>The track with the matching local rotation, or <see cref="null"/> if there is none.</returns>
    public Track GetTrackForRotationValue(float rotation)
    {
        return loadedTracks.TryGetValue(rotation, out Track track) ? track : null;
    }
}
