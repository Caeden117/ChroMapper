using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleColourDropdown : MonoBehaviour {
    [SerializeField] private RectTransform ColourDropdown;
    public float YDestination { get; private set; } = 50f;
	
	public void ToggleDropdown(bool visible)
    {
        YDestination =(visible ? -40 : 50);
        if (!visible) EventPreview.QueuedChromaColor = -1;
    }

	void Update () {
        ColourDropdown.anchoredPosition3D = Vector3.Slerp(ColourDropdown.anchoredPosition3D, new Vector3(-100, YDestination, 0), 2 * Time.deltaTime);
	}
}
