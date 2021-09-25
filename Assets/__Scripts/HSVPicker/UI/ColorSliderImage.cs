using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
[ExecuteInEditMode]
public class ColorSliderImage : MonoBehaviour
{
    [FormerlySerializedAs("picker")] public ColorPicker Picker;

    /// <summary>
    ///     Which value this slider can edit.
    /// </summary>
    [FormerlySerializedAs("type")] public ColorValues Type;

    [FormerlySerializedAs("direction")] public Slider.Direction Direction;

    private RawImage image;

    private void Awake()
    {
        image = GetComponent<RawImage>();

        if (Application.isPlaying)
            RegenerateTexture();
    }

    private void OnEnable()
    {
        if (Picker != null && Application.isPlaying)
        {
            Picker.ONValueChanged.AddListener(ColorChanged);
            Picker.OnhsvChanged.AddListener(HSVChanged);
        }
    }

    private void OnDisable()
    {
        if (Picker != null)
        {
            Picker.ONValueChanged.RemoveListener(ColorChanged);
            Picker.OnhsvChanged.RemoveListener(HSVChanged);
        }
    }

    private void OnDestroy()
    {
        if (image.texture != null)
            DestroyImmediate(image.texture);
    }

    private void ColorChanged(Color newColor)
    {
        switch (Type)
        {
            case ColorValues.R:
            case ColorValues.G:
            case ColorValues.B:
            case ColorValues.Saturation:
            case ColorValues.Value:
                RegenerateTexture();
                break;
            case ColorValues.A:
            case ColorValues.Hue:
            default:
                break;
        }
    }

    private void HSVChanged(float hue, float saturation, float value)
    {
        switch (Type)
        {
            case ColorValues.R:
            case ColorValues.G:
            case ColorValues.B:
            case ColorValues.Saturation:
            case ColorValues.Value:
                RegenerateTexture();
                break;
            case ColorValues.A:
            case ColorValues.Hue:
            default:
                break;
        }
    }

    private void RegenerateTexture()
    {
        Color32 baseColor = Picker != null ? Picker.CurrentColor : Color.black;

        var h = Picker != null ? Picker.H : 0;
        var s = Picker != null ? Picker.S : 0;
        var v = Picker != null ? Picker.V : 0;

        Texture2D texture;
        Color32[] colors;

        var vertical = Direction == Slider.Direction.BottomToTop || Direction == Slider.Direction.TopToBottom;
        var inverted = Direction == Slider.Direction.TopToBottom || Direction == Slider.Direction.RightToLeft;

        int size;
        switch (Type)
        {
            case ColorValues.R:
            case ColorValues.G:
            case ColorValues.B:
            case ColorValues.A:
                size = 255;
                break;
            case ColorValues.Hue:
                size = 360;
                break;
            case ColorValues.Saturation:
            case ColorValues.Value:
                size = 100;
                break;
            default:
                throw new NotImplementedException("");
        }

        if (vertical)
            texture = new Texture2D(1, size);
        else
            texture = new Texture2D(size, 1);

        texture.hideFlags = HideFlags.DontSave;
        colors = new Color32[size];

        switch (Type)
        {
            case ColorValues.R:
                for (byte i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = new Color32(i, baseColor.g, baseColor.b, 255);
                break;
            case ColorValues.G:
                for (byte i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = new Color32(baseColor.r, i, baseColor.b, 255);
                break;
            case ColorValues.B:
                for (byte i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = new Color32(baseColor.r, baseColor.g, i, 255);
                break;
            case ColorValues.A:
                for (byte i = 0; i < size; i++) colors[inverted ? size - 1 - i : i] = new Color32(i, i, i, 255);
                break;
            case ColorValues.Hue:
                for (var i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = HSVUtil.ConvertHsvToRgb(i, 1, 1, 1);
                break;
            case ColorValues.Saturation:
                for (var i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = HSVUtil.ConvertHsvToRgb(h * 360, (float)i / size, v, 1);
                break;
            case ColorValues.Value:
                for (var i = 0; i < size; i++)
                    colors[inverted ? size - 1 - i : i] = HSVUtil.ConvertHsvToRgb(h * 360, s, (float)i / size, 1);
                break;
            default:
                throw new NotImplementedException("");
        }

        texture.SetPixels32(colors);
        texture.Apply();

        if (image.texture != null)
            DestroyImmediate(image.texture);
        image.texture = texture;

        switch (Direction)
        {
            case Slider.Direction.BottomToTop:
            case Slider.Direction.TopToBottom:
                image.uvRect = new Rect(0, 0, 2, 1);
                break;
            case Slider.Direction.LeftToRight:
            case Slider.Direction.RightToLeft:
                image.uvRect = new Rect(0, 0, 1, 2);
                break;
        }
    }
}
