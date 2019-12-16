using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    public void RefreshTracks()
    {
        if (!loadedTracks.Any())
        {
            for (int i = 0; i < 24; i++)
            {
                Track track = Instantiate(TrackPrefab, TracksParent).GetComponent<Track>();
                track.gameObject.name = $"Track {15 * i}";
                track.AssignRotationValue(15 * i, false);
                loadedTracks.Add(track);
            }
        }
        List<BeatmapObjectContainer> allObjects = new List<BeatmapObjectContainer>();
        objectContainerCollections.ForEach(x => allObjects.AddRange(x.LoadedContainers));

        List<BeatmapEventContainer> allRotationEvents = events.LoadedContainers.Cast<BeatmapEventContainer>().Where(x =>
            x.eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION ||
            x.eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION).OrderBy(x => x.eventData._time).ToList();
        int rotation = 0;
        List<BeatmapObjectContainer> firstObjects = allObjects.Where(x =>
            (x.objectData._time < allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
            (x.objectData._time <= allRotationEvents.First().eventData._time && allRotationEvents.First().eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        ).ToList();
        firstObjects.ForEach(x => loadedTracks.First().AttachContainer(x));
        for (int i = 0; i < allRotationEvents.Count - 1; i++)
        {
            float firstTime = allRotationEvents[i].eventData._time;
            float secondTime = allRotationEvents[i + 1].eventData._time;
            rotation += MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES[allRotationEvents[i].eventData._value];
            int localRotation = betterModulo(rotation, 360);
            Debug.Log($"Rotation: {localRotation}");
            List<BeatmapObjectContainer> rotatedObjects = allObjects.Where(x =>
                ((x.objectData._time >= firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time > firstTime && allRotationEvents[i].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)) &&
                ((x.objectData._time < secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) ||
                (x.objectData._time <= secondTime && allRotationEvents[i + 1].eventData._type == MapEvent.EVENT_TYPE_LATE_ROTATION))
                ).ToList();
            Track track = loadedTracks.Where(x => x.RotationValue == localRotation).First();
            rotatedObjects.ForEach(x => track.AttachContainer(x));
        }
        foreach(Track track in loadedTracks)
            track.AssignRotationValue(track.RotationValue);
    }

    private int betterModulo(int x, int m) => (x % m + m) % m; //thanks stackoverflow

    public void UpdatePosition(float position)
    {
        foreach (Track track in loadedTracks) track.UpdatePosition(position);
    }
}
