using UnityEngine;
using UnityEngine.EventSystems;

public class MessageOnClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool bigMessage;

    [SerializeField] [TextArea(3, 10)] private string message;

    public void OnPointerClick(PointerEventData eventData) => PersistentUI.Instance.DisplayMessage(message,
        bigMessage ? PersistentUI.DisplayMessageType.Center : PersistentUI.DisplayMessageType.Bottom);
}
