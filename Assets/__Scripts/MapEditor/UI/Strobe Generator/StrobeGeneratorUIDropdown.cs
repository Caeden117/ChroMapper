using System.Collections;
using UnityEngine;

public class StrobeGeneratorUIDropdown : MonoBehaviour
{
    [SerializeField] private RectTransform strobeGenUIRect;

    public static bool IsActive { get; private set; } = false;

    public void ToggleDropdown(bool visible)
    {
        StartCoroutine(UpdateGroup(visible, strobeGenUIRect));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        IsActive = enabled;
        float dest = enabled ? -150 : 90;
        float og = group.anchoredPosition.y;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, Mathf.Lerp(og, dest, t));
            og = group.anchoredPosition.y;
            yield return new WaitForEndOfFrame();
        }
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, dest);
    }
}
