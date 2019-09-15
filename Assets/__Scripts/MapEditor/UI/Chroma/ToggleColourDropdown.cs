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
        Visible = visible;
    }

	void Update () {
        ColourDropdown.anchoredPosition = Vector2.Lerp(ColourDropdown.anchoredPosition, new Vector2(-100, Visible ? YBottom : YTop), 2 * Time.deltaTime);
	}
}
