using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerMessageSender : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData data)
    {
        switch (data.button)
        {
            case PointerEventData.InputButton.Left:
                SendMessageUpwards("PresetSelect", GetComponent<Image>());
                break;
            case PointerEventData.InputButton.Middle:
                SendMessageUpwards("OverridePreset", GetComponent<Image>());
                break;
            case PointerEventData.InputButton.Right:
                SendMessageUpwards("DeletePreset", GetComponent<Image>());
                break;
        }
    }
}
