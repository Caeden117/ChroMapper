using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using UnityEngine;

public class MetronomeHandler : MonoBehaviour
{
    private static readonly int bpm = Animator.StringToHash("BPM");
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioClip moreCowbellSound;
    [SerializeField] private AudioClip cowbellSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    public bool CowBell;
    private float beatProgress;
    private bool cowBellPlayed;
    private float lastBpm = 100;
    private BaseBpmEvent lastBpmChange;
    private Animator metronomeUIAnimator;
    private bool metronomeUIDirection = true;

    private float metronomeVolume;

    private float songSpeed = 1;

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        Settings.NotifyBySettingName("SongSpeed", UpdateSongSpeed);

        lastBpm = atsc.Song.BeatsPerMinute;
        atsc.PlayToggle += OnPlayToggle;
    }

    private void LateUpdate()
    {
        if (CowBell && !cowBellPlayed)
        {
            audioUtil.PlayOneShotSound(moreCowbellSound);
            cowBellPlayed = true;
        }
        else if (!CowBell)
        {
            cowBellPlayed = false;
        }

        metronomeVolume = Settings.Instance.MetronomeVolume;
        if (metronomeVolume != 0f && atsc.IsPlaying && !atsc.StopScheduled)
        {
            var collection =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                    ObjectType.BpmChange);
            var toCheck = collection.FindLastBpm(atsc.CurrentAudioBeats);
            if (lastBpmChange != toCheck)
            {
                lastBpmChange = toCheck;
                lastBpm = lastBpmChange?.Bpm ?? atsc.Song.BeatsPerMinute;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume);
                RunAnimation();
                beatProgress = 0;
            }

            beatProgress += lastBpm / 60f * Time.deltaTime * songSpeed;
            if (!metronomeUI.activeInHierarchy) metronomeUI.SetActive(true);
            if (beatProgress >= 1)
            {
                beatProgress %= 1;
                audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume);
                RunAnimation();
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

    private void RunAnimation()
    {
        if (!metronomeUIAnimator.gameObject.activeInHierarchy)
            return;

        metronomeUIAnimator.StopPlayback();
        metronomeUIAnimator.SetFloat(bpm, Mathf.Abs(lastBpm * atsc.SongAudioSource.pitch));
        metronomeUIAnimator.Play(metronomeUIDirection ? "Metronome_R2L" : "Metronome_L2R");
        metronomeUIDirection = !metronomeUIDirection;
    }

    private void OnPlayToggle(bool playing)
    {
        if (metronomeVolume == 0) return;
        if (playing)
        {
            RunAnimation();
            var collection =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                    ObjectType.BpmChange);
            lastBpmChange = collection.FindLastBpm(atsc.CurrentAudioBeats);
            lastBpm = lastBpmChange?.Bpm ?? atsc.Song.BeatsPerMinute;
            if (lastBpmChange != null)
            {
                var differenceInSongBpm = atsc.CurrentAudioBeats - lastBpmChange.JsonTime;
                var differenceInLastBpm = differenceInSongBpm * lastBpmChange.Bpm / atsc.Song.BeatsPerMinute;
                beatProgress = differenceInLastBpm % 1;
            }
            else
            {
                beatProgress = atsc.CurrentAudioBeats % 1;
            }
        }
    }
}
