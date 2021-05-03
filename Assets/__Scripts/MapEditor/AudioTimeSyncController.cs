using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AudioTimeSyncController : MonoBehaviour, CMInput.IPlaybackActions, CMInput.ITimelineActions
{
    public static readonly string PrecisionSnapName = "PrecisionSnap";

    [SerializeField] public AudioSource songAudioSource;
    [SerializeField] AudioSource waveformSource;

    [SerializeField] GameObject moveables;
    [SerializeField] TracksManager tracksManager;
    [SerializeField] Track[] otherTracks;
    [SerializeField] BPMChangesContainer bpmChangesContainer;
    [SerializeField] GridRenderingController gridRenderingController;
    [SerializeField] CustomStandaloneInputModule customStandaloneInputModule;

    public int gridMeasureSnapping
    {
        get => _gridMeasureSnapping;
        set
        {
            int old = _gridMeasureSnapping;
            _gridMeasureSnapping = value;
            Settings.NonPersistentSettings[PrecisionSnapName] = value;
            if (_gridMeasureSnapping != old) GridMeasureSnappingChanged?.Invoke(value);
        }
    }

    private bool controlSnap = false;
    private bool preciselyControlSnap = false;

    private AudioClip clip;
    [HideInInspector] public BeatSaberSong song;
    private int _gridMeasureSnapping = 1;

    [SerializeField] private float currentBeat;
    [SerializeField] private float currentSeconds;

    public float CurrentBeat {
        get => currentBeat;
        private set {
            currentBeat = value;
            currentSeconds = GetSecondsFromBeat(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public float CurrentSeconds {
        get => currentSeconds;
        private set {
            currentSeconds = value;
            currentBeat = GetBeatFromSeconds(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public bool IsPlaying { get; private set; }
    private float playStartTime;
    private const float cancelPlayInputDuration = 0.3f;

    private float offsetMS;
    public float offsetBeat { get; private set; } = -1;
    public float gridStartPosition { get; private set; } = -1;

    private float songSpeed = 10f;
    private bool levelLoaded = false;
    
    public Action OnTimeChanged;
    public Action<bool> OnPlayToggle;
    public Action<int> GridMeasureSnappingChanged;

    // Use this for initialization
    void Start() {
        try
        {
            //Init dat stuff
            clip = BeatSaberSongContainer.Instance.loadedSong;
            song = BeatSaberSongContainer.Instance.song;
            offsetMS = song.songTimeOffset / 1000;
            ResetTime();
            offsetBeat = currentBeat;
            gridStartPosition = currentBeat * EditorScaleController.EditorScale;
            IsPlaying = false;
            songAudioSource.clip = clip;
            songAudioSource.volume = Settings.Instance.SongVolume;
            waveformSource.clip = clip;
            UpdateMovables();
            if (Settings.NonPersistentSettings.ContainsKey(PrecisionSnapName))
            {
                gridMeasureSnapping = (int)Settings.NonPersistentSettings[PrecisionSnapName];
            }
            GridMeasureSnappingChanged?.Invoke(gridMeasureSnapping);
            LoadInitialMap.LevelLoadedEvent += OnLevelLoaded;
            Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
            Settings.NotifyBySettingName("SongVolume", UpdateSongVolume);
        }
        catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void UpdateSongVolume(object obj)
    {
        songAudioSource.volume = (float) obj;
    }

    private void UpdateSongSpeed(object obj)
    {
        songSpeed = (float)obj;
    }

    private void OnLevelLoaded()
    {
        levelLoaded = true;
    }

    private void OnDestroy()
    {
        LoadInitialMap.LevelLoadedEvent -= OnLevelLoaded;
        Settings.ClearSettingNotifications("SongSpeed");
        Settings.ClearSettingNotifications("SongVolume");
    }

    private void Update() {
        try
        {
            if (!levelLoaded) return;
            if (IsPlaying)
            {
                var time = currentSeconds;

                // Slightly more accurate than songAudioSource.time
                var trackTime = songAudioSource.timeSamples / (float) songAudioSource.clip.frequency;

                // Sync correction
                var correction = CurrentSeconds > 1 ? trackTime / CurrentSeconds : 1f;

                // Snap forward if we are more than a 2 frames out of sync as we're trying to make it one frame out?
                float frameTime = Mathf.Max(0.04f, Time.smoothDeltaTime * 2);
                if (Mathf.Abs(trackTime - CurrentSeconds) >= frameTime * (songSpeed / 10f))
                {
                    time = trackTime;
                    correction = 1;
                }

                // Add frame time to current time
                CurrentSeconds = time + (correction * (Time.deltaTime * (songSpeed / 10f)));

                if (!songAudioSource.isPlaying) TogglePlaying();
            }

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void UpdateMovables() {
        float position = currentBeat * EditorScaleController.EditorScale;
        gridStartPosition = offsetBeat * EditorScaleController.EditorScale;

        gridRenderingController.UpdateOffset(position - gridStartPosition);

        tracksManager.UpdatePosition(position * -1);
        foreach (Track track in otherTracks) track.UpdatePosition(position * -1);
        OnTimeChanged?.Invoke();
    }

    private void ResetTime() {
        CurrentSeconds = offsetMS;
    }

    public void TogglePlaying() {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            if (CurrentSeconds >= songAudioSource.clip.length - 0.1f)
            {
                Debug.LogError(":hyperPepega: :mega: STOP TRYING TO PLAY THE SONG AT THE VERY END");
            }
            else
            {
                playStartTime = CurrentSeconds;
                songAudioSource.time = CurrentSeconds;
                songAudioSource.Play();
            }
        }
        else
        {
            songAudioSource.Stop();
            SnapToGrid();
        }
        if (OnPlayToggle != null) OnPlayToggle(IsPlaying);
    }

    public void CancelPlaying()
    {
        if (!IsPlaying) return;

        TogglePlaying();
        CurrentSeconds = playStartTime;
    }

    public void SnapToGrid(float seconds)
    {
        if (IsPlaying) return;
        var beatTime = GetBeatFromSeconds(seconds);
        currentBeat = bpmChangesContainer.FindRoundedBPMTime(beatTime) + offsetBeat;
        currentSeconds = GetSecondsFromBeat(currentBeat);
        songAudioSource.time = CurrentSeconds + offsetMS;
        ValidatePosition();
        UpdateMovables();
    }

    public void SnapToGrid(bool positionValidated = false) {
        currentBeat = bpmChangesContainer.FindRoundedBPMTime(currentBeat) + offsetBeat;
        currentSeconds = GetSecondsFromBeat(currentBeat);
        if (!positionValidated) ValidatePosition();
        UpdateMovables();
    }

    public void MoveToTimeInSeconds(float seconds) {
        if (IsPlaying) return;
        CurrentSeconds = seconds;
        songAudioSource.time = CurrentSeconds + offsetMS;
    }

    public void RefreshGridSnapping()
    {
        GridMeasureSnappingChanged?.Invoke(gridMeasureSnapping);
    }

    public void MoveToTimeInBeats(float beats) {
        if (IsPlaying) return;
        CurrentBeat = beats;
        songAudioSource.time = CurrentSeconds + offsetBeat;
    }

    public float FindRoundedBeatTime(float beat, float snap = -1) => bpmChangesContainer.FindRoundedBPMTime(beat, snap);

    public float GetBeatFromSeconds(float seconds) => song.beatsPerMinute / 60 * seconds;

    public float GetSecondsFromBeat(float beat) => 60 / song.beatsPerMinute * beat;

    private void ValidatePosition() {
        if (currentSeconds < offsetMS) currentSeconds = offsetMS;
        if (currentBeat < offsetBeat) currentBeat = offsetBeat;
        if (currentSeconds > BeatSaberSongContainer.Instance.loadedSong.length)
        {
            CurrentSeconds = BeatSaberSongContainer.Instance.loadedSong.length;
            SnapToGrid(true);
        }
    }

    public void OnTogglePlaying(InputAction.CallbackContext context)
    {
        if (context.performed) TogglePlaying();

        // if play is held and released a significant time later, cancel playing instead of merely toggling
        if (!CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.IPlaybackActions))
            && context.canceled
            && context.duration >= cancelPlayInputDuration) CancelPlaying();
    }

    public void OnResetTime(InputAction.CallbackContext context)
    {
        if (context.performed && !IsPlaying) ResetTime();
    }

    public void OnChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        if (!KeybindsController.IsMouseInWindow || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true)) return;
        float value = context.ReadValue<float>();
        if (context.performed)
        {
            if (controlSnap)
            {
                float scrollDirection;
                if (Settings.Instance.InvertPrecisionScroll) scrollDirection = value > 0 ? 0.5f : 2;
                else scrollDirection = value > 0 ? 2 : 0.5f;
                if (!preciselyControlSnap)
                {
                    gridMeasureSnapping = Mathf.Clamp(Mathf.RoundToInt(gridMeasureSnapping * scrollDirection), 1, 64);
                }
                else
                {
                    int addition = scrollDirection > 1 ? 1 : -1;
                    gridMeasureSnapping = Mathf.Clamp(gridMeasureSnapping + addition, 1, 64);
                }
            }
            else
            {
                if (Settings.Instance.InvertScrollTime) value *= -1;
                // +1 beat if we're going forward, -1 beat if we're going backwards
                float beatShiftRaw = 1f / gridMeasureSnapping * (value > 0 ? 1f : -1f);

                // Grab any BPM Change at this location, calculate a BPM-modified shift in beat
                BeatmapBPMChange currentBpmChange = bpmChangesContainer.FindLastBPM(CurrentBeat, true);
                float beatShift = beatShiftRaw;
                // This new beatShift value will move us 1 BPM-modified beat forward or backward
                if (currentBpmChange != null) beatShift *= (song.beatsPerMinute / currentBpmChange._BPM);

                // Now we check if the BPM Change after the shift is different.
                BeatmapBPMChange lastBpmChange = bpmChangesContainer.FindLastBPM(CurrentBeat + beatShift, true);

                if (lastBpmChange != currentBpmChange && currentBpmChange != null)
                {
                    if (currentBpmChange._time == CurrentBeat) // We're on top of a BPM change, move using previous BPM
                    {
                        beatShift = lastBpmChange == null ? beatShiftRaw : (beatShiftRaw * (song.beatsPerMinute / lastBpmChange._BPM));
                        MoveToTimeInBeats(CurrentBeat + beatShift);
                    }
                    else if (beatShiftRaw < 0)
                    {
                        MoveToTimeInBeats(currentBpmChange._time); // If we're going backward, snap to our current bpm change.
                    }
                    else if (lastBpmChange != null)
                    {
                        MoveToTimeInBeats(lastBpmChange._time); // If we're going forward, snap to that bpm change.
                    }
                }
                else
                {
                    MoveToTimeInBeats(CurrentBeat + beatShift);
                }
            }
        }
    }

    public void OnChangePrecisionModifier(InputAction.CallbackContext context)
    {
        controlSnap = context.performed;
    }

    public void OnPreciseSnapModification(InputAction.CallbackContext context)
    {
        preciselyControlSnap = context.performed;
    }
}
