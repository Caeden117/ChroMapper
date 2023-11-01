using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;
using NAudio.Wave;
using NAudio.Vorbis;
using System.IO;

public class AudioTimeSyncController : MonoBehaviour, CMInput.IPlaybackActions, CMInput.ITimelineActions, CMInput.ITimelineNavigationActions
{
    public static readonly string PrecisionSnapName = "PrecisionSnap";

    private static readonly int songTime = Shader.PropertyToID("_SongTime");
    private const float cancelPlayInputDuration = 0.3f;

    //ELECAST TEST [FormerlySerializedAs("songAudioSource")] public AudioSource SongAudioSource;
    [SerializeField] private AudioSource waveformSource;

    [SerializeField] private GameObject moveables;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private Track[] otherTracks;
    [FormerlySerializedAs("bpmChangesContainer")][SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private GridRenderingController gridRenderingController;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [FormerlySerializedAs("song")][HideInInspector] public BeatSaberSong Song;

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

    [SerializeField] private float currentJsonTime;
    public float CurrentJsonTime
    {
        get => currentJsonTime;
        private set
        {
            currentJsonTime = value;
            currentSongBpmTime = bpmChangeGridContainer?.JsonTimeToSongBpmTime(value) ?? value;
            currentSeconds = GetSecondsFromBeat(currentSongBpmTime);
            ValidatePosition();
            UpdateMovables();
        }
    }

    [Obsolete("This is for existing dev plugin compatibility. Use CurrentSongBpmTime, CurrentJsonTime, or CurrentSeconds.", true)]
    public float CurrentBeat { get => CurrentSongBpmTime; }

    [SerializeField] private float currentSongBpmTime;
    public float CurrentSongBpmTime
    {
        get => currentSongBpmTime;
        private set
        {
            currentSongBpmTime = value;
            currentJsonTime = bpmChangeGridContainer?.SongBpmTimeToJsonTime(value) ?? value;
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
            currentSongBpmTime = GetBeatFromSeconds(value);
            currentJsonTime = bpmChangeGridContainer.SongBpmTimeToJsonTime(currentSongBpmTime);
            ValidatePosition();
            UpdateMovables();
        }
    }

    //ELECAST TEST public float CurrentAudioSeconds => SongAudioSource.clip is null ? 0f : SongAudioSource.timeSamples / (float)SongAudioSource.clip.frequency;

    public float CurrentAudioSeconds => Vorbis is null ? 0f : IsPlaying ? CurrentSeconds : Vorbis.SamplePosition/sampleRate;//ELECAST TEST

    public float CurrentAudioBeats => GetBeatFromSeconds(CurrentAudioSeconds);

    public bool IsPlaying { get; private set; }

    private int bytesPerSecond;//ELECAST TEST
    private WaveOutEvent waveOut; //ELECAST TEST
    public VorbisSampleProvider Vorbis; //ELECAST TEST
    private int sampleRate; //ELECAST TEST

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
            //ELECAST TEST SongAudioSource.clip = clip;
            //ELECAST TEST SongAudioSource.volume = Settings.Instance.SongVolume;

            Vorbis = new VorbisSampleProvider(File.OpenRead(Song.Directory + "\\" + Song.SongFilename), true);//ELECAST TEST
            bytesPerSecond = Vorbis.WaveFormat.AverageBytesPerSecond;//ELECAST TEST
            sampleRate = Vorbis.WaveFormat.SampleRate;//ELECAST TEST
            waveOut = new WaveOutEvent();//ELECAST TEST
            waveOut.Init(Vorbis);//ELECAST TEST

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
                var time = currentSeconds + (audioLatencyCompensationSeconds * (songSpeed / 10f));

                // Slightly more accurate than songAudioSource.time
                var trackTime = CurrentAudioSeconds;

                // Sync correction
                var correction = time > 1 ? trackTime / time : 1f;

                if (waveOut.PlaybackState == PlaybackState.Playing) //ELECAST TEST
                //if (SongAudioSource.isPlaying)
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
                                 (audioLatencyCompensationSeconds * (songSpeed / 10f));
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

                MoveToJsonTime(Mathf.Max(0, CurrentJsonTime + beatShiftRaw));
            }
        }
    }

    /// <summary>
    /// Newly introduced in beatmap V3, because arc and chain need `shift + scroll`, 
    /// which override default input for `shift` for <see cref="OnChangePrecisionModifier(InputAction.CallbackContext)"/> and `scroll` for <see cref="OnChangeTimeandPrecision(InputAction.CallbackContext)"/>
    /// </summary>
    /// <param name="context"></param>
    public void OnPreciselyChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        if (!KeybindsController.IsMouseInWindow ||
            customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(-1, true))
        {
            return;
        }

        var value = context.ReadValue<float>();
        if (context.performed)
        {
            float scrollDirection;
            if (Settings.Instance.InvertPrecisionScroll) scrollDirection = value > 0 ? 0.5f : 2;
            else scrollDirection = value > 0 ? 2 : 0.5f;

            var addition = scrollDirection > 1 ? 1 : -1;
            GridMeasureSnapping = Mathf.Clamp(GridMeasureSnapping + addition, 1, 64);
        }
    }

    public void OnChangePrecisionModifier(InputAction.CallbackContext context) => controlSnap = context.performed;

    public void OnPreciseSnapModification(InputAction.CallbackContext context) =>
        preciselyControlSnap = context.performed;

    public void OnGoToBeat(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        PersistentUI.Instance.ShowInputBox("Mapper", "gotobeat.dialog", GoToBeat);
    }

    internal void GoToBeat(string beatInput)
    {
        if (string.IsNullOrEmpty(beatInput) || string.IsNullOrWhiteSpace(beatInput))
        {
            return;
        }

        if (float.TryParse(beatInput, out var jsonTime))
        {
            CurrentJsonTime = Mathf.Max(0, jsonTime);
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "gotobeat.dialog.invalid", GoToBeat);
        }
    }

    public void OnMoveCursorForward(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        CurrentJsonTime += (1f / gridMeasureSnapping);
    }

    public void OnMoveCursorBackward(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        CurrentJsonTime -= (1f / gridMeasureSnapping);
    }

    //ELECAST TEST private void UpdateSongVolume(object obj) => SongAudioSource.volume = (float)obj;
    private void UpdateSongVolume(object obj) => waveOut.Volume = (float)obj;//ELECAST TEST

    //ELECAST TEST private void UpdateSongSpeed(object obj) => songSpeed = (float)obj;
    private void UpdateSongSpeed(object obj) //ELECAST TEST
    {
        songSpeed = (float)obj;

        
        //TODO Add NAudio Support!!

    }
        

    private void OnLevelLoaded() => levelLoaded = true;

    private void UpdateMovables()
    {
        Shader.SetGlobalFloat(songTime, currentSongBpmTime);

        var position = currentSongBpmTime * EditorScaleController.EditorScale;

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
            //ELECAST TEST if (CurrentSeconds >= SongAudioSource.clip.length - 0.1f)
            if (CurrentSeconds >= GetTotalSeconds() - 0.1f)//ELECAST TEST
            {
                ResetTime();
            }

            playStartTime = CurrentSeconds;
            //ELECAST TEST SongAudioSource.time = CurrentSeconds;
            //ELECAST TEST SongAudioSource.Play();
            SetVorbisPosition((long)(CurrentSeconds * bytesPerSecond));//ELECAST TEST
            waveOut.Play();//ELECAST TEST

            audioLatencyCompensationSeconds = Settings.Instance.AudioLatencyCompensation / 1000f;
            CurrentSeconds -= audioLatencyCompensationSeconds * (songSpeed / 10f);
        }
        else
        {
            //ELECAST TEST SongAudioSource.Stop();
            waveOut.Stop();//ELECAST TEST
            SnapToGrid();
        }

        PlayToggle?.Invoke(IsPlaying);
    }

    public float GetTotalSeconds()
    {
        return (float)Vorbis.Length / sampleRate;
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
        var songBpmTime = GetBeatFromSeconds(seconds);
        UpdateCurrentTimes(songBpmTime);
        //ELECAST TEST SongAudioSource.time = CurrentSeconds;
        SetVorbisPosition((long)(CurrentSeconds * bytesPerSecond));//ELECAST TEST
        ValidatePosition();
        UpdateMovables();
    }

    public void SnapToGrid(bool positionValidated = false)
    {
        UpdateCurrentTimes(currentSongBpmTime);
        if (!positionValidated) ValidatePosition();
        UpdateMovables();
    }

    private void UpdateCurrentTimes(float songBpmTime)
    {
        currentJsonTime = bpmChangeGridContainer.SongBpmTimeToRoundedJsonTime(songBpmTime);
        currentSongBpmTime = bpmChangeGridContainer.JsonTimeToSongBpmTime(currentJsonTime);
        currentSeconds = GetSecondsFromBeat(currentSongBpmTime);
    }

    public void RefreshGridSnapping() => GridMeasureSnappingChanged?.Invoke(GridMeasureSnapping);

    public void MoveToTimeInSeconds(float seconds)
    {
        if (IsPlaying) return;
        CurrentSeconds = seconds;
        //ELECAST TEST SongAudioSource.time = CurrentSeconds;
        SetVorbisPosition((long)(CurrentSeconds * bytesPerSecond));//ELECAST TEST
    }

    [Obsolete("This is for existing dev plugin compatibility. Use MoveToSongBpmTime or MoveToJsonTime.", true)]
    public void MoveToTimeInBeats(float beats) => MoveToSongBpmTime(beats);
    public void MoveToSongBpmTime(float songBpmTime)
    {
        if (IsPlaying) return;
        CurrentSongBpmTime = songBpmTime;
        //ELECAST TEST SongAudioSource.time = CurrentSeconds;
        SetVorbisPosition((long)(CurrentSeconds * bytesPerSecond));//ELECAST TEST
    }

    public void MoveToJsonTime(float jsonTime)
    {
        if (IsPlaying) return;
        CurrentJsonTime = jsonTime;
        //ELECAST TEST SongAudioSource.time = CurrentSeconds;
        SetVorbisPosition((long)(CurrentSeconds * bytesPerSecond));//ELECAST TEST
    }

    //ELECAST TEST
    private void SetVorbisPosition(long value)
    {
        if (!Vorbis.CanSeek) throw new InvalidOperationException("Cannot seek!");
        if (value < 0 || value > Vorbis.Length * Vorbis.WaveFormat.BlockAlign) throw new ArgumentOutOfRangeException(nameof(value));

        Vorbis.Seek(value / Vorbis.WaveFormat.BlockAlign);
    }

    public float FindRoundedBeatTime(float beat, float snap = -1) => bpmChangeGridContainer.FindRoundedBpmTime(beat, snap);

    public float GetBeatFromSeconds(float seconds) => Song.BeatsPerMinute / 60 * seconds;

    public float GetSecondsFromBeat(float beat) => 60 / Song.BeatsPerMinute * beat;

    private void ValidatePosition()
    {
        // Don't validate during playback
        if (IsPlaying) return;

        if (currentSeconds < 0) currentSeconds = 0;
        if (currentSongBpmTime < 0) currentSongBpmTime = 0;
        if (currentJsonTime < 0) currentJsonTime = 0;
        if (currentSeconds > BeatSaberSongContainer.Instance.LoadedSong.length)
        {
            CurrentSeconds = BeatSaberSongContainer.Instance.LoadedSong.length;
            SnapToGrid(true);
        }
    }
}
