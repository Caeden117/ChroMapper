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

    private bool metronomeActive = true;

    private float songSpeed = 1;

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        metronomeActive = Settings.Instance.MetronomeVolume != 0;
        
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);
        Settings.NotifyBySettingName("MetronomeVolume", value =>
        {
            if ((float)value != 0 && !metronomeActive)
            {
                metronomeActive = true;
            } 
            else if (metronomeActive && (float)value == 0)
            {
                metronomeActive = false;
            }
        });

        atsc.PlayToggle += OnPlayToggle;
    }

    private void LateUpdate()
    {
        if (metronomeActive && atsc.IsPlaying && !atsc.StopScheduled)
        {
            if (atsc.CurrentAudioBeats > queuedDingSongBpmTime)
            {
                var nextJsonTime = Mathf.Ceil(atsc.CurrentJsonTime);
                
                if (Mathf.Abs(Mathf.Floor(bpmChangeGridContainer.SongBpmTimeToJsonTime(atsc.CurrentAudioBeats))
                              - Mathf.Floor(atsc.CurrentJsonTime)) > 0.01f)
                {
                    nextJsonTime = Mathf.Ceil(nextJsonTime + 1f);
                }
                queuedDingSongBpmTime = bpmChangeGridContainer.JsonTimeToSongBpmTime(nextJsonTime);

                var delay = atsc.GetSecondsFromBeat(queuedDingSongBpmTime - atsc.CurrentAudioBeats) / songSpeed;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume,
                    1f, delay);

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
        if (Settings.Instance.MetronomeVolume == 0) return;
        if (!playing)
        {
            queuedDingSongBpmTime = 0;
        }
    }
}
