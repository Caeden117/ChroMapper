using System;
using UnityEngine;
using UnityEngine.UI;

public class MetronomeHandler : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioClip moreCowbellSound;
    [SerializeField] private AudioClip cowbellSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    private int lastWholeBeat = -1;
    private Animator metronomeUIAnimator;
    private static readonly int Bpm = Animator.StringToHash("BPM");
    private bool metronomeUIDirection = true;
    public bool CowBell;
    private bool CowBellPlayed;

    private void Start()
    {
        metronomeUIAnimator = metronomeUI.GetComponent<Animator>();
        atsc.OnPlayToggle += OnPlayToggle;
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
        if (metronomeVolume != 0f)
        {
            metronomeUI.SetActive(true);
            int flooredBeat = Mathf.FloorToInt(atsc.CurrentBeat);
            if (flooredBeat != lastWholeBeat)
            {
                if (atsc.IsPlaying)
                {
                    audioUtil.PlayOneShotSound(CowBell ? cowbellSound : metronomeSound, Settings.Instance.MetronomeVolume);
                    RunAnimation();
                }
                lastWholeBeat = flooredBeat;
            }
        }
        else metronomeUI.SetActive(false);
    }

    private void RunAnimation()
    {
        metronomeUIAnimator.SetFloat(Bpm, Mathf.Abs(atsc.song.beatsPerMinute*atsc.songAudioSource.pitch));
        metronomeUIAnimator.Play(metronomeUIDirection ? "Metronome_R2L" : "Metronome_L2R");
        metronomeUIDirection = !metronomeUIDirection;
    }

    void OnPlayToggle(bool playing)
    {
        if (metronomeVolume == 0) return;
        if(playing) RunAnimation();
    }
    
}
