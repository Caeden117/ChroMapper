using UnityEngine;
using UnityEngine.EventSystems;

public class ColorPickerMessageSender : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData data)
    {
        switch (data.button)
        {
            case PointerEventData.InputButton.Left:
                SendMessageUpwards("PresetSelect", gameObject);
                break;
            case PointerEventData.InputButton.Middle:
                SendMessageUpwards("OverridePreset", gameObject);
                break;
            case PointerEventData.InputButton.Right:
                SendMessageUpwards("DeletePreset", gameObject);
                break;
        }
    }
}
