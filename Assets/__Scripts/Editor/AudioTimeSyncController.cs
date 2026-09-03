using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Beatmap.Info;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class AudioTimeSyncController : MonoBehaviour,
                                       CMInput.IPlaybackActions,
                                       CMInput.ITimelineActions,
                                       CMInput.ITimelineNavigationActions,
                                       IEditorStateProvider
{
    public static readonly string PrecisionSnapName = "PrecisionSnap";

    private static readonly int songTime = Shader.PropertyToID("_SongTime");
    private static readonly int songTimeSeconds = Shader.PropertyToID("_SongTimeSeconds");
    private static readonly int songTimeOrigin = Shader.PropertyToID("_SongTimeOrigin");
    private static readonly int viewStart = Shader.PropertyToID("_ViewStart");
    private static readonly int viewEnd = Shader.PropertyToID("_ViewEnd");

    private const float cancelPlayInputDuration = 0.3f;

    [FormerlySerializedAs("songAudioSource")]
    public AudioSource SongAudioSource;

    [SerializeField] private AudioSource waveformSource;

    [SerializeField] private GameObject moveables;

    [SerializeField] private TracksManager tracksManager;

    // TODO: track should subscribe to this, not ATSC holding these
    [SerializeField] public List<Track> otherTracks;

    [FormerlySerializedAs("bpmChangesContainer")] [SerializeField]
    private BPMChangeGridContainer bpmChangeGridContainer;

    [SerializeField] private GridRenderingController gridRenderingController;
    [SerializeField] private CustomStandaloneInputModule customStandaloneInputModule;
    [SerializeField] private EditModeContext editModeContext;

    public BaseInfo MapInfo;

    [SerializeField] private float currentSeconds;

    [FormerlySerializedAs("stopScheduled")]
    public bool StopScheduled;

    [FormerlySerializedAs("initialized")] public bool Initialized;
    private int gridMeasureSnapping = 1;
    private float audioLatencyCompensationSeconds;

    private AudioClip clip;

    private bool controlSnap;
    private bool levelLoaded;

    public event Action<int> OnGridMeasureSnappingChanged;
    public event Action<bool> OnPlayToggled;
    public event Action<float> OnVisualBeatOriginChanged;
    public event Action OnTimeChangedEarly;
    public event Action OnTimeChanged;

    // Keep the map cursor with the controller that owns song-time conversion and track positioning.
    public string StateKey => "currentJsonTime";

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
            if (gridMeasureSnapping != old) OnGridMeasureSnappingChanged?.Invoke(value);
        }
    }

    [SerializeField] private float currentJsonTime;

    public float CurrentJsonTime
    {
        get => currentJsonTime;
        private set
        {
            currentJsonTime = value;
            currentSongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(value);
            currentSeconds = GetSecondsFromBeat(currentSongBpmTime);
            ValidatePosition();
            UpdateMovables();
        }
    }

    [SerializeField] private float originBeat;

    // Changes the "origin beat", or the visual position of beat 0.
    // Used in GLS event box groups since all event boxes are relative to the beat of the event box group.
    // This affects grid lines, measure line text, and placement systems which require beat snapping
    public float VisualBeatOrigin
    {
        get => originBeat;
        set
        {
            originBeat = value;
            OnVisualBeatOriginChanged?.Invoke(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    // Yep, you guessed it.
    public float VisualBeatOriginJsonTime
    {
        get => (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(VisualBeatOrigin);
        set => VisualBeatOrigin = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(value);
    }

    [Obsolete(
        "This is for existing dev plugin compatibility. Use CurrentSongBpmTime, CurrentJsonTime, or CurrentSeconds.",
        true)]
    public float CurrentBeat { get => CurrentSongBpmTime; }

    [SerializeField] private float currentSongBpmTime;

    public float CurrentSongBpmTime
    {
        get => currentSongBpmTime;
        private set
        {
            currentSongBpmTime = value;
            currentJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(value);
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
            currentJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(currentSongBpmTime);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public float CurrentAudioSeconds =>
        SongAudioSource.clip is null ? 0f : SongAudioSource.timeSamples / (float)SongAudioSource.clip.frequency;

    public float CurrentAudioBeats => GetBeatFromSeconds(CurrentAudioSeconds);

    public bool IsPlaying { get; private set; }

    public bool IsSnapped
    {
        get
        {
            if (IsPlaying) return false;
            // Grid lines are relative to VisualBeatOrigin, so snapped-state detection must use the same interval
            // calculation as cursor restoration and SnapToGrid instead of assuming every grid begins at beat zero.
            return Mathf.Approximately(currentJsonTime, GetNearestGridLineJsonTime(currentJsonTime));
        }
    }

    // Use this for initialization
    private void Start()
    {
        try
        {
            //Init dat stuff
            clip = BeatSaberSongContainer.Instance.LoadedSong;
            // Song = BeatSaberSongContainer.Instance.Song;
            MapInfo = BeatSaberSongContainer.Instance.Info;
            ResetTime();
            IsPlaying = false;
            SongAudioSource.clip = clip;
            SongAudioSource.volume = Settings.Instance.SongVolume;
            waveformSource.clip = clip;
            UpdateMovables();
            if (Settings.NonPersistentSettings.ContainsKey(PrecisionSnapName))
                GridMeasureSnapping = (int)Settings.NonPersistentSettings[PrecisionSnapName];
            OnGridMeasureSnappingChanged?.Invoke(GridMeasureSnapping);
            LoadInitialMap.OnLevelLoaded += OnLevelLoaded;
            editModeContext.OnEditModeChanged += OnEditModeChanged;
            Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
            Settings.NotifyBySettingName("SongVolume", UpdateSongVolume);
            Settings.NotifyBySettingName(nameof(Settings.TrackLength), UpdateTrackLength);

            Initialized = true;
            // Register after this controller can safely convert restored JSON time into song time.
            EditorStateService.Register(this);
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
                CurrentSeconds =
                    time
                    + (correction * (Time.deltaTime * (songSpeed / 10f)))
                    - (audioLatencyCompensationSeconds * (songSpeed / 10f));
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        EditorStateService.Unregister(this);
        clip = null;
        LoadInitialMap.OnLevelLoaded -= OnLevelLoaded;
        Settings.ClearSettingNotifications("SongSpeed");
        Settings.ClearSettingNotifications("SongVolume");
    }

    // Save the timeline denominator beside the cursor so each map restores the grid on which that cursor was authored.
    public void CaptureEditorState(SimpleJSON.JSONObject data)
    {
        data["value"] = CurrentJsonTime;
        data["gridMeasureSnapping"] = GridMeasureSnapping;
    }

    // Restore grid precision before resolving the cursor so Info.dat float noise snaps to this map's authored interval.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("gridMeasureSnapping"))
        {
            GridMeasureSnapping = Math.Max(1, data["gridMeasureSnapping"].AsInt);
        }

        if (data.HasKey("value"))
        {
            // RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot moves once at the final snapped
            // time so lighting never renders the serialized epsilon-offset cursor as an intermediate state.
            var snappedJsonTime = GetNearestGridLineJsonTime(data["value"].AsFloat);
            MoveToJsonTime(snappedJsonTime);
        }
    }

    private bool toggledPlayingPreviousFrame;

    private IEnumerator TrackToggledPlayingPreviousFrame()
    {
        toggledPlayingPreviousFrame = true;
        yield return null;
        toggledPlayingPreviousFrame = false;
    }

    public void OnTogglePlaying(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            TogglePlaying();

            // On maps with dense lighting, it can take longer than the cancelPlayInputDuration to start playing.
            // When this happens it becomes impossible to play without holding so track if this was performed on the
            // previous frame to determine if we want to ignore the cancelPlaying behaviour
            if (IsPlaying) StartCoroutine(TrackToggledPlayingPreviousFrame());
        }

        // if play is held and released a significant time later, cancel playing instead of merely toggling
        if (!CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.IPlaybackActions))
            && context is { canceled: true, duration: >= cancelPlayInputDuration }
            && !toggledPlayingPreviousFrame)
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
        if (!KeybindsController.IsMouseInWindow
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
        {
            return;
        }

        if (controlSnap
            && preciselyControlSnap
            && IsCursorIntervalOwnedByHoveredObject())
        {
            return;
        }

        var value = context.ReadValue<float>();
        if (context.performed)
        {
            if (controlSnap)
            {
                float scrollDirection;
                if (Settings.Instance.InvertPrecisionScroll)
                    scrollDirection = value > 0 ? 0.5f : 2;
                else
                    scrollDirection = value > 0 ? 2 : 0.5f;
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
                var snapped = IsSnapped;
                var minimumJsonTime = Settings.Instance.AllowGLSEventGridScrollingBeforeGroup
                    ? 0
                    : VisualBeatOriginJsonTime;
                var targetJsonTime = Mathf.Max(minimumJsonTime, CurrentJsonTime + beatShiftRaw);
                if (Mathf.Approximately(targetJsonTime, CurrentJsonTime)) return;

                MoveToJsonTime(targetJsonTime);
                if (snapped) SnapToGrid(true);
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
        if (!KeybindsController.IsMouseInWindow
            || customStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true))
        {
            return;
        }

        // VNJS, GLS and Basic Event hover edits own [Ctrl+]Shift+Scroll instead of the global cursor interval.
        if (IsCursorIntervalOwnedByHoveredObject())
        {
            return;
        }

        var value = context.ReadValue<float>();
        if (context.performed)
        {
            float scrollDirection;
            if (Settings.Instance.InvertPrecisionScroll)
                scrollDirection = value > 0 ? 0.5f : 2;
            else
                scrollDirection = value > 0 ? 2 : 0.5f;

            var addition = scrollDirection > 1 ? 1 : -1;
            GridMeasureSnapping = Mathf.Clamp(GridMeasureSnapping + addition, 1, 64);
        }
    }

    private static bool IsCursorIntervalOwnedByHoveredObject()
    {
        return GLSEventInputHoverTracker.IsHovering
            || BeatmapEventInputController.IsCursorIntervalOwnedByPointer()
            || BeatmapNJSEventInputController.IsCursorIntervalOwnedByPointer();
    }

    public void OnChangePrecisionModifier(InputAction.CallbackContext context) => controlSnap = context.performed;

    public void OnPreciseSnapModification(InputAction.CallbackContext context) =>
        preciselyControlSnap = context.performed;

    public void OnGoToBeat(InputAction.CallbackContext context)
    {
        if (!context.performed
            || editModeContext.EditingMode.HasFlag(EditingMode.GLS)
            || editModeContext.EditingMode.HasFlag(EditingMode.EventBox))
            return;

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

        var snapped = IsSnapped;
        CurrentJsonTime += (1f / gridMeasureSnapping);
        if (snapped) SnapToGrid(true);
    }

    public void OnMoveCursorBackward(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        var snapped = IsSnapped;
        var minimumJsonTime = Settings.Instance.AllowGLSEventGridScrollingBeforeGroup
            ? 0
            : VisualBeatOriginJsonTime;
        var targetJsonTime = Mathf.Max(minimumJsonTime, CurrentJsonTime - (1f / gridMeasureSnapping));
        if (Mathf.Approximately(targetJsonTime, CurrentJsonTime)) return;

        CurrentJsonTime = targetJsonTime;
        if (snapped) SnapToGrid(true);
    }

    private void UpdateSongVolume(object obj) => SongAudioSource.volume = (float)obj;

    private void UpdateSongSpeed(object obj) => songSpeed = (float)obj;

    private void UpdateTrackLength(object _) => UpdateMovables();

    // Always reset visual beat origin when we switch
    // Ideally, each user who changes the beat origin should also reset it, but this is a safety measure
    // There are too many places to change edit mode contexts for that to be reliable
    private void OnEditModeChanged(EditingMode mode)
    {
        if (VisualBeatOrigin != 0) VisualBeatOrigin = 0;
    }

    private void OnLevelLoaded() => levelLoaded = true;

    private void UpdateMovables()
    {
        Shader.SetGlobalFloat(songTime, currentSongBpmTime);
        Shader.SetGlobalFloat(songTimeSeconds, currentSeconds);
        Shader.SetGlobalFloat(songTimeOrigin, VisualBeatOriginJsonTime);

        // set view range based on track length
        Shader.SetGlobalFloat(viewStart, GetSecondsFromBeat(currentSongBpmTime - (Settings.Instance.TrackLength / 4f)));
        Shader.SetGlobalFloat(viewEnd, GetSecondsFromBeat(currentSongBpmTime + Settings.Instance.TrackLength));

        var position = currentSongBpmTime * EditorScaleController.EditorScale;

        tracksManager.UpdatePosition(-position);
        foreach (var track in otherTracks) track.UpdatePosition(-position);

        // TODO(Caeden): what is the difference between these events
        OnTimeChangedEarly?.Invoke();
        OnTimeChanged?.Invoke();
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
                ResetTime();
            }

            playStartTime = CurrentSeconds;
            SongAudioSource.time = CurrentSeconds;
            SongAudioSource.Play();

            audioLatencyCompensationSeconds = Settings.Instance.AudioLatencyCompensation / 1000f;
            CurrentSeconds -= audioLatencyCompensationSeconds * (songSpeed / 10f);
        }
        else
        {
            SongAudioSource.Stop();
            SnapToGrid();
        }

        OnPlayToggled?.Invoke(IsPlaying);
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
        currentJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(songBpmTime);

        SnapToGrid();
        SongAudioSource.time = CurrentSeconds;
    }

    // Cursor restore and interactive snapping must resolve exactly the same visual-origin-relative interval line.
    private float GetNearestGridLineJsonTime(float jsonTime)
    {
        var offsetTime = VisualBeatOriginJsonTime;
        var snappedJsonTime = (float)Math.Round(
                (jsonTime - offsetTime) * GridMeasureSnapping,
                MidpointRounding.AwayFromZero)
            / GridMeasureSnapping;

        return snappedJsonTime + offsetTime;
    }

    public void SnapToGrid(bool positionValidated = false)
    {
        // Keep manual snapping on the shared interval calculation used by editor-state restoration.
        var jsonTime = GetNearestGridLineJsonTime(CurrentJsonTime);

        currentJsonTime = jsonTime;
        currentSongBpmTime = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(jsonTime);
        currentSeconds = GetSecondsFromBeat(currentSongBpmTime);

        if (!positionValidated) ValidatePosition();
        UpdateMovables();
    }

    public void RefreshGridSnapping() => OnGridMeasureSnappingChanged?.Invoke(GridMeasureSnapping);

    public void MoveToTimeInSeconds(float seconds)
    {
        if (IsPlaying) return;
        CurrentSeconds = seconds;
        SongAudioSource.time = CurrentSeconds;
    }

    [Obsolete("This is for existing dev plugin compatibility. Use MoveToSongBpmTime or MoveToJsonTime.", true)]
    public void MoveToTimeInBeats(float beats) => MoveToSongBpmTime(beats);

    public void MoveToSongBpmTime(float songBpmTime)
    {
        if (IsPlaying) return;
        CurrentSongBpmTime = songBpmTime;
        SongAudioSource.time = CurrentSeconds;
    }

    public void MoveToJsonTime(float jsonTime)
    {
        if (IsPlaying) return;
        CurrentJsonTime = jsonTime;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetBeatFromSeconds(float seconds) => MapInfo.BeatsPerMinute / 60 * seconds;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetSecondsFromBeat(float beat) => 60 / MapInfo.BeatsPerMinute * beat;

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

        // GLS event times are relative to their group, so keep the grid at or after relative time zero by default.
        if (!Settings.Instance.AllowGLSEventGridScrollingBeforeGroup && currentSongBpmTime < VisualBeatOrigin)
        {
            currentSongBpmTime = VisualBeatOrigin;
            currentJsonTime = VisualBeatOriginJsonTime;
            currentSeconds = GetSecondsFromBeat(currentSongBpmTime);
        }
    }
}
