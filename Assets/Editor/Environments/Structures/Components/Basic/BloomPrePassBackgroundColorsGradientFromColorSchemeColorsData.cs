using System.Linq;
using UnityEngine;

public class
    BloomPrePassBackgroundColorsGradientFromColorSchemeColorsData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientFromColorSchemeColors>
{
    public int BloomPrePassBackgroundColorsGradient;
    public ElementComponent[] Elements;

    public class ElementComponent
    {
        public bool LoadFromColorScheme;
        public int EnvironmentColor;
        public float Intensity;
        public Color Color;
    }

    public override void FillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientFromColorSchemeColors comp,
        CreateContainer container)
    {
        comp.ColorSchemeProvider = container.Descriptor.ColorSchemeProvider;

        comp.BloomPrePassBackgroundColorsGradient =
            container.GetComponentOrNull<BloomPrePassBackgroundColorsGradient>(BloomPrePassBackgroundColorsGradient);
        comp.Elements = Elements
            .Select(x => new BloomPrePassBackgroundColorsGradientFromColorSchemeColors.Element
            {
                LoadFromColorScheme = x.LoadFromColorScheme,
                EnvironmentColor =
                    (BloomPrePassBackgroundColorsGradientFromColorSchemeColors.EnvironmentColor)x.EnvironmentColor,
                Intensity = x.Intensity,
                Color = x.Color
            })
            .ToArray();
    }
}
