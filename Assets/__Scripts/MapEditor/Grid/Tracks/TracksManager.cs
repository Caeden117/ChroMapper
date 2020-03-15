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

    private Dictionary<int, Track> loadedTracks = new Dictionary<int, Track>();
    private List<BeatmapObjectContainerCollection> objectContainerCollections;
    private float songTimeInBeats;

    // Start is called before the first frame update
    void Start()
    {
        objectContainerCollections = GetComponents<BeatmapObjectContainerCollection>()
            .Where(x => x is NotesContainer || x is ObstaclesContainer).ToList();
        BeatmapObjectContainer.FlaggedForDeletionEvent += FlaggedForDeletion;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
        songTimeInBeats = atsc.GetBeatFromSeconds(BeatSaberSongContainer.Instance.loadedSong.length);
    }

    private void UpdateRotateTrack(object obj)
    {
        RefreshTracks();
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
        Settings.ClearSettingNotifications("RefreshTrack");
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
        allObjects = allObjects.OrderBy(x => x.objectData._time).ToList();

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
        MapEvent firstEvent = allRotationEvents.First().eventData;
        int rotation = 0;
        List<BeatmapObjectContainer> firstObjects = GetAllObjectsBeforeRotationTime(ref allObjects, firstEvent._time, firstEvent._type == MapEvent.EVENT_TYPE_EARLY_ROTATION);
        firstObjects.ForEach(x => CreateTrack(0).AttachContainer(x));

        //Assign objects in between each rotation event according to their types.
        for (int i = 0; i < allRotationEvents.Count; i++)
        {
            MapEvent e = allRotationEvents[i].eventData;
            float time = e._time; //Grab the time and event data.
            int localRotation = betterModulo(rotation, 360); //Use a better modulo function to go from 0-360 degrees, even when negative.

            //Grab objects that should be rotated. Depending on what rotation type we're on, and what rotation type is next, we need to grab
            //objects at different times.
            List<BeatmapObjectContainer> rotatedObjects = GetAllObjectsBeforeRotationTime(ref allObjects, time, e._type == MapEvent.EVENT_TYPE_EARLY_ROTATION);
            //Finally, grab the track that equals the local rotation set earlier, or create a new track if it doesn't exist.
            //We then assign the objects to it.
            Track track = CreateTrack(localRotation);
            rotatedObjects.ForEach(x => track?.AttachContainer(x));

            //Add new rotation value for the next loop around.
            rotation += allRotationEvents[i].eventData.GetRotationDegreeFromValue() ?? 0; //Add it's rotation value.
        }

        //After all of that, we need to assign objects after the very last rotation event.
        //Read up a few lines to see how we are grabbing the objects needed to rotate, as it's essentially the same code.
        MapEvent lastEvent = allRotationEvents.Last().eventData;
        List<BeatmapObjectContainer> lastObjects = GetAllObjectsBeforeRotationTime(ref allObjects, songTimeInBeats, lastEvent._type == MapEvent.EVENT_TYPE_EARLY_ROTATION);
        Track lastTrack = CreateTrack(betterModulo(rotation, 360));
        lastObjects.ForEach(x => lastTrack?.AttachContainer(x));
    }

    private List<BeatmapObjectContainer> GetAllObjectsBeforeRotationTime(ref List<BeatmapObjectContainer> list, float time, bool isEarlyRotation)
    {
        List<BeatmapObjectContainer> allObjects = new List<BeatmapObjectContainer>();
        foreach (BeatmapObjectContainer obj in list.ToArray())
        {
            float objTime = obj.objectData._time;
            if ((objTime < time && isEarlyRotation) || (objTime <= time && !isEarlyRotation))
            {
                allObjects.Add(obj);
            }
            else break; //Because all objects are sorted by time, we can break this early if we've gone ahead of ourselves.
        }
        list.RemoveRange(0, allObjects.Count);
        return allObjects;
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
