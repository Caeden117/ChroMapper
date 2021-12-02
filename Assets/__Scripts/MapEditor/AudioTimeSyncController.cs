using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AudioTimeSyncController : MonoBehaviour, CMInput.IPlaybackActions, CMInput.ITimelineActions
{
    private const float cancelPlayInputDuration = 0.3f;
    public static readonly string PrecisionSnapName = "PrecisionSnap";

    [FormerlySerializedAs("songAudioSource")] public AudioSource SongAudioSource;
    [SerializeField] private AudioSource waveformSource;

    [SerializeField] private GameObject moveables;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private Track[] otherTracks;
    [SerializeField] private BPMChangesContainer bpmChangesContainer;
    [SerializeField] private GridRenderingController gridRenderingController;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [FormerlySerializedAs("song")] [HideInInspector] public BeatSaberSong Song;

    [SerializeField] private float currentBeat;
    [SerializeField] private float currentSeconds;
    [FormerlySerializedAs("stopScheduled")] public bool StopScheduled;
    [FormerlySerializedAs("initialized")] public bool Initialized;
    private int gridMeasureSnapping = 1;
    private float audioLatencyCompensationSeconds;

    private AudioClip clip;

    private bool controlSnap;
    public Action<int> GridMeasureSnappingChanged;
    private bool levelLoaded;
    public Action<bool> PlayToggle;

    public Action TimeChanged;
    private float playStartTime;
    private bool preciselyControlSnap;

    private float songSpeed = 10f;

    public int GridMeasureSnapping
    {
        get => gridMeasureSnapping;
        set
        {
            var old = gridMeasureSnapping;
            gridMeasureSnapping = value;
            Settings.NonPersistentSettings[PrecisionSnapName] = value;
            if (gridMeasureSnapping != old) GridMeasureSnappingChanged?.Invoke(value);
        }
    }

    public float CurrentBeat
    {
        get => currentBeat;
        private set
        {
            currentBeat = value;
            currentSeconds = GetSecondsFromBeat(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public float CurrentSeconds
    {
        get => currentSeconds;
        private set
        {
            currentSeconds = value;
            currentBeat = GetBeatFromSeconds(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public float CurrentSongSeconds => SongAudioSource.clip is null ? 0f : SongAudioSource.timeSamples / (float)SongAudioSource.clip.frequency;

    public float CurrentSongBeats => GetBeatFromSeconds(CurrentSongSeconds);

    public bool IsPlaying { get; private set; }

    // Use this for initialization
    private void Start()
    {
        try
        {
            //Init dat stuff
            clip = BeatSaberSongContainer.Instance.LoadedSong;
            Song = BeatSaberSongContainer.Instance.Song;
            ResetTime();
            IsPlaying = false;
            SongAudioSource.clip = clip;
            SongAudioSource.volume = Settings.Instance.SongVolume;
            waveformSource.clip = clip;
            UpdateMovables();
            if (Settings.NonPersistentSettings.ContainsKey(PrecisionSnapName))
                GridMeasureSnapping = (int)Settings.NonPersistentSettings[PrecisionSnapName];
            GridMeasureSnappingChanged?.Invoke(GridMeasureSnapping);
            LoadInitialMap.LevelLoadedEvent += OnLevelLoaded;
            Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
            Settings.NotifyBySettingName("SongVolume", UpdateSongVolume);

            Initialized = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        try
        {
            if (!levelLoaded) return;
            if (IsPlaying)
            {
                var time = currentSeconds + audioLatencyCompensationSeconds;

                // Slightly more accurate than songAudioSource.time
                var trackTime = CurrentSongSeconds;

                // Sync correction
                var correction = time > 1 ? trackTime / time : 1f;

                if (SongAudioSource.isPlaying)
                {
                    // Snap forward if we are more than a 2 frames out of sync as we're trying to make it one frame out?
                    var frameTime = Mathf.Max(0.04f, Time.smoothDeltaTime * 2);
                    if (Mathf.Abs(trackTime - time) >= frameTime * (songSpeed / 10f))
                    {
                        time = trackTime;
                        correction = 1;
                    }
                }
                else
                {
                    correction = 1;
                    if (!StopScheduled) StartCoroutine(StopPlayingDelayed(audioLatencyCompensationSeconds));
                }

                // Add frame time to current time
                CurrentSeconds = time + (correction * (Time.deltaTime * (songSpeed / 10f))) -
                                 audioLatencyCompensationSeconds;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        clip = null;
        LoadInitialMap.LevelLoadedEvent -= OnLevelLoaded;
        Settings.ClearSettingNotifications("SongSpeed");
        Settings.ClearSettingNotifications("SongVolume");
    }

    public void OnTogglePlaying(InputAction.CallbackContext context)
    {
        if (context.performed) TogglePlaying();

        // if play is held and released a significant time later, cancel playing instead of merely toggling
        if (!CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.IPlaybackActions))
            && context.canceled
            && context.duration >= cancelPlayInputDuration)
        {
            CancelPlaying();
        }
    }

    public void OnResetTime(InputAction.CallbackContext context)
    {
        if (context.performed && !IsPlaying) ResetTime();
    }

    public void OnChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        if (!KeybindsController.IsMouseInWindow ||
            customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true))
        {
            return;
        }

        var value = context.ReadValue<float>();
        if (context.performed)
        {
            if (controlSnap)
            {
                float scrollDirection;
                if (Settings.Instance.InvertPrecisionScroll) scrollDirection = value > 0 ? 0.5f : 2;
                else scrollDirection = value > 0 ? 2 : 0.5f;
                if (!preciselyControlSnap)
                {
                    GridMeasureSnapping = Mathf.Clamp(Mathf.RoundToInt(GridMeasureSnapping * scrollDirection), 1, 64);
                }
                else
                {
                    var addition = scrollDirection > 1 ? 1 : -1;
                    GridMeasureSnapping = Mathf.Clamp(GridMeasureSnapping + addition, 1, 64);
                }
            }
            else
            {
                if (Settings.Instance.InvertScrollTime) value *= -1;
                // +1 beat if we're going forward, -1 beat if we're going backwards
                var beatShiftRaw = 1f / GridMeasureSnapping * (value > 0 ? 1f : -1f);

                MoveToTimeInBeats(CurrentBeat + bpmChangesContainer.LocalBeatsToSongBeats(beatShiftRaw, CurrentBeat));
            }
        }
    }

    public void OnChangePrecisionModifier(InputAction.CallbackContext context) => controlSnap = context.performed;

    public void OnPreciseSnapModification(InputAction.CallbackContext context) =>
        preciselyControlSnap = context.performed;

    private void UpdateSongVolume(object obj) => SongAudioSource.volume = (float)obj;

    private void UpdateSongSpeed(object obj) => songSpeed = (float)obj;

    private void OnLevelLoaded() => levelLoaded = true;

    private void UpdateMovables()
    {
        var position = currentBeat * EditorScaleController.EditorScale;

        gridRenderingController.UpdateOffset(position);

        tracksManager.UpdatePosition(position * -1);
        foreach (var track in otherTracks) track.UpdatePosition(position * -1);
        TimeChanged?.Invoke();
    }

    private void ResetTime() => CurrentSeconds = 0;

    public IEnumerator StopPlayingDelayed(float delaySeconds)
    {
        StopScheduled = true;
        yield return new WaitForSeconds(delaySeconds);
        StopScheduled = false;
        if (IsPlaying) TogglePlaying();
    }

    public void TogglePlaying()
    {
        if (StopScheduled)
        {
            StopCoroutine(nameof(StopPlayingDelayed));
            StopScheduled = false;
        }

        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            if (CurrentSeconds >= SongAudioSource.clip.length - 0.1f)
            {
                Debug.LogError(":hyperPepega: :mega: STOP TRYING TO PLAY THE SONG AT THE VERY END");
                IsPlaying = false;
                return;
            }

            playStartTime = CurrentSeconds;
            SongAudioSource.time = CurrentSeconds;
            SongAudioSource.Play();

            audioLatencyCompensationSeconds = Settings.Instance.AudioLatencyCompensation / 1000f;
            CurrentSeconds -= audioLatencyCompensationSeconds;
        }
        else
        {
            SongAudioSource.Stop();
            SnapToGrid();
        }

        PlayToggle?.Invoke(IsPlaying);
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
        currentBeat = bpmChangesContainer.FindRoundedBpmTime(beatTime);
        currentSeconds = GetSecondsFromBeat(currentBeat);
        SongAudioSource.time = CurrentSeconds;
        ValidatePosition();
        UpdateMovables();
    }

    public void SnapToGrid(bool positionValidated = false)
    {
        currentBeat = bpmChangesContainer.FindRoundedBpmTime(currentBeat);
        currentSeconds = GetSecondsFromBeat(currentBeat);
        if (!positionValidated) ValidatePosition();
        UpdateMovables();
    }

    public void MoveToTimeInSeconds(float seconds)
    {
        if (IsPlaying) return;
        CurrentSeconds = seconds;
        SongAudioSource.time = CurrentSeconds;
    }

    public void RefreshGridSnapping() => GridMeasureSnappingChanged?.Invoke(GridMeasureSnapping);

    public void MoveToTimeInBeats(float beats)
    {
        if (IsPlaying) return;
        CurrentBeat = beats;
        SongAudioSource.time = CurrentSeconds;
    }

    public float FindRoundedBeatTime(float beat, float snap = -1) => bpmChangesContainer.FindRoundedBpmTime(beat, snap);

    public float GetBeatFromSeconds(float seconds) => Song.BeatsPerMinute / 60 * seconds;

    public float GetSecondsFromBeat(float beat) => 60 / Song.BeatsPerMinute * beat;

    private void ValidatePosition()
    {
        // Don't validate during playback
        if (IsPlaying) return;

        if (currentSeconds < 0) currentSeconds = 0;
        if (currentBeat < 0) currentBeat = 0;
        if (currentSeconds > BeatSaberSongContainer.Instance.LoadedSong.length)
        {
            CurrentSeconds = BeatSaberSongContainer.Instance.LoadedSong.length;
            SnapToGrid(true);
        }
    }
}
