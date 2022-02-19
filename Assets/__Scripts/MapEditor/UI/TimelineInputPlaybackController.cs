using UnityEngine;

public class TimelineInputPlaybackController : MonoBehaviour
{
    [SerializeField] private AudioTimeSyncController atsc;
    private bool resume;

    private void OnEnable() => atsc.PlayToggle += OnPlayToggle;

    private void OnDestroy() => atsc.PlayToggle -= OnPlayToggle;

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
        if (resume && !atsc.IsPlaying) atsc.TogglePlaying();
        resume = false;
    }

    private void OnPlayToggle(bool playing) => resume = false;
}
