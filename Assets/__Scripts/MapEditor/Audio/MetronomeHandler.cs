using UnityEngine;

public class MetronomeHandler : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    private int lastWholeBeat = -1;

    private void LateUpdate()
    {
        metronomeUI.SetActive(Settings.Instance.MetronomeVolume != 0f);
        //Another way to go about this without dealing with box colliders and the measure lines
        int flooredBeat = Mathf.FloorToInt(atsc.CurrentBeat);
        if (flooredBeat != lastWholeBeat)
        {
            if (atsc.IsPlaying) audioUtil.PlayOneShotSound(metronomeSound, Settings.Instance.MetronomeVolume);
            lastWholeBeat = flooredBeat;
        }
    }
}
