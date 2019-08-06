using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("Use AudioTimeSyncController", true)]
public class AudioTimeSyncControllerBak : MonoBehaviour {
/*
    [SerializeField] WaveformGenerator waveformGenerator;
    [SerializeField] AudioSource cameraAudioSource;
    [SerializeField] Renderer[] gridRenderers;
    [SerializeField] Renderer[] gridThickRenderers;
    [SerializeField] GameObject moveables;
    [SerializeField] int gridMeasureSnapping = 1;
    private int gridMeasureStep = 1;
    //1 is every measure (4 beats), 2 is every half measure, 4

    private AudioClip clip;
    private BeatSaberMap data;
    private BeatSaberSong.DifficultyBeatmap diff;

    [SerializeField] private float _songTime; //Serializing this to expose in editor for debugging
    public float SongTime { 
        get { return _songTime; }
        set { _songTime = value; }
    }
    public bool IsPlaying { get; private set; }

    public float NoteTime {
        get {
            //return (data._beatsPerMinute / 60) * SongTime * (data._beatsPerBar / 4) - offsetDistance;
            return ((data._beatsPerMinute / 60) * (SongTime));
        }
    }

    [SerializeField] private float currentBeat = 0;

    private float scrollDelay = 0.25f;
    private float waitForDelay = 0;
    private float position;
    private float startPosition;
    private float offsetDistance;
    private float offsetMS;
    
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
            SongTime = offsetMS;
            startPosition = (data._beatsPerMinute / 60) * (SongTime) * (data._beatsPerBar / 4);// - offsetDistance;
            IsPlaying = false;
            cameraAudioSource.clip = clip;
            //Misc. stuff that helps me in maths
            offsetDistance = (data._beatsPerMinute / 60) * offsetMS * (data._beatsPerBar / 4);
            //Set initial positions
            UpdateMovables();
            waveformGenerator.InitializeWaveform(clip, data, diff);
        }
        catch { }
	}

    private void Start() {
        OnTimeChanged.Invoke();
    }

    void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.Space) && !Input.GetMouseButton(1)) TogglePlaying();
            if (Input.GetKeyDown(KeyCode.Semicolon)) SetTimeByBeats(currentBeat + 1);
            if (Input.GetKeyDown(KeyCode.Escape)) SetTime(0);
            if (IsPlaying)
            {
                UpdateMovables();
                SongTime = cameraAudioSource.time;
            }
            else
            {
                waitForDelay -= Time.deltaTime;
                if (waitForDelay < 0) waitForDelay = 0;
                if (Input.GetAxis("Mouse ScrollWheel") != 0)
                {
                    bool direction = Input.GetAxis("Mouse ScrollWheel") > 0;
                    float movAmount = ((1f / gridMeasureSnapping) / (data._beatsPerMinute / 60f) * (data._beatsPerBar / 16f)) * (Input.GetAxis("Mouse ScrollWheel") > 0 ? 1f : -1f);
                    SetTime(SongTime + movAmount);
                }
            }
        }
        catch { }
    }

    public void SetTimeByBeats(float beats) {
        SetTime((data._beatsPerMinute / 60f) * (data._beatsPerBar / 16f) * beats);
    }

    public void SetTime(float timeInSeconds)
    {
        if (timeInSeconds <= offsetMS) timeInSeconds = offsetMS;
        SongTime = timeInSeconds;
        cameraAudioSource.time = timeInSeconds;
        UpdateMovables();
        if (OnTimeChanged != null) OnTimeChanged.Invoke();
        UpdateBeat();
    }

    public void UpdateBeat() {
        currentBeat = SongTime * (60 / data._beatsPerMinute) * (data._beatsPerBar / 16f);// (data._beatsPerMinute / 60f);
    }
    
    public void UpdateMovables() {
        position = (data._beatsPerMinute / 60) * (SongTime) * (data._beatsPerBar / 4);// - offsetDistance;
        foreach (Renderer g in gridRenderers) g.material.SetFloat("_Offset", position - startPosition);
        foreach (Renderer g in gridThickRenderers) g.material.SetFloat("_Offset", (position - startPosition) / 4);
        moveables.transform.position = new Vector3(0, 0, position * -1);
    }

    public void TogglePlaying()
    {
        IsPlaying = !IsPlaying;
        if (IsPlaying)
        {
            cameraAudioSource.time = SongTime;
            cameraAudioSource.Play();
        }
        else
        {
            cameraAudioSource.Stop();
            SnapToGrid();
        }
        if (OnPlayToggle != null) OnPlayToggle(IsPlaying);
    }

    public void SnapToGrid()
    {
        float bps = data._beatsPerMinute / 60f;
        float roundedTime = Mathf.RoundToInt(SongTime);
        SetTime((Mathf.RoundToInt(moveables.transform.position.z / 4) * -1) / bps);
    }
*/}
