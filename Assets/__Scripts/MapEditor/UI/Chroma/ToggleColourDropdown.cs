using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleColourDropdown : MonoBehaviour {
    [SerializeField] private RectTransform ColourDropdown;
    private float YDestination = 75f;
	
	public void ToggleDropdown(bool visible)
    {
        YDestination = 75f * (visible ? -1 : 1);
        if (!visible) EventPreview.QueuedChromaColor = -1;
    }

	void Update () {
        ColourDropdown.anchoredPosition3D = Vector3.Slerp(ColourDropdown.anchoredPosition3D, new Vector3(-250, YDestination, 0), 2 * Time.deltaTime);
	}
}
