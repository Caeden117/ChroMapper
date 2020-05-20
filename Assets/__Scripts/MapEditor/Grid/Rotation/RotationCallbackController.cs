using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RotationCallbackController : MonoBehaviour
{
    public bool IsActive { get; private set; }
    private readonly string[] enabledCharacteristics = { "360Degree", "90Degree", "Lawless" };

    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;
    public AudioTimeSyncController atsc;
    [SerializeField] private EventsContainer events;

    public Action<bool, int> RotationChangedEvent; //Natural, degrees
    public MapEvent LatestRotationEvent { get; private set; }

    public int Rotation { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        BeatSaberSong.DifficultyBeatmapSet set = BeatSaberSongContainer.Instance.difficultyData.parentBeatmapSet;
        IsActive = enabledCharacteristics.Contains(set.beatmapCharacteristicName);
        if (IsActive && Settings.Instance.Reminder_Loading360Levels)
        {
            PersistentUI.Instance.ShowDialogBox(
                "360 Mapping is relatively new and can easily make the map unplayable if left untested.\n\n" +
                "For the love of all that is holy, have yourself and others playtest your map frequently."
                , Handle360LevelReminder, "Ok", "Don't Remind Me");
        }
        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        atsc.OnPlayToggle += PlayToggle;
        atsc.OnTimeChanged += OnTimeChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void UpdateRotateTrack(object obj)
    {
        if (Settings.Instance.RotateTrack) return;
        RotationChangedEvent?.Invoke(false, 0);
    }

    private void Handle360LevelReminder(int res)
    {
        Settings.Instance.Reminder_Loading360Levels = res == 0;
    }

    private void OnTimeChanged()
    {
        if (atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        float time = atsc.CurrentBeat;
        IEnumerable<BeatmapObject> rotations = events.LoadedObjects.Where(
            x => x._time <= atsc.CurrentBeat && (x as MapEvent).IsRotationEvent);
        Rotation = 0;
        if (rotations.Count() > 0)
        {
            MapEvent e = null; //The last event in time should be the last one through the foreach loop so this should work.
            foreach (BeatmapObject o in rotations)
            {
                e = o as MapEvent;
                if (e._time == atsc.CurrentBeat && e._type == MapEvent.EVENT_TYPE_LATE_ROTATION) continue;
                Rotation += e.GetRotationDegreeFromValue() ?? 0;
            }
            LatestRotationEvent = e;
        }
        else LatestRotationEvent = null;
        RotationChangedEvent.Invoke(false, Rotation);
    }

    private void EventPassedThreshold(bool initial, int index, BeatmapObject obj)
    {
        MapEvent e = obj as MapEvent;
        if (e is null || !IsActive || (e == LatestRotationEvent && e._type == MapEvent.EVENT_TYPE_EARLY_ROTATION) || !e.IsRotationEvent) return;
        int rotationValue = e.GetRotationDegreeFromValue() ?? 0;
        Rotation += rotationValue;
        LatestRotationEvent = e;
        RotationChangedEvent.Invoke(true, Rotation);
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        atsc.OnPlayToggle -= PlayToggle;
        atsc.OnTimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }
}
