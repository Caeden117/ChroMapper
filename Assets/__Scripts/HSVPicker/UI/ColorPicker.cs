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
            if (CurrentColor == value)
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
        set
        {
            if (hue == value)
                return;

            hue = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float S
    {
        get => saturation;
        set
        {
            if (saturation == value)
                return;

            saturation = value;

            HSVChanged();

            SendChangedEvent();
        }
    }

    public float V
    {
        get => brightness;
        set
        {
            if (brightness == value)
                return;

            brightness = value;

            HSVChanged();

            SendChangedEvent();
        }
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

    private void OnDestroy() => ColourHistory.Save();

    private void RGBChanged()
    {
        var color = HSVUtil.ConvertRgbToHsv(CurrentColor);

        hue = color.NormalizedH;
        saturation = color.NormalizedS;
        brightness = color.NormalizedV;
    }

    private void HSVChanged()
    {
        var color = HSVUtil.ConvertHsvToRgb(hue * 360, saturation, brightness, alpha);

        red = color.r;
        green = color.g;
        blue = color.b;
    }

    private void SendChangedEvent(bool updateChroma = true)
    {
        ONValueChanged.Invoke(CurrentColor);
        OnhsvChanged.Invoke(hue, saturation, brightness);
        //if (updateChroma)
        //    placeChromaToggle.isOn = true;
    }

    public void AssignColor(ColorValues type, float value)
    {
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
                H = value;
                break;
            case ColorValues.Saturation:
                S = value;
                break;
            case ColorValues.Value:
                V = value;
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
        if (Setup.ShowRgb) Setup.SliderToggleButtonText.text = "RGB";

        if (Setup.ShowHsv) Setup.SliderToggleButtonText.text = "HSV";
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
