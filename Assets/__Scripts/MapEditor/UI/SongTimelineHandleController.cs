using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SongTimelineHandleController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] private SongTimelineController timeline;

    public void OnPointerDown(PointerEventData fucku)
    {
        timeline.IsClicked = true;
    }

    public void OnPointerUp(PointerEventData fucku)
    {
        timeline.IsClicked = false;
    }
}
