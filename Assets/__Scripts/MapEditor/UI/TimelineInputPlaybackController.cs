using UnityEngine;

public class TimelineInputPlaybackController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    private bool resume = false;

    private void OnEnable()
    {
        atsc.OnPlayToggle += OnPlayToggle;
    }

    public void PointerDown()
    {
        if (atsc.IsPlaying)
        {
            atsc.TogglePlaying();
            resume = true;
        }
    }

    public void PointerUp()
    {
        if (resume && !atsc.IsPlaying)
        {
            atsc.TogglePlaying();
        }
        resume = false;
    }

    private void OnPlayToggle(bool playing)
    {
        resume = false;
    }

    private void OnDestroy()
    {
        atsc.OnPlayToggle -= OnPlayToggle;
    }
}
