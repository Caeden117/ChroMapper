using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [SerializeField]
    public LocalizedString tooltip;

    [HideInInspector]
    public string tooltipOverride;

    [SerializeField]
    public string advancedTooltip;

    public bool TooltipActive = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (routine == null)
        {
            routine = StartCoroutine(TooltipRoutine(0));
        }

        TooltipActive = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        PersistentUI.Instance?.HideTooltip();
        TooltipActive = false;
    }

    void OnDisable() => OnPointerExit(null);

    private Coroutine routine;
    
    private IEnumerator TooltipRoutine(float timeToWait)
    {
        string tooltipTextResult = tooltipOverride;
        if (string.IsNullOrEmpty(tooltipOverride))
        {
            tooltipTextResult = tooltip.GetLocalizedString();
        }

        PersistentUI.Instance.SetTooltip(tooltipTextResult, advancedTooltip);
        yield return new WaitForSeconds(timeToWait);
        PersistentUI.Instance.ShowTooltip();
    }

}
