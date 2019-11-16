using System;
using System.Collections.Generic;
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
            Debug.Log(cnt);
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

	public void PresetSelect(Image sender)
	{
		picker.CurrentColor = sender.color;
	}

    public void DeletePreset(Image sender)
    {
        _colors.Colors.Remove(sender.color);
        OnColorsUpdate(_colors.Colors);
    }

    public void OverridePreset(Image sender)
    {
        _colors.Colors[_colors.Colors.IndexOf(sender.color)] = picker.CurrentColor;
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
