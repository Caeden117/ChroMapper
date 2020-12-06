using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RightButtonPanel : MonoBehaviour
{
    public bool IsActive { get; private set; } = false;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = transform as RectTransform;
        rectTransform.anchoredPosition = new Vector2(rectTransform.sizeDelta.x, rectTransform.anchoredPosition.y);
    }

    public void TogglePanel()
    {
        IsActive = !IsActive;
        StopAllCoroutines();
        StartCoroutine(Slide(IsActive ? 0 : rectTransform.sizeDelta.x));
    }

    private IEnumerator Slide(float dest)
    {
        float t = 0;
        float og = rectTransform.anchoredPosition.x;
        while (t < 1)
        {
            t += Time.deltaTime;
            rectTransform.anchoredPosition = new Vector2(Mathf.Lerp(og, dest, t), rectTransform.anchoredPosition.y);
            og = rectTransform.anchoredPosition.x;
            yield return new WaitForEndOfFrame();
        }
        rectTransform.anchoredPosition = new Vector2(dest, rectTransform.anchoredPosition.y);
    }
}
