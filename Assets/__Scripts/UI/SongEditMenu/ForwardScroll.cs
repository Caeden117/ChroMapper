using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ForwardScroll : MonoBehaviour, IScrollHandler
{
    public void OnScroll(PointerEventData eventData)
    {
        var scrollRect = transform.GetComponentInParent<ScrollRect>();
        scrollRect.OnScroll(eventData);
    }
}
