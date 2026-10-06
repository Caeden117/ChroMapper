using System;
using Assets.HSVPicker;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour
{
    [SerializeField] private Toggle placeChromaToggle;

    [Header("Setup")] public ColorPickerSetup Setup;

    [FormerlySerializedAs("onValueChanged")] [Header("Event")] public ColorChangedEvent ONValueChanged = new ColorChangedEvent();

    private float alpha = 1;
    private float blue;
    private float brightness;
    private float green;

    private float hue;

    private float red = 1;
    private float saturation;
    public HSVChangedEvent OnhsvChanged = new HSVChangedEvent();

    public Color CurrentColor
    {
        get => new Color(red, green, blue, alpha);
        set
        {
            // Approximate Color equality can discard small imported channel changes such as a tiny alpha.
            if (CurrentColor.Equals(value))
                return;

            red = value.r;
            green = value.g;
            blue = value.b;
            alpha = value.a;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float H
    {
        get => hue;
        set => AssignHsv(value, saturation, brightness, false);
    }

    public float S
    {
        get => saturation;
        set => AssignHsv(hue, value, brightness, false);
    }

    public float V
    {
        get => brightness;
        set => AssignHsv(hue, saturation, value, false);
    }

    public float R
    {
        get => red;
        set
        {
            if (red == value)
                return;

            red = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float G
    {
        get => green;
        set
        {
            if (green == value)
                return;

            green = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    public float B
    {
        get => blue;
        set
        {
            if (blue == value)
                return;

            blue = value;

            RGBChanged();

            SendChangedEvent();
        }
    }

    private float A
    {
        get => alpha;
        set
        {
            if (alpha == value)
                return;

            alpha = value;

            SendChangedEvent();
        }
    }

    private void Start()
    {
        Setup.AlphaSlidiers.Toggle(Setup.ShowAlpha);
        Setup.ColorToggleElement.Toggle(Setup.ShowColorSliderToggle);
        Setup.RgbSliders.Toggle(Setup.ShowRgb);
        Setup.HsvSliders.Toggle(Setup.ShowHsv);
        Setup.ColorBox.Toggle(Setup.ShowColorBox);

        HandleHeaderSetting(Setup.ShowHeader);
        UpdateColorToggleText();

        RGBChanged();
        SendChangedEvent();
    }

    // AI removed this??
    private void OnDestroy() => ColourHistory.Save();

    private void RGBChanged()
    {
        var color = HSVUtil.ConvertRgbToHsv(CurrentColor);

        hue = color.NormalizedH;
        saturation = color.NormalizedS;
        brightness = color.NormalizedV;
    }

    private void AssignHsv(float newHue, float newSaturation, float newBrightness, bool roundChannels)
    {
        if (hue == newHue && saturation == newSaturation && brightness == newBrightness)
            return;

        hue = newHue;
        saturation = newSaturation;
        brightness = newBrightness;
        var color = HSVUtil.ConvertHsvToRgb(hue * 360, saturation, brightness, alpha);

        // HSV sliders must round their generated RGB channels, while typed HSV values keep their precision.
        red = roundChannels
            ? RoundChannel(color.r)
            : color.r;
        green = roundChannels
            ? RoundChannel(color.g)
            : color.g;
        blue = roundChannels
            ? RoundChannel(color.b)
            : color.b;
        SendChangedEvent();
    }

    private static float RoundChannel(float value) => (float)Math.Round(value, 3);

    private void SendChangedEvent(bool updateChroma = true)
    {
        ONValueChanged.Invoke(CurrentColor);
        OnhsvChanged.Invoke(hue, saturation, brightness);
        //if (updateChroma)
        //    placeChromaToggle.isOn = true;
    }

    public void AssignColor(ColorValues type, float value) => AssignColor(type, value, false);

    public void AssignColor(ColorValues type, float value, bool roundChannels)
    {
        // Text and loaded colors bypass quantization. Only slider callbacks opt into three-decimal channels.
        if (roundChannels && type is ColorValues.R or ColorValues.G or ColorValues.B or ColorValues.A)
            value = RoundChannel(value);

        switch (type)
        {
            case ColorValues.R:
                R = value;
                break;
            case ColorValues.G:
                G = value;
                break;
            case ColorValues.B:
                B = value;
                break;
            case ColorValues.A:
                A = value;
                break;
            case ColorValues.Hue:
                AssignHsv(value, saturation, brightness, roundChannels);
                break;
            case ColorValues.Saturation:
                AssignHsv(hue, value, brightness, roundChannels);
                break;
            case ColorValues.Value:
                AssignHsv(hue, saturation, value, roundChannels);
                break;
        }
    }

    public float GetValue(ColorValues type)
    {
        return type switch
        {
            ColorValues.R => R,
            ColorValues.G => G,
            ColorValues.B => B,
            ColorValues.A => A,
            ColorValues.Hue => H,
            ColorValues.Saturation => S,
            ColorValues.Value => V,
            _ => throw new NotImplementedException(""),
        };
    }

    public void ToggleColorSliders()
    {
        Setup.ShowHsv = !Setup.ShowHsv;
        Setup.ShowRgb = !Setup.ShowRgb;
        Setup.HsvSliders.Toggle(Setup.ShowHsv);
        Setup.RgbSliders.Toggle(Setup.ShowRgb);


        UpdateColorToggleText();
    }

    private void UpdateColorToggleText()
    {
        if (Setup.ShowRgb && Setup.SliderToggleButtonText) Setup.SliderToggleButtonText.text = "RGB";

        if (Setup.ShowHsv && Setup.SliderToggleButtonText) Setup.SliderToggleButtonText.text = "HSV";
    }

    private void HandleHeaderSetting(ColorPickerSetup.ColorHeaderShowing setupShowHeader)
    {
        if (setupShowHeader == ColorPickerSetup.ColorHeaderShowing.Hide)
        {
            Setup.ColorHeader.Toggle(false);
            return;
        }

        Setup.ColorHeader.Toggle(true);

        Setup.ColorPreview.Toggle(setupShowHeader != ColorPickerSetup.ColorHeaderShowing.ShowColorCode);
        Setup.ColorCode.Toggle(setupShowHeader != ColorPickerSetup.ColorHeaderShowing.ShowColor);
    }
}
