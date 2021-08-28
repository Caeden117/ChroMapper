using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class ToggleColourDropdown : MonoBehaviour
{
    [FormerlySerializedAs("ColourDropdown")] [SerializeField] private RectTransform colourDropdown;
    public float YTop = 90;
    public float YBottom = -50;
    public bool Visible;

    public void ToggleDropdown(bool visible)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        Visible = visible;
        StartCoroutine(UpdateGroup(visible, colourDropdown));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        var dest = enabled ? YBottom : YTop;
        var og = group.anchoredPosition.y;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, Mathf.Lerp(og, dest, t));
            og = group.anchoredPosition.y;
            yield return new WaitForEndOfFrame();
        }

        group.anchoredPosition = new Vector2(group.anchoredPosition.x, dest);
        if (!enabled) group.gameObject.SetActive(false);
    }
}
