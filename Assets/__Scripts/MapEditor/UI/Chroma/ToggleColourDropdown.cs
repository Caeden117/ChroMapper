using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleColourDropdown : MonoBehaviour {
    [SerializeField] private RectTransform ColourDropdown;
    public float YTop = 90;
    public float YBottom = -50;
    public bool Visible = false;
	
	public void ToggleDropdown(bool visible)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        Visible = visible;
        StartCoroutine(UpdateGroup(visible, ColourDropdown));
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        float dest = enabled ? YBottom : YTop;
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
        if (!enabled) group.gameObject.SetActive(false);
    }
}
