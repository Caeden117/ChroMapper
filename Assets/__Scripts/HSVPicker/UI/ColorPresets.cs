using System;
using System.Collections.Generic;
using Assets.HSVPicker;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ColorPresets : MonoBehaviour
{
    [FormerlySerializedAs("picker")] public ColorPicker Picker;
    [FormerlySerializedAs("presets")] public GameObject[] Presets;
    [FormerlySerializedAs("createPresetImage")] public Image CreatePresetImage;

    private ColorPresetList colors;

    private void Awake() =>
        //		picker.onHSVChanged.AddListener(HSVChanged);
        Picker.ONValueChanged.AddListener(ColorChanged);

    private void Start()
    {
        colors = ColorPresetManager.Get(Picker.Setup.PresetColorsId);

        if (colors.Colors.Count < Picker.Setup.DefaultPresetColors.Length)
            colors.UpdateList(Picker.Setup.DefaultPresetColors);

        colors.ColorsUpdated += OnColorsUpdate;
        OnColorsUpdate(colors.Colors);
    }

    private void OnDestroy() =>
        //Whoever made this HSV Picker is a dumbass and forgot to unsubscribe from events when the object is destroyed
        colors.ColorsUpdated -= OnColorsUpdate;

    private void OnColorsUpdate(List<Color> colors)
    {
        for (var cnt = 0; cnt < Presets.Length; cnt++)
        {
            if (colors.Count <= cnt)
            {
                Presets[cnt].SetActive(false);
                continue;
            }

            Presets[cnt].SetActive(true);
            Presets[cnt].GetComponent<Image>().color = colors[cnt];
        }

        CreatePresetImage.gameObject.SetActive(colors.Count < Presets.Length);
    }

    public void CreatePresetButton() => colors.AddColor(Picker.CurrentColor);

    public void PresetSelect(GameObject sender) => Picker.CurrentColor = sender.GetComponent<Image>().color;

    public void DeletePreset(GameObject sender)
    {
        colors.Colors.RemoveAt(Array.IndexOf(Presets, sender));
        OnColorsUpdate(colors.Colors);
    }

    public void OverridePreset(GameObject sender)
    {
        colors.Colors[Array.IndexOf(Presets, sender)] = Picker.CurrentColor;
        //_colors.Colors.set(sender.color);
        OnColorsUpdate(colors.Colors);
    }

    private void ColorChanged(Color color) => CreatePresetImage.color = color;
}
