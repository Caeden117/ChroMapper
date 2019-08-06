using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioTimeSyncController : MonoBehaviour {

    [SerializeField] WaveformGenerator waveformGenerator;
    [SerializeField] AudioSource songAudioSource;

    [SerializeField] Renderer[] gridRenderers;
    [SerializeField] Renderer[] gridThickRenderers;

    [SerializeField] GameObject moveables;

    [SerializeField] int gridMeasureSnapping = 1;
    
    private AudioClip clip;
    private BeatSaberMap data;
    private BeatSaberSong.DifficultyData diff;

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
            data = BeatSaberSongContainer.Instance.map;
            diff = BeatSaberSongContainer.Instance.difficultyData;
            offsetMS = diff.offset / 1000;
            ResetTime();
            offsetBeat = currentBeat;
            gridStartPosition = currentBeat * (data._beatsPerBar / 4);
            IsPlaying = false;
            songAudioSource.clip = clip;
            UpdateMovables();
            waveformGenerator.InitializeWaveform(clip, data, diff);
        }
        catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void Update() {
        try {
            if (Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(1)) TogglePlaying();
            if (Input.GetKeyDown(KeyCode.Semicolon)) MoveToTimeInBeats(CurrentBeat + 1);
            if (Input.GetKeyDown(KeyCode.Escape)) ResetTime();

            if (IsPlaying) {
                UpdateMovables();
                CurrentSeconds = songAudioSource.time;
            } else {
                if (Input.GetAxis("Mouse ScrollWheel") != 0) {
                    MoveToTimeInBeats(CurrentBeat + ((1f / gridMeasureSnapping) * (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f)));
                    /*bool direction = Input.GetAxis("Mouse ScrollWheel") > 0;
                    float movAmount = ((1f / gridMeasureSnapping) / (data._beatsPerMinute / 60f) * (data._beatsPerBar / 16f)) * (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f);
                    SetTime(SongTime + movAmount);*/
                }
            }

        } catch (Exception e) {
            Debug.LogException(e);
        }
    }

    private void UpdateMovables() {
        float position = currentBeat * (data._beatsPerBar / 4);
        foreach (Renderer g in gridRenderers) g.material.SetFloat("_Offset", position - gridStartPosition);
        foreach (Renderer g in gridThickRenderers) g.material.SetFloat("_Offset", (position - gridStartPosition) / 4);
        moveables.transform.position = new Vector3(0, 0, position * -1);
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
        /*float bps = data._beatsPerMinute / 60f;
        float roundedTime = Mathf.RoundToInt(CurrentSeconds);
        CurrentSeconds = ((Mathf.RoundToInt(moveables.transform.position.z / 4) * -1) / bps);*/
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
        return (data._beatsPerMinute / 60) * seconds;
    }

    public float GetSecondsFromBeat(float beat) {
        return (60 / data._beatsPerMinute) * beat;
    }

    private void ValidatePosition() {
        if (currentSeconds < offsetMS) currentSeconds = offsetMS;
        if (currentBeat < offsetBeat) currentBeat = offsetBeat;
    }

}
