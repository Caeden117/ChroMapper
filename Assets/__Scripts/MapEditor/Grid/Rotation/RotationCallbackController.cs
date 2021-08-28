using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class RotationCallbackController : MonoBehaviour
{
    [SerializeField] private BeatmapObjectCallbackController interfaceCallback;
    [FormerlySerializedAs("atsc")] public AudioTimeSyncController Atsc;
    [SerializeField] private EventsContainer events;
    private readonly string[] enabledCharacteristics = {"360Degree", "90Degree", "Lawless"};

    public Action<bool, int> RotationChangedEvent; //Natural, degrees
    public bool IsActive { get; private set; }
    public MapEvent LatestRotationEvent { get; private set; }

    public int Rotation { get; private set; }

    // Start is called before the first frame update
    internal void Start()
    {
        var set = BeatSaberSongContainer.Instance.DifficultyData.ParentBeatmapSet;
        IsActive = enabledCharacteristics.Contains(set.BeatmapCharacteristicName);
        if (IsActive && Settings.Instance.Reminder_Loading360Levels)
        {
            PersistentUI.Instance.ShowDialogBox(
                "PersistentUI", "360warning"
                , Handle360LevelReminder, PersistentUI.DialogBoxPresetType.OkIgnore);
        }

        interfaceCallback.EventPassedThreshold += EventPassedThreshold;
        Atsc.PlayToggle += PlayToggle;
        Atsc.TimeChanged += OnTimeChanged;
        Settings.NotifyBySettingName("RotateTrack", UpdateRotateTrack);
    }

    private void OnDestroy()
    {
        interfaceCallback.EventPassedThreshold -= EventPassedThreshold;
        Atsc.PlayToggle -= PlayToggle;
        Atsc.TimeChanged -= OnTimeChanged;
        Settings.ClearSettingNotifications("RotateTrack");
    }

    private void UpdateRotateTrack(object obj)
    {
        if (Settings.Instance.RotateTrack) return;
        RotationChangedEvent?.Invoke(false, 0);
    }

    private void Handle360LevelReminder(int res) => Settings.Instance.Reminder_Loading360Levels = res == 0;

    private void OnTimeChanged()
    {
        if (Atsc.IsPlaying) return;
        PlayToggle(false);
    }

    private void PlayToggle(bool isPlaying)
    {
        if (!IsActive) return;
        var time = Atsc.CurrentBeat;
        var rotations = events.AllRotationEvents.Where(x =>
            x.Time < time || (x.Time == time && x.Type == MapEvent.EventTypeEarlyRotation));
        Rotation = 0;
        if (rotations.Count() > 0)
        {
            Rotation = rotations.Sum(x => x.GetRotationDegreeFromValue() ?? 0);
            LatestRotationEvent = rotations.LastOrDefault();
        }
        else
        {
            LatestRotationEvent = null;
        }

        RotationChangedEvent.Invoke(false, Rotation);
    }

    private void EventPassedThreshold(bool initial, int index, BeatmapObject obj)
    {
        var e = obj as MapEvent;
        if (e is null || !IsActive || (e == LatestRotationEvent && e.Type == MapEvent.EventTypeEarlyRotation) ||
            !e.IsRotationEvent)
        {
            return;
        }

        var rotationValue = e.GetRotationDegreeFromValue() ?? 0;
        Rotation += rotationValue;
        LatestRotationEvent = e;
        RotationChangedEvent.Invoke(true, Rotation);
    }
}
