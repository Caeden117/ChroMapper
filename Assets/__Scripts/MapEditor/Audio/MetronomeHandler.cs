using UnityEngine;

public class MetronomeHandler : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private AudioClip metronomeSound;
    [SerializeField] private AudioUtil audioUtil;
    [SerializeField] private GameObject metronomeUI;
    private int lastWholeBeat = -1;
    private RectTransform metronomeUITransform;

    private void Start()
    {
        metronomeUITransform = metronomeUI.GetComponent<RectTransform>();
    }
    
    private void LateUpdate()
    {
        metronomeUI.SetActive(Settings.Instance.MetronomeVolume != 0f);
        
        int flooredBeat = Mathf.FloorToInt(atsc.CurrentBeat);
        if (flooredBeat != lastWholeBeat)
        {
            if (atsc.IsPlaying) audioUtil.PlayOneShotSound(metronomeSound, Settings.Instance.MetronomeVolume);
            lastWholeBeat = flooredBeat;
            
            Vector3 vec = metronomeUITransform.localScale;
            vec.x = -vec.x; //This is for flipping the img. This is temp, I will convert this to an animation later.
            metronomeUITransform.localScale = vec;
        }
    }
}
