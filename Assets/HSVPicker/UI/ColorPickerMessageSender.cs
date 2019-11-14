using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPickerMessageSender : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData data)
    {
        if (data.button == PointerEventData.InputButton.Left)
            SendMessageUpwards(KeybindsController.ShiftHeld ? "DeletePreset" : "PresetSelect", GetComponent<Image>());
        else if (data.button == PointerEventData.InputButton.Middle)
            SendMessageUpwards("OverridePreset", GetComponent<Image>());
    }
}
