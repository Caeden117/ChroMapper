using System;
using System.Collections.Generic;
using System.Linq;
using Assets.HSVPicker;
using UnityEngine;
using UnityEngine.UI;

public class ColorPresets : MonoBehaviour
{
	public ColorPicker picker;
	public GameObject[] presets;
	public Image createPresetImage;

    private ColorPresetList _colors;

	void Awake()
	{
//		picker.onHSVChanged.AddListener(HSVChanged);
		picker.onValueChanged.AddListener(ColorChanged);
	}

    void Start()
    {
        _colors = ColorPresetManager.Get(picker.Setup.PresetColorsId);

        if (_colors.Colors.Count < picker.Setup.DefaultPresetColors.Length)
        {
            _colors.UpdateList(picker.Setup.DefaultPresetColors);
        }

        _colors.OnColorsUpdated += OnColorsUpdate;
        OnColorsUpdate(_colors.Colors);
    }

    private void OnColorsUpdate(List<Color> colors)
    {
        for (int cnt = 0; cnt < presets.Length; cnt++)
        {
            if (colors.Count <= cnt)
            {
                presets[cnt].SetActive(false);
                continue;
            }

            presets[cnt].SetActive(true);
            presets[cnt].GetComponent<Image>().color = colors[cnt];
            
        }
        createPresetImage.gameObject.SetActive(colors.Count < presets.Length);
    }

    public void CreatePresetButton()
	{
        _colors.AddColor(picker.CurrentColor);
	}

	public void PresetSelect(GameObject sender)
	{
		picker.CurrentColor = sender.GetComponent<Image>().color;
	}

    public void DeletePreset(GameObject sender)
    {
        _colors.Colors.RemoveAt(Array.IndexOf(presets, sender));
        OnColorsUpdate(_colors.Colors);
    }

    public void OverridePreset(GameObject sender)
    {
        _colors.Colors[Array.IndexOf(presets, sender)] = picker.CurrentColor;
        //_colors.Colors.set(sender.color);
        OnColorsUpdate(_colors.Colors);
    }

    private void OnDestroy()
    {
        //Whoever made this HSV Picker is a dumbass and forgot to unsubscribe from events when the object is destroyed
        _colors.OnColorsUpdated -= OnColorsUpdate;
    }

    private void ColorChanged(Color color)
	{
		createPresetImage.color = color;
	}
}
