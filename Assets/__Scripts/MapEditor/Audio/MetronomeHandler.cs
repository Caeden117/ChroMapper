using System;
using UnityEngine;

public class MetronomeHandler : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioClip moreCowbellSound;
    [SerializeField] private AudioClip cowbellSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    private float lastBPM = 100;
    private float beatProgress = 0;
    private BeatmapBPMChange lastBPMChange = null;
    private Animator metronomeUIAnimator;
    private static readonly int Bpm = Animator.StringToHash("BPM");
    private bool metronomeUIDirection = true;
    public bool CowBell;
    private bool CowBellPlayed;

    private float songSpeed = 1;

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        lastBPM = atsc.song.beatsPerMinute;
        atsc.OnPlayToggle += OnPlayToggle;
    }

    private void UpdateSongSpeed(object value)
    {
        var speedValue = (float)Convert.ChangeType(value, typeof(float));
        songSpeed = speedValue / 10f;
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggle -= OnPlayToggle;
    }

    private float metronomeVolume;
    
    private void LateUpdate()
    {
        if (CowBell && !CowBellPlayed)
        {
            audioUtil.PlayOneShotSound(moreCowbellSound);
            CowBellPlayed = true;
        }
        else if (!CowBell)
        {
            CowBellPlayed = false;
        }
        metronomeVolume = Settings.Instance.MetronomeVolume;
        if (metronomeVolume != 0f && atsc.IsPlaying)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
            var toCheck = collection.FindLastBPM(atsc.CurrentBeat);
            if (lastBPMChange != toCheck)
            {
                lastBPMChange = toCheck;
                lastBPM = lastBPMChange?._BPM ?? atsc.song.beatsPerMinute;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume);
                RunAnimation();
                beatProgress = 0;
            }

            beatProgress += lastBPM / 60f * Time.deltaTime * songSpeed;
            if (!metronomeUI.activeInHierarchy) metronomeUI.SetActive(true);
            if (beatProgress >= 1)
            {
                beatProgress %= 1;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume);
                RunAnimation();
            }
        }
        else metronomeUI.SetActive(false);
    }

    private void RunAnimation()
    {
        if (!metronomeUIAnimator.gameObject.activeInHierarchy)
            return;

        metronomeUIAnimator.StopPlayback();
        metronomeUIAnimator.SetFloat(Bpm, Mathf.Abs(lastBPM * atsc.songAudioSource.pitch));
        metronomeUIAnimator.Play(metronomeUIDirection ? "Metronome_R2L" : "Metronome_L2R");
        metronomeUIDirection = !metronomeUIDirection;
    }

    void OnPlayToggle(bool playing)
    {
        if (metronomeVolume == 0) return;
        if (playing)
        {
            RunAnimation();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
            lastBPMChange = collection.FindLastBPM(atsc.CurrentBeat);
            lastBPM = lastBPMChange?._BPM ?? atsc.song.beatsPerMinute;
            if (lastBPMChange != null)
            {
                float differenceInSongBPM = atsc.CurrentBeat - lastBPMChange._time;
                float differenceInLastBPM = differenceInSongBPM * lastBPMChange._BPM / atsc.song.beatsPerMinute;
                beatProgress = differenceInLastBPM % 1;
            }
            else
            {
                beatProgress = atsc.CurrentBeat % 1;
            }
        }
    }
}
