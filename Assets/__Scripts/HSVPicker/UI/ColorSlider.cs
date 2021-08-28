using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
///     Displays one of the color values of aColorPicker
/// </summary>
[RequireComponent(typeof(Slider))]
public class ColorSlider : MonoBehaviour
{
    [FormerlySerializedAs("hsvpicker")] public ColorPicker Hsvpicker;

    /// <summary>
    ///     Which value this slider can edit.
    /// </summary>
    [FormerlySerializedAs("type")] public ColorValues Type;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();

        Hsvpicker.ONValueChanged.AddListener(ColorChanged);
        Hsvpicker.OnhsvChanged.AddListener(HSVChanged);
        slider.onValueChanged.AddListener(SliderChanged);
    }

    private void OnDestroy()
    {
        Hsvpicker.ONValueChanged.RemoveListener(ColorChanged);
        Hsvpicker.OnhsvChanged.RemoveListener(HSVChanged);
        slider.onValueChanged.RemoveListener(SliderChanged);
    }

    private void ColorChanged(Color newColor)
    {
        switch (Type)
        {
            case ColorValues.R:
                slider.SetValueWithoutNotify(newColor.r);
                break;
            case ColorValues.G:
                slider.SetValueWithoutNotify(newColor.g);
                break;
            case ColorValues.B:
                slider.SetValueWithoutNotify(newColor.b);
                break;
            case ColorValues.A:
                slider.SetValueWithoutNotify(newColor.a);
                break;
        }
    }

    private void HSVChanged(float hue, float saturation, float value)
    {
        switch (Type)
        {
            case ColorValues.Hue:
                slider.SetValueWithoutNotify(hue);
                break;
            case ColorValues.Saturation:
                slider.SetValueWithoutNotify(saturation);
                break;
            case ColorValues.Value:
                slider.SetValueWithoutNotify(value);
                break;
        }
    }

    private void SliderChanged(float newValue) => Hsvpicker.AssignColor(Type, newValue);
}
