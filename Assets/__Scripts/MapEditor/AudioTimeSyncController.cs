using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AudioTimeSyncController : MonoBehaviour, CMInput.IPlaybackActions, CMInput.ITimelineActions
{
    public static readonly string PrecisionSnapName = "PrecisionSnap";

    [SerializeField] public AudioSource songAudioSource;
    [SerializeField] AudioSource waveformSource;

    [SerializeField] Renderer[] oneMeasureRenderers;
    [SerializeField] Renderer[] oneFourthMeasureRenderers;
    [SerializeField] Renderer[] oneEighthMeasureRenderers;
    [SerializeField] Renderer[] oneSixteenthMeasureRenderers;

    [SerializeField] GameObject moveables;
    [SerializeField] TracksManager tracksManager;
    [SerializeField] Track[] otherTracks;
    [SerializeField] BPMChangesContainer bpmChangesContainer;

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

    private int gridStep = 0;
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

    private float offsetMS;
    public float offsetBeat { get; private set; } = -1;
    public float gridStartPosition { get; private set; } = -1;
    private bool levelLoaded = false;
    
    public Action OnTimeChanged;
    public Action<bool> OnPlayToggle;
    public Action<int> GridMeasureSnappingChanged;
    
    private static readonly int Offset = Shader.PropertyToID("_Offset");
    private static readonly int GridSpacing = Shader.PropertyToID("_GridSpacing");

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
            waveformSource.clip = clip;
            UpdateMovables();
            if (Settings.NonPersistentSettings.ContainsKey(PrecisionSnapName))
            {
                gridMeasureSnapping = (int)Settings.NonPersistentSettings[PrecisionSnapName];
            }
            LoadInitialMap.LevelLoadedEvent += OnLevelLoaded;
        }
        catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void OnLevelLoaded()
    {
        levelLoaded = true;
    }

    private void OnDestroy()
    {
        LoadInitialMap.LevelLoadedEvent -= OnLevelLoaded;
    }

    private void Update() {
        try
        {
            if (!levelLoaded) return;
            if (IsPlaying)
            {
                CurrentSeconds = songAudioSource.time - offsetMS;
                if (!songAudioSource.isPlaying) TogglePlaying();
            }

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void UpdateMovables() {
        float position = currentBeat * EditorScaleController.EditorScale;
        gridStartPosition = offsetBeat * EditorScaleController.EditorScale;
        foreach (Renderer g in oneMeasureRenderers)
        {
            g.material.SetFloat(Offset, (position - gridStartPosition));
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f); //1 measure
        }
        foreach (Renderer g in oneFourthMeasureRenderers)
        {
            g.material.SetFloat(Offset, (position - gridStartPosition));
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / 4f); //1/4th measures
        }
        foreach (Renderer g in oneEighthMeasureRenderers)
        {
            g.material.SetFloat(Offset, (position - gridStartPosition));
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / 8f); //1/8th measures
        }
        foreach (Renderer g in oneSixteenthMeasureRenderers)
        {
            g.material.SetFloat(Offset, (position - gridStartPosition));
            g.material.SetFloat(GridSpacing, EditorScaleController.EditorScale / 4f / 16f); //1/16th measures
        }
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
            if (CurrentSeconds >= songAudioSource.clip.length)
            {
                Debug.LogError(":hyperPepega: :mega: STOP TRYING TO PLAY THE SONG AT THE VERY END");
            }
            else
            {
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

    public void MoveToTimeInBeats(float beats) {
        if (IsPlaying) return;
        CurrentBeat = beats;
        songAudioSource.time = CurrentSeconds + offsetBeat;
    }

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
    }

    public void OnResetTime(InputAction.CallbackContext context)
    {
        if (context.performed) ResetTime();
    }

    public void OnChangeTimeandPrecision(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (!KeybindsController.AltHeld && context.performed)
        {
            if (KeybindsController.CtrlHeld)
            {
                float scrollDirection;
                if (Settings.Instance.InvertPrecisionScroll) scrollDirection = value > 0 ? 0.5f : 2;
                else scrollDirection = value > 0 ? 2 : 0.5f;
                gridMeasureSnapping = Mathf.Clamp(Mathf.RoundToInt(gridMeasureSnapping * scrollDirection), 1, 64);
            }
            else
            {
                if (Settings.Instance.InvertScrollTime) value *= -1;
                MoveToTimeInBeats(CurrentBeat + (1f / gridMeasureSnapping * (value > 0 ? 1f : -1f)));
            }
        }
    }
}
