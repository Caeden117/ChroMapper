using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AudioTimeSyncController : MonoBehaviour {
    [SerializeField] AudioSource songAudioSource;
    [SerializeField] AudioSource waveformSource;

    [SerializeField] Renderer[] oneMeasureRenderers;
    [SerializeField] Renderer[] oneFourthMeasureRenderers;
    [SerializeField] Renderer[] oneEighthMeasureRenderers;
    [SerializeField] Renderer[] oneSixteenthMeasureRenderers;

    [SerializeField] GameObject moveables;
    [SerializeField] BPMChangesContainer bpmchanges;

    public int gridMeasureSnapping
    {
        get { return _gridMeasureSnapping; }
        set
        {
            int old = _gridMeasureSnapping;
            _gridMeasureSnapping = value;
            if (_gridMeasureSnapping != old) GridMeasureSnappingChanged?.Invoke(value);
        }
    }

    private int gridStep = 0;
    private AudioClip clip;
    private BeatSaberSong song;
    private int _gridMeasureSnapping = 1;

    [SerializeField] private float currentBeat = 0;
    [SerializeField] private float currentSeconds = 0;

    public float CurrentBeat {
        get { return currentBeat; }
        private set {
            currentBeat = value;
            currentSeconds = GetSecondsFromBeat(value);
            ValidatePosition();
            UpdateMovables();
        }
    }

    public float CurrentSeconds {
        get { return currentSeconds; }
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
            waveformSource.clip = clip;
            UpdateMovables();
        }
        catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void Update() {
        try {
            if (IsPlaying)
            {
                CurrentSeconds = songAudioSource.time - offsetMS;
                if (!songAudioSource.isPlaying) TogglePlaying();
            }
            else
            {
                if (Input.GetAxis("Mouse ScrollWheel") != 0 && !KeybindsController.AltHeld)
                {
                    if (KeybindsController.CtrlHeld)
                    {
                        float scrollDirection = Input.GetAxis("Mouse ScrollWheel") > 0 ? 2 : 0.5f;
                        gridMeasureSnapping = Mathf.Clamp(Mathf.RoundToInt(gridMeasureSnapping * scrollDirection), 1, 64);
                    }
                    else
                        MoveToTimeInBeats(CurrentBeat + (1f / gridMeasureSnapping * (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f)));
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(1) && 
                !NodeEditorController.IsActive && !PersistentUI.Instance.InputBox_IsEnabled) TogglePlaying();
            if (Input.GetKeyDown(KeyCode.Semicolon)) ResetTime();

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void UpdateMovables() {
        float position = currentBeat * EditorScaleController.EditorScale;
        gridStartPosition = offsetBeat * EditorScaleController.EditorScale;
        foreach (Renderer g in oneMeasureRenderers)
        {
            g.material.SetFloat("_Offset", (position - gridStartPosition) / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale);
        }
        foreach (Renderer g in oneFourthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", (position - gridStartPosition) * 4 / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 4); //1/4th measures
        }
        foreach (Renderer g in oneEighthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", (position - gridStartPosition) * 8 / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 8); //1/8th measures
        }
        foreach (Renderer g in oneSixteenthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", (position - gridStartPosition) * 16 / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 16); //1/16th measures
        }
        moveables.transform.localPosition = new Vector3(0, 0, (position) * -1);
    }

    private void ResetTime() {
        CurrentSeconds = offsetMS;
    }

    public void TogglePlaying() {
        IsPlaying = !IsPlaying;
        if (IsPlaying) {
            songAudioSource.time = CurrentSeconds;
            songAudioSource.Play();
        } else {
            songAudioSource.Stop();
            SnapToGrid();
        }
        if (OnPlayToggle != null) OnPlayToggle(IsPlaying);
    }

    public void SnapToGrid(bool positionValidated = false) {
        float snapDouble = (float)Math.Round(currentBeat / (1f / gridMeasureSnapping), MidpointRounding.AwayFromZero) * (1f / gridMeasureSnapping);
        currentBeat = snapDouble + offsetBeat;
        currentSeconds = GetSecondsFromBeat(snapDouble + offsetBeat);
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

    public float GetBeatFromSeconds(float seconds) {
        //return (bpmchanges.FindLastBPM(seconds) / 60) * seconds;
        float unmodifiedBeatTime = song.beatsPerMinute / 60 * seconds;
        bpmchanges.FindLastBPM(unmodifiedBeatTime, out int lastBPMChangeIndex);
        if (bpmchanges.LoadedContainers.Count == 0 || lastBPMChangeIndex == -1) return unmodifiedBeatTime;
        float totalBeat = bpmchanges.LoadedContainers[0].objectData._time;
        if (bpmchanges.LoadedContainers.Count >= 2)
        {
            for (int i = 0; i < bpmchanges.LoadedContainers.Count - 1; i++)
            {
                BeatmapBPMChangeContainer change = bpmchanges.LoadedContainers[i + 1] as BeatmapBPMChangeContainer;
                if (i >= lastBPMChangeIndex) break;
                float distance = change.bpmData._time - (bpmchanges.LoadedContainers[i] as BeatmapBPMChangeContainer).bpmData._time;
                totalBeat += 60 / song.beatsPerMinute * distance * (change.bpmData._BPM / 60);
            }
        }
        BeatmapBPMChangeContainer lastChange = bpmchanges.LoadedContainers[lastBPMChangeIndex] as BeatmapBPMChangeContainer;
        totalBeat += (seconds - (60 / song.beatsPerMinute * bpmchanges.LoadedContainers[lastBPMChangeIndex].objectData._time)) *
                (lastChange.bpmData._BPM / 60);
        return totalBeat;
    }

    public float GetSecondsFromBeat(float beat) {
        //return (60 / bpmchanges.lastBPM) * beat;
        if (bpmchanges.LoadedContainers.Count == 0 || beat <= bpmchanges.LoadedContainers[0].objectData._time)
            return 60 / song.beatsPerMinute * beat;
        float totalSeconds = 60 / song.beatsPerMinute * bpmchanges.LoadedContainers[0].objectData._time;
        int lastBPMChangeIndex = 0;
        if (bpmchanges.LoadedContainers.Count >= 2)
        { 
            for (int i = 0; i < bpmchanges.LoadedContainers.Count - 1; i++)
            {
                BeatmapBPMChangeContainer change = bpmchanges.LoadedContainers[i + 1] as BeatmapBPMChangeContainer;
                float distance = bpmchanges.LoadedContainers[i + 1].objectData._time - bpmchanges.LoadedContainers[i].objectData._time;
                if (i >= bpmchanges.lastCheckedBPMIndex) break;
                totalSeconds += 60 / change.bpmData._BPM * (distance * (song.beatsPerMinute / 60));
                lastBPMChangeIndex++;
            }
        }
        BeatmapBPMChangeContainer lastChange = bpmchanges.LoadedContainers[lastBPMChangeIndex] as BeatmapBPMChangeContainer;
        totalSeconds += (60 / lastChange.bpmData._BPM) *
            (beat - bpmchanges.LoadedContainers[lastBPMChangeIndex].objectData._time);
        return totalSeconds;
    }

    private void ValidatePosition() {
        if (currentSeconds < offsetMS) currentSeconds = offsetMS;
        if (currentBeat < offsetBeat) currentBeat = offsetBeat;
        if (currentSeconds > BeatSaberSongContainer.Instance.loadedSong.length)
        {
            CurrentSeconds = BeatSaberSongContainer.Instance.loadedSong.length;
            SnapToGrid(true);
        }
    }

}
