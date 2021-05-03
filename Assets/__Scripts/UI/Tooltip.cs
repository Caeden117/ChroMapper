using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    [SerializeField]
    public LocalizedString tooltip;

    [HideInInspector]
    public string tooltipOverride;

    [SerializeField]
    public string advancedTooltip;

    public void OnPointerEnter(PointerEventData eventData) {
        if (routine == null) {
            routine = StartCoroutine(TooltipRoutine(0));
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (routine != null) {
            StopCoroutine(routine);
            routine = null;
        }
        PersistentUI.Instance?.HideTooltip();
    }

    void OnDisable() {
        if (routine != null) {
            StopCoroutine(routine);
            routine = null;
        }
        PersistentUI.Instance?.HideTooltip();
    }

    Coroutine routine;
    IEnumerator TooltipRoutine(float timeToWait)
    {
        string tooltipTextResult = tooltipOverride;
        if (string.IsNullOrEmpty(tooltipOverride))
        {
            var tooltipText = tooltip.GetLocalizedString();
            yield return tooltipText;
            tooltipTextResult = tooltipText.Result;
        }

        PersistentUI.Instance.SetTooltip(tooltipTextResult, advancedTooltip);
        yield return new WaitForSeconds(timeToWait);
        PersistentUI.Instance.ShowTooltip();
    }

}
