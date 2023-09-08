using System;
using UnityEngine;

public class MetronomeHandler : MonoBehaviour
{
    private static readonly int animatorBpm = Animator.StringToHash("BPM");
    [SerializeField] private BPMChangeGridContainer bpmChangeGridContainer;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioClip moreCowbellSound;
    [SerializeField] private AudioClip cowbellSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    public bool CowBell;
    private float queuedDingSongBpmTime = 0;

    private Animator metronomeUIAnimator;
    private bool metronomeUIDirection = true;

    private float metronomeVolume;

    private float songSpeed = 1;

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        atsc.PlayToggle += OnPlayToggle;
    }

    private void LateUpdate()
    {
        metronomeVolume = Settings.Instance.MetronomeVolume;
        if (metronomeVolume != 0f && atsc.IsPlaying && !atsc.StopScheduled)
        {
            if (atsc.CurrentAudioBeats > queuedDingSongBpmTime)
            {
                var nextJsonTime = Mathf.Ceil(atsc.CurrentJsonTime);
                queuedDingSongBpmTime = bpmChangeGridContainer.JsonTimeToSongBpmTime(nextJsonTime);

                var delay = atsc.GetSecondsFromBeat(queuedDingSongBpmTime - atsc.CurrentAudioBeats) / songSpeed;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, metronomeVolume, 1f, delay);

                if (!metronomeUI.activeInHierarchy) metronomeUI.SetActive(true);
                RunAnimation(60f / delay);
            }
        }
        else
        {
            metronomeUI.SetActive(false);
        }
    }

    private void OnDestroy() => atsc.PlayToggle -= OnPlayToggle;

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10f;
    }

    private void RunAnimation(float inferredBpm)
    {
        if (!metronomeUIAnimator.gameObject.activeInHierarchy)
            return;

        metronomeUIAnimator.StopPlayback();
        metronomeUIAnimator.SetFloat(animatorBpm, inferredBpm);
        metronomeUIAnimator.Play(metronomeUIDirection ? "Metronome_R2L" : "Metronome_L2R");
        metronomeUIDirection = !metronomeUIDirection;
    }

    private void OnPlayToggle(bool playing)
    {
        if (metronomeVolume == 0) return;
        if (!playing)
        {
            queuedDingSongBpmTime = 0;
        }
    }
}
