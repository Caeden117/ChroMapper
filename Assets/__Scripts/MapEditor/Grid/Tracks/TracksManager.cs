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

    private Dictionary<Vector3, Track> loadedTracks = new Dictionary<Vector3, Track>();
    private List<BeatmapObjectContainerCollection> objectContainerCollections = new List<BeatmapObjectContainerCollection>();
    private float position = 0;

    public float LowestRotation { get; private set; } = 0;
    public float HighestRotation { get; private set; } = 0;

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
    /// Create a new <see cref="Track"/> with the specified global rotation. If a track already exists with that rotation, it will simply return that track.
    /// </summary>
    /// <param name="rotation">Global euler rotation</param>
    /// <returns></returns>
    public Track CreateTrack(Vector3 rotation)
    {
        if (loadedTracks.TryGetValue(rotation, out Track track))
        {
            return track;
        }
        else
        {
            track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
            track.gameObject.name = $"Track [{rotation.x}, {rotation.y}, {rotation.z}]";
            track.AssignRotationValue(rotation);
            track.UpdatePosition(position);
            loadedTracks.Add(rotation, track);
            return track;
        }
    }

    /// <summary>
    /// Create a new <see cref="Track"/> with the specified rotation around the Y axis.
    /// It simply calls <see cref="CreateTrack(Vector3)"/> with a Vector3 of (0, <paramref name="rotation"/>, 0)/>
    /// </summary>
    /// <param name="rotation">Y-axis rotation.</param>
    public Track CreateTrack(float rotation)
    {
        float roundedRotation = FloatModulo(rotation, 360);
        Vector3 vectorRotation = new Vector3(0, roundedRotation, 0);
        return CreateTrack(vectorRotation);
    }

    public Track GetTrackAtTime(float beatInSongBPM)
    {
        if (!Settings.Instance.RotateTrack) return CreateTrack(0);
        float rotation = 0;
        foreach (MapEvent rotationEvent in events.AllRotationEvents)
        {
            if (rotationEvent._time > beatInSongBPM + 0.001f) continue;
            if (Mathf.Approximately(rotationEvent._time, beatInSongBPM) && rotationEvent._type == MapEvent.EVENT_TYPE_LATE_ROTATION) continue;

            rotation += rotationEvent.GetRotationDegreeFromValue() ?? 0;
            if (rotation < LowestRotation) LowestRotation = rotation;
            if (rotation > HighestRotation) HighestRotation = rotation;
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
}
