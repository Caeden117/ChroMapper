using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    [SerializeField] [TextArea(3, 10)]
    public string tooltip;

    [SerializeField] [TextArea(3, 10)]
    public string advancedTooltip;

    public void OnPointerEnter(PointerEventData eventData) {
        if (routine == null) {
            routine = StartCoroutine(TooltipRoutine());
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (routine != null) {
            StopCoroutine(routine);
            routine = null;
        }
        PersistentUI.Instance.HideTooltip();
    }

    void OnDisable() {
        if (routine != null) {
            StopCoroutine(routine);
            routine = null;
        }
        PersistentUI.Instance.HideTooltip();
    }

    Coroutine routine;
    IEnumerator TooltipRoutine() {
        PersistentUI.Instance.SetTooltip(tooltip, advancedTooltip);
        yield return new WaitForSeconds(2f);
        PersistentUI.Instance.ShowTooltip();
    }

}
