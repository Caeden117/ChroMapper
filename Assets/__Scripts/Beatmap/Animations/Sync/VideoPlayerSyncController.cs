using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoPlayerSyncController : MonoBehaviour, ISync
{
    public AudioTimeSyncController Atsc;

    private bool seeking;
    private float startTime;
    private VideoPlayer videoPlayer = null!;

    private float SongTime => Atsc.CurrentSeconds - startTime;

    public void SetStartTime(float time) => startTime = time;

    private void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.errorReceived += OnErrorRecieved;
        videoPlayer.prepareCompleted += OnPrepareCompleted;
        videoPlayer.skipOnDrop = false;
    }

    private void OnDestroy()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (videoPlayer != null)
        {
            videoPlayer.errorReceived -= OnErrorRecieved;
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
        }

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (Atsc != null) Atsc.OnPlayToggled -= OnStateChange;
    }

    private void OnEnable()
    {
        if (Atsc != null) Atsc.OnPlayToggled += OnStateChange;
        StartCoroutine(Prepare());
    }

    private void OnDisable()
    {
        if (Atsc != null) Atsc.OnPlayToggled -= OnStateChange;
        videoPlayer.seekCompleted -= OnSeekCompleted;
        seeking = false;
        videoPlayer.Pause();
        StopAllCoroutines();
    }

    private void OnErrorRecieved(VideoPlayer _, string error) => Debug.LogError(error);

    private void OnPrepareCompleted(VideoPlayer _) => OnStateChange();

    private void OnSeekCompleted(VideoPlayer _)
    {
        videoPlayer.seekCompleted -= OnSeekCompleted;
        StartCoroutine(SeekCompleteDelay());
    }

    private void OnStateChange(bool _) => OnStateChange();

    private void OnStateChange()
    {
        if (!isActiveAndEnabled || !videoPlayer.isPrepared) return;

        switch (Atsc.IsPlaying)
        {
            case true:
                ResyncTime();
                videoPlayer.Play();
                break;
            case false:
                videoPlayer.Pause();
                break;
        }
    }

    private IEnumerator Prepare()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        yield return new WaitUntil(() => videoPlayer != null && videoPlayer.isActiveAndEnabled);
        videoPlayer.Prepare();
    }

    private void ResyncTime()
    {
        if (seeking) return;

        seeking = true;
        videoPlayer.playbackSpeed = Atsc.SongSpeed / 10f;
        videoPlayer.seekCompleted += OnSeekCompleted;
        videoPlayer.time = SongTime;
    }

    private IEnumerator SeekCompleteDelay()
    {
        yield return new WaitForEndOfFrame();
        seeking = false;
    }

    private void Update()
    {
        if (!videoPlayer.isPrepared) return;

        if (Math.Abs(videoPlayer.time - SongTime) > 0.2) ResyncTime();
    }
}
