using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TracksManager : MonoBehaviour
{
    [SerializeField] private GameObject TrackPrefab;
    [SerializeField] private Transform TracksParent;
    [SerializeField] private EventsContainer events;

    private Dictionary<int, Track> loadedTracks = new Dictionary<int, Track>();

    private List<BeatmapObjectContainerCollection> objectContainerCollections;

    // Start is called before the first frame update
    void Start()
    {
        objectContainerCollections = GetComponents<BeatmapObjectContainerCollection>()
            .Where(x => x is NotesContainer || x is ObstaclesContainer).ToList();
        BeatmapObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
    }

    private void FlaggedForDeletion(BeatmapObjectContainer obj, bool _, string __)
    {
        //Refresh the tracks if we delete any rotation event
        if (obj is BeatmapEventContainer e && e.eventData.IsRotationEvent)
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

    /// <summary>
    /// Create a new <see cref="Track"/> with the specified local rotation. If a track already exists there, it will simply return that track.
    /// </summary>
    /// <param name="rotation">A local rotation from 0-359 degrees.</param>
    /// <returns>A newly created track at the specified local rotation. If any track already exists with that local rotation, it returns that instead.</returns>
    public Track CreateTrack(int rotation)
    {
        if (loadedTracks.TryGetValue(rotation, out Track track))
        {
            track.AssignRotationValue(rotation);
            if (!Settings.Instance.RotateTrack) track.AssignTempRotation(0);
            return track;
        }
        else
        {
            track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
            track.gameObject.name = $"Track {rotation}";
            track.AssignRotationValue(rotation);
            if (!Settings.Instance.RotateTrack) track.AssignTempRotation(0);
            loadedTracks.Add(rotation, track);
            return track;
        }
    }

    public void RefreshTracks()
    {
        //We then grab our rotation events, then sort by time and type so that Type 14 events are always before Type 15 events.
        //Type 14 should always trigger before Type 15, since they effect things at the same exact time.
        //If Type 15 was before Type 14, there will be inaccuracies with how objects were rotated.
        List<BeatmapEventContainer> allRotationEvents = events.LoadedContainers.Cast<BeatmapEventContainer>().Where(x =>
            x.eventData.IsRotationEvent).OrderBy(x => x.eventData._time).ThenBy(x => x.eventData._type).ToList();

        //Grab every Note and Obstacle object we have, since those are the objects being effected by rotation
        List<BeatmapObjectContainer> allObjects = new List<BeatmapObjectContainer>();
        objectContainerCollections.ForEach(x => allObjects.AddRange(x.LoadedContainers));

        //Filter out bad rotation events (Legacy MM BPM changes, custom platform events using Events 14 and 15, etc.)
        allRotationEvents = allRotationEvents.Where(x => x.eventData.GetRotationDegreeFromValue() != null).ToList();

        //If there is no rotation events at all, assign them all to the first track and call it a day.
        //No need for extra calculations.
        if (allRotationEvents.Count == 0)
        {
            Track track = CreateTrack(0);
            foreach (BeatmapObjectContainer obj in allObjects) track.AttachContainer(obj);
            return;
        }

        //Assign objects up to the first rotation event. These should be directly in front of the player.
        int rotation = 0;
        List<BeatmapObjectContainer> firstObjects = allObjects.Where(x =>
            (x.objectData._time < allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
            (x.objectData._time <= allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        ).ToList();
        firstObjects.ForEach(x => CreateTrack(0).AttachContainer(x));

        //Assign objects in between each rotation event according to their types.
        for (int i = 0; i < allRotationEvents.Count - 1; i++)
        {
            float firstTime = allRotationEvents[i].eventData._time; //Grab the times from our current event to the next one
            float secondTime = allRotationEvents[i + 1].eventData._time;
            rotation += allRotationEvents[i].eventData.GetRotationDegreeFromValue() ?? 0; //Add it's rotation value.
            int localRotation = betterModulo(rotation, 360); //Use a better modulo function to go from 0-360 degrees, even when negative.

            //Grab objects that should be rotated. Depending on what rotation type we're on, and what rotation type is next, we need to grab
            //objects at different times.
            //For example, an Early Rotation needs to grab the objects after itself as well as objects on the same beat.
            //A Late Rotation event just needs to grab objects after itself, since it doesn't effect objects on the same beat.
            List<BeatmapObjectContainer> rotatedObjects = allObjects.Where(x =>
                ((x.objectData._time >= firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time > firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)) &&
                ((x.objectData._time < secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time <= secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION))
                ).ToList();

            //Finally, grab the track that equals the local rotation set earlier, or create a new track if it doesn't exist.
            //We then assign the objects to it.
            Track track = CreateTrack(localRotation);
            rotatedObjects.ForEach(x => track?.AttachContainer(x));
        }

        //After all of that, we need to assign objects after the very last rotation event.
        //Read up a few lines to see how we are grabbing the objects needed to rotate, as it's essentially the same code.
        rotation += allRotationEvents.Last().eventData.GetRotationDegreeFromValue() ?? 0;
        int lastRotationType = allRotationEvents.Last().eventData._type;
        List<BeatmapObjectContainer> lastObjects = allObjects.Where(x =>
            (x.objectData._time >= allRotationEvents.Last().eventData._time && lastRotationType == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
            (x.objectData._time > allRotationEvents.Last().eventData._time && lastRotationType == MapEvent.EVENT_TYPE_LATE_ROTATION)
            ).ToList();
        Track lastTrack = CreateTrack(betterModulo(rotation, 360));
        lastObjects.ForEach(x => lastTrack?.AttachContainer(x));
    }

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    public void UpdatePosition(float position) //Take our position from AudioTimeSyncController and broadcast that to every track.
    {
        foreach (Track track in loadedTracks.Values) track.UpdatePosition(position);
    }

    /// <summary>
    /// Grab a <see cref="Track"/> with the specific rotation.
    /// </summary>
    /// <param name="rotation">Local Rotation from 0-359 degrees. It will be rounded to the nearest integer.</param>
    /// <returns>The track with the matching local rotation, or <see cref="null"/> if there is none.</returns>
    public Track GetTrackForRotationValue(int rotation)
    {
        return loadedTracks.TryGetValue(rotation, out Track track) ? track : null;
    }
}
