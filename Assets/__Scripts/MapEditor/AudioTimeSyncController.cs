using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AudioTimeSyncController : MonoBehaviour {

    [SerializeField] WaveformGenerator waveformGenerator;
    [SerializeField] AudioSource songAudioSource;

    [SerializeField] Renderer[] oneMeasureRenderers;
    [SerializeField] Renderer[] oneFourthMeasureRenderers;
    [SerializeField] Renderer[] oneEighthMeasureRenderers;
    [SerializeField] Renderer[] oneSixteenthMeasureRenderers;

    [SerializeField] GameObject moveables;
    [SerializeField] BPMChangesContainer bpmchanges;

    [SerializeField] public int gridMeasureSnapping = 1;

    private int gridStep = 0;
    private AudioClip clip;
    private BeatSaberMap data;
    private BeatSaberSong song;
    private BeatSaberSong.DifficultyBeatmap diff;

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
    private float offsetBeat;
    private float gridStartPosition;
    
    public Action OnTimeChanged;
    public Action<bool> OnPlayToggle;

    // Use this for initialization
    void Awake () {
        try
        {
            //Init dat stuff
            clip = BeatSaberSongContainer.Instance.loadedSong;
            song = BeatSaberSongContainer.Instance.song;
            data = BeatSaberSongContainer.Instance.map;
            diff = BeatSaberSongContainer.Instance.difficultyData;
            offsetMS = (diff.customData["_editorOffset"].AsFloat) / 1000;
            ResetTime();
            offsetBeat = currentBeat;
            gridStartPosition = currentBeat * EditorScaleController.EditorScale;
            IsPlaying = false;
            songAudioSource.clip = clip;
            UpdateMovables();
        }
        catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void Update() {
        try {
            if (Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(1)) TogglePlaying();
            //if (Input.GetKeyDown(KeyCode.Semicolon)) MoveToTimeInBeats(CurrentBeat + 1);
            if (Input.GetKeyDown(KeyCode.Semicolon)) ResetTime();

            if (IsPlaying) {
                CurrentSeconds = songAudioSource.time;
            } else {
                if (Input.GetAxis("Mouse ScrollWheel") != 0) {
                    if (Input.GetButton("LeftControl"))
                    {
                        gridStep += (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1 : -1);
                        if (gridStep < 0) gridStep = 0;
                        if (gridStep > 6) gridStep = 6; 
                        gridMeasureSnapping = Mathf.RoundToInt(Mathf.Pow(2, gridStep));
                    }
                    else
                        MoveToTimeInBeats(CurrentBeat + ((1f / gridMeasureSnapping) * (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f)));
                }
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
            g.material.SetFloat("_Offset", (position - gridStartPosition) / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale);
        }
        foreach (Renderer g in oneFourthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", ((position - gridStartPosition) * 4) / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 4); //1/4th measures
        }
        foreach (Renderer g in oneEighthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", ((position - gridStartPosition) * 8) / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 8); //1/8th measures
        }
        foreach (Renderer g in oneSixteenthMeasureRenderers)
        {
            g.material.SetFloat("_Offset", ((position - gridStartPosition) * 16) / EditorScaleController.EditorScale);
            g.material.SetFloat("_GridSpacing", EditorScaleController.EditorScale / 16); //1/16th measures
        }
        moveables.transform.localPosition = new Vector3(0, 0, position * -1);
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

    public void SnapToGrid() {
        float snapDouble = Mathf.Round(currentBeat / (1f / gridMeasureSnapping)) * (1f / gridMeasureSnapping);
        CurrentBeat = snapDouble + offsetBeat;
    }

    public void MoveToTimeInSeconds(float seconds) {
        if (IsPlaying) return;
        CurrentSeconds = seconds;
        songAudioSource.time = CurrentSeconds;
    }

    public void MoveToTimeInBeats(float beats) {
        if (IsPlaying) return;
        CurrentBeat = beats;
        songAudioSource.time = CurrentSeconds;
    }

    public float GetBeatFromSeconds(float seconds) {
        //return (bpmchanges.FindLastBPM(seconds) / 60) * seconds;
        float unmodifiedBeatTime = (song.beatsPerMinute / 60) * seconds;
        int lastBPMChangeIndex = 0;
        bpmchanges.FindLastBPM(unmodifiedBeatTime, out lastBPMChangeIndex);
        if (bpmchanges.loadedBPMChanges.Count == 0 || lastBPMChangeIndex == 0) return unmodifiedBeatTime;
        float totalBeat = bpmchanges.loadedBPMChanges[0].objectData._time;
        if (bpmchanges.loadedBPMChanges.Count >= 2)
        {
            for (int i = 0; i < bpmchanges.loadedBPMChanges.Count - 1; i++)
            {
                if (i >= lastBPMChangeIndex) break;
                float distance = bpmchanges.loadedBPMChanges[i + 1].bpmData._time - bpmchanges.loadedBPMChanges[i].bpmData._time;
                totalBeat += ((60 / song.beatsPerMinute) * distance) * (bpmchanges.loadedBPMChanges[i].bpmData._BPM / 60);
            }
        }
        totalBeat += (((60 / song.beatsPerMinute) * seconds) - bpmchanges.loadedBPMChanges[lastBPMChangeIndex].objectData._time) *
                (bpmchanges.loadedBPMChanges[lastBPMChangeIndex].bpmData._BPM / 60);
        return totalBeat;
    }

    public float GetSecondsFromBeat(float beat) {
        //return (60 / bpmchanges.lastBPM) * beat;
        if (bpmchanges.loadedBPMChanges.Count == 0 || beat <= bpmchanges.loadedBPMChanges[0].objectData._time)
            return (60 / song.beatsPerMinute) * beat;
        float totalSeconds = (60 / song.beatsPerMinute) * bpmchanges.loadedBPMChanges[0].objectData._time;
        int lastBPMChangeIndex = 0;
        float lastBPM = song.beatsPerMinute;
        if (bpmchanges.loadedBPMChanges.Count >= 2)
        { 
            for (int i = 0; i < bpmchanges.loadedBPMChanges.Count - 1; i++)
            {
                lastBPM = bpmchanges.loadedBPMChanges[i].bpmData._BPM;
                float distance = bpmchanges.loadedBPMChanges[i + 1].objectData._time - bpmchanges.loadedBPMChanges[i].objectData._time;
                if (i >= bpmchanges.lastCheckedBPMIndex) break;
                totalSeconds += (60 / bpmchanges.loadedBPMChanges[i].bpmData._BPM) * distance;
                lastBPMChangeIndex++;
            }
        }
        totalSeconds += (60 / bpmchanges.loadedBPMChanges[lastBPMChangeIndex].bpmData._BPM) *
            (beat - bpmchanges.loadedBPMChanges[lastBPMChangeIndex].objectData._time);
        return totalSeconds;
    }

    private void ValidatePosition() {
        if (currentSeconds < offsetMS) currentSeconds = offsetMS;
        if (currentBeat < offsetBeat) currentBeat = offsetBeat;
    }

}
