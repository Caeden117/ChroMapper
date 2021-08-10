using UnityEngine;
using UnityEngine.EventSystems;

public class SongTimelineHandleController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] private SongTimelineController timeline;
    [SerializeField] private AudioTimeSyncController atsc;
    private bool resume = false;

    private void OnEnable()
    {
        atsc.OnPlayToggle += OnPlayToggle;
    }

    public void OnPointerDown(PointerEventData fucku)
    {
        if (atsc.IsPlaying)
        {
            atsc.TogglePlaying();
            resume = true;
        }

        timeline.IsClicked = true;
        timeline.TriggerUpdate();
    }

    public void OnPointerUp(PointerEventData fucku)
    {
        timeline.TriggerUpdate();
        timeline.IsClicked = false;

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
