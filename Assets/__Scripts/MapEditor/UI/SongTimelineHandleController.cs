using UnityEngine;
using UnityEngine.EventSystems;

public class SongTimelineHandleController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] private SongTimelineController timeline;
    [SerializeField] private TimelineInputPlaybackController tipc;

    public void OnPointerDown(PointerEventData fucku)
    {
        tipc.PointerDown();
        timeline.IsClicked = true;
        timeline.TriggerUpdate();
    }

    public void OnPointerUp(PointerEventData fucku)
    {
        timeline.TriggerUpdate();
        timeline.IsClicked = false;
        tipc.PointerUp();
    }
}
