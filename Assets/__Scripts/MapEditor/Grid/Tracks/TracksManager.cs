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
    [SerializeField] private List<Track> loadedTracks = new List<Track>();

    private List<BeatmapObjectContainerCollection> objectContainerCollections;

    // Start is called before the first frame update
    void Start()
    {
        objectContainerCollections = GetComponents<BeatmapObjectContainerCollection>()
            .Where(x => x is NotesContainer || x is ObstaclesContainer).ToList();
        BeatmapObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
    }

    private void FlaggedForDeletion(BeatmapObjectContainer obj)
    {
        //Refresh the tracks if we delete any rotation event
        if (obj is BeatmapEventContainer e && (e.eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || e.eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION))
            StartCoroutine(WaitThenRefreshTracks());
    }

    private IEnumerator WaitThenRefreshTracks()
    {
        yield return new WaitForEndOfFrame();
        RefreshTracks();
    }

    private void OnDestroy()
    {
        BeatmapObjectContainer.FlaggedForDeletionEvent -= FlaggedForDeletion;
    }

    public Track CreateTrack(int rotation, int multiple = 15)
    {
        Track track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
        track.gameObject.name = $"Track {multiple * rotation}";
        track.AssignRotationValue(multiple * rotation, false);
        return track;
    }

    public void RefreshTracks()
    {
        if (!loadedTracks.Any())
        {
            for (int i = 0; i < 24; i++)
            {
                Track track = CreateTrack(i);
                loadedTracks.Add(track);
            }
        }
        else foreach (Track track in loadedTracks) track.AssignTempRotation(0);
        List<BeatmapEventContainer> allRotationEvents = events.LoadedContainers.Cast<BeatmapEventContainer>().Where(x =>
            x.eventData.IsRotationEvent).OrderBy(x => x.eventData._time).ToList();

        List<BeatmapObjectContainer> allObjects = new List<BeatmapObjectContainer>();
        objectContainerCollections.ForEach(x => allObjects.AddRange(x.LoadedContainers));

        //Filter out bad rotation events (Legacy MM BPM changes, custom platform events using Events 14 and 15, etc.)
        allRotationEvents = allRotationEvents.Where(x => x.eventData._value > 0 &&
            x.eventData._value < MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.Count()).ToList();

        if (allRotationEvents.Count == 0)
        {
            Track track = loadedTracks.First();
            foreach (BeatmapObjectContainer obj in allObjects) track.AttachContainer(obj, 0);
            foreach (Track loadedTrack in loadedTracks)
                loadedTrack.AssignRotationValue(loadedTrack.RotationValue, Settings.Instance.RotateTrack);
            return;
        }

        //Assign objects up to the first rotation event.
        int rotation = 0;
        List<BeatmapObjectContainer> firstObjects = allObjects.Where(x =>
            (x.objectData._time < allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
            (x.objectData._time <= allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        ).ToList();
        firstObjects.ForEach(x => loadedTracks.First().AttachContainer(x, rotation));

        //Assign objects in between each rotation event according to their types.
        for (int i = 0; i < allRotationEvents.Count - 1; i++)
        {
            float firstTime = allRotationEvents[i].eventData._time;
            float secondTime = allRotationEvents[i + 1].eventData._time;
            rotation += MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES[allRotationEvents[i].eventData._value];
            int localRotation = betterModulo(rotation, 360);
            List<BeatmapObjectContainer> rotatedObjects = allObjects.Where(x =>
                ((x.objectData._time >= firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time > firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)) &&
                ((x.objectData._time < secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time <= secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION))
                ).ToList();
            Track track = loadedTracks.Where(x => x.RotationValue == localRotation).FirstOrDefault();
            rotatedObjects.ForEach(x => track?.AttachContainer(x, rotation));
        }

        //Finally, assign objects to the last rotation event.
        rotation += MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES[allRotationEvents.Last().eventData._value];
        int lastRotationType = allRotationEvents.Last().eventData._type;
        List<BeatmapObjectContainer> lastObjects = allObjects.Where(x =>
            (x.objectData._time >= allRotationEvents.Last().eventData._time && lastRotationType == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
            (x.objectData._time > allRotationEvents.Last().eventData._time && lastRotationType == MapEvent.EVENT_TYPE_LATE_ROTATION)
            ).ToList();
        Track lastTrack = loadedTracks.Where(x => x.RotationValue == betterModulo(rotation, 360)).FirstOrDefault();
        lastObjects.ForEach(x => lastTrack?.AttachContainer(x, rotation));

        //Refresh all of the tracks
        foreach (Track track in loadedTracks)
            track.AssignRotationValue(track.RotationValue, Settings.Instance.RotateTrack);
    }

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    public void UpdatePosition(float position)
    {
        foreach (Track track in loadedTracks) track.UpdatePosition(position);
    }

    public Track GetTrackForRotationValue(float rotation)
    {
        int roundedRotation = Mathf.RoundToInt(rotation);
        int localRotation = betterModulo(roundedRotation, 360);
        return loadedTracks.Where(x => x.RotationValue == localRotation).FirstOrDefault();
    }
}
