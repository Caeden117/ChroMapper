using System;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientFromColorSchemeColors : MonoBehaviour
{
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;

    [SerializeField] public BloomPrePassBackgroundColorsGradient BloomPrePassBackgroundColorsGradient;
    [SerializeField] public Element[] Elements;

    protected void Start()
    {
        if (ColorSchemeProvider != null)
            ColorSchemeProvider.OnColorSchemeChanged += HandleColorSchemeChanged;

        HandleColorSchemeChanged();
    }

    protected void OnDestroy()
    {
        if (ColorSchemeProvider != null)
            ColorSchemeProvider.OnColorSchemeChanged -= HandleColorSchemeChanged;
    }

    private void HandleColorSchemeChanged() => SetColorsToElements();

    private void SetColorsToElements()
    {
        if (ColorSchemeProvider == null || ColorSchemeProvider.ColorScheme == null) return;
        if (BloomPrePassBackgroundColorsGradient == null || Elements == null) return;

        var gradientElements = BloomPrePassBackgroundColorsGradient.Elements;
        if (gradientElements == null) return;

        for (var i = 0; i < gradientElements.Length && i < Elements.Length; i++)
        {
            if (Elements[i].LoadFromColorScheme)
            {
                Elements[i].Color = Elements[i].EnvironmentColor switch
                {
                    EnvironmentColor.Color0 => ColorSchemeProvider.ColorScheme.EnvironmentLeftColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color1 => ColorSchemeProvider.ColorScheme.EnvironmentRightColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color0Boost => ColorSchemeProvider.ColorScheme.EnvironmentLeftBoostColor
                        * Elements[i].Intensity,
                    EnvironmentColor.Color1Boost => ColorSchemeProvider.ColorScheme.EnvironmentRightBoostColor
                        * Elements[i].Intensity,
                    _ => Elements[i].Color
                };
            }

            BloomPrePassBackgroundColorsGradient.Elements[i].Color = Elements[i].Color;
        }

        BloomPrePassBackgroundColorsGradient.UpdateGradientTexture();
    }

    [Serializable]
    public class Element
    {
        public bool LoadFromColorScheme;
        public EnvironmentColor EnvironmentColor;
        public float Intensity;
        public Color Color;
    }

    public enum EnvironmentColor
    {
        Color0,
        Color1,
        Color0Boost,
        Color1Boost
    }
}
