using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BetterSliderRingHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private readonly Vector3 ringHidden = new Vector3(0, 0, 1);
    private readonly Vector3 ringVisible = new Vector3(2.5f, 2.5f, 1);

    private Coroutine onHandleCoroutine;
    private Image ringImage;

    private RectTransform ringTransform;

    private void Start()
    {
        ringImage = GetComponentsInChildren<Image>().First(i => i.name == "Ring");
        ringTransform = ringImage.rectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData) => onHandleCoroutine = StartCoroutine(ScaleRing(true));

    public void OnPointerExit(PointerEventData eventData) => onHandleCoroutine = StartCoroutine(ScaleRing(false));

    private IEnumerator ScaleRing(bool grow)
    {
        if (onHandleCoroutine != null) StopCoroutine(onHandleCoroutine);

        var startTime = Time.time;

        while (true)
        {
            var ringSize = ringTransform.localScale;

            if (grow)
            {
                try { PersistentUI.Instance.HideTooltip(); }
                catch (NullReferenceException) { }
            }
            else if (ringSize.x == ringVisible.x)
            {
                try { PersistentUI.Instance.ShowTooltip(); }
                catch (NullReferenceException) { }
            }

            var toBe = grow ? ringVisible : ringHidden;

            ringSize = Vector3.MoveTowards(ringSize, toBe, Time.time / startTime * 0.1f);
            ringTransform.localScale = ringSize;

            if (ringSize.x == toBe.x) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
