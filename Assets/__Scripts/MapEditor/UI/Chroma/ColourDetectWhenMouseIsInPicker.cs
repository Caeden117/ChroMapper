using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ColourDetectWhenMouseIsInPicker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public void OnPointerEnter(PointerEventData e)
    {
        ColourSelector.IsHovering = true;
    }

    public void OnPointerExit(PointerEventData e)
    {
        ColourSelector.IsHovering = false;
    }
}
