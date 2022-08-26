using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// basically similar to <see cref="StrobeGeneratorUIDropdown"/>
/// </summary>
public class LightV3GeneratorAppearance : MonoBehaviour
{
    [SerializeField] private RectTransform lightV3GenUIRect;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject rotationPanel;

    public bool IsActive { get; private set; }

    public void ToggleDropdown(bool visible) => StartCoroutine(UpdateGroup(visible, lightV3GenUIRect));

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        IsActive = enabled;
        float dest = enabled ? -150 : 120;
        var og = group.anchoredPosition.x;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(Mathf.Lerp(og, dest, t), group.anchoredPosition.y);
            og = group.anchoredPosition.x;
            yield return new WaitForEndOfFrame();
        }

        group.anchoredPosition = new Vector2(dest, group.anchoredPosition.y);
    }

    public void OnToggleColorRotationSwitch()
    {
        if (colorPanel.activeSelf)
        {
            colorPanel.SetActive(false);
            rotationPanel.SetActive(true);
        }
        else
        {
            colorPanel.SetActive(true);
            rotationPanel.SetActive(false);
        }
    }
}
