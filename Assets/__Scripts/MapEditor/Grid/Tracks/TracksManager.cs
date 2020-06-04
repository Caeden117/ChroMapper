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
    private float position = 0;

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
        float roundedRotation = FloatModulo(rotation, 360);
        if (loadedTracks.TryGetValue(roundedRotation, out Track track))
        {
            return track;
        }
        else
        {
            track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
            track.gameObject.name = $"Track {roundedRotation}";
            track.AssignRotationValue(roundedRotation);
            track.UpdatePosition(position);
            loadedTracks.Add(roundedRotation, track);
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
                //container.UpdateGridPosition();
            }
        }
    }

    private float FloatModulo(float x, float m)
    {
        //float largestFactor = Mathf.Floor(x / m); //Same functionality as x % m but with floats cuz fuck you
        //float regularModulo = x - largestFactor * m;

        //float moduloAddBase = regularModulo + m;
        //float betterLargestFactor = Mathf.Floor(moduloAddBase / m);
        //float betterModulo = moduloAddBase - betterLargestFactor * m;
        return ((x - (Mathf.Floor(x / m)) * m) + m) - Mathf.Floor(((x - (Mathf.Floor(x / m)) * m) + m) / m) * m;
    }

    public void UpdatePosition(float position) //Take our position from AudioTimeSyncController and broadcast that to every track.
    {
        this.position = position;
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
