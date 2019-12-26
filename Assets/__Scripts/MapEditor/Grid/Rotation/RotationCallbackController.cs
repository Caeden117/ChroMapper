using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RotationCallbackController : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;
    private readonly string[] enabledCharacteristics = new string[] { "360Degree", "90Degree", "Lawless" };

    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    public Action<bool, int> RotationChangedEvent; //Natural, early rotation, degrees
    public MapEvent LatestRotationEvent { get; private set; } = null;

    public int Rotation { get; private set; } = 0;

    // Start is called before the first frame update
    void Start()
    {
        BeatSaberSong.DifficultyBeatmapSet set = BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet;
        IsActive = enabledCharacteristics.Contains(set.beatmapCharacteristicName);
        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        atsc.OnPlayToggle += PlayToggle;
    }

    private void Update()
    {
        if (atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        float time = atsc.CurrentBeat;
        IEnumerable<MapEvent> rotations = events.LoadedContainers.Cast<BeatmapEventContainer>().Select(x => x.eventData)
            .Where(x => (x._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || x._type == MapEvent.EVENT_TYPE_LATE_ROTATION) &&
                x._time <= time
        );
        Rotation = 0;
        if (rotations.Any())
        {
            foreach (MapEvent e in rotations) Rotation += MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES[e._value];
            LatestRotationEvent = rotations.OrderBy(x => x._time).Last();
        }
        else LatestRotationEvent = null;
        RotationChangedEvent.Invoke(false, Rotation);
    }

    private void EventPassedThreshold(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent;
        if (e is null || !IsActive || e == LatestRotationEvent) return;
        if (e._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || e._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        {
            int rotationValue = MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES[e._value];
            Rotation += rotationValue;
            LatestRotationEvent = e;
            RotationChangedEvent.Invoke(true, Rotation);
        }
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        atsc.OnPlayToggle -= PlayToggle;
    }
}
