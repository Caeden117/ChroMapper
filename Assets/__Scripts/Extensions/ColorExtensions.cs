using UnityEngine;

internal static class ColorExtensions
{
    /// <summary>
    ///     Sets the alpha component of a <see cref="Color" /> without allocating a new object.
    /// </summary>
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    /// <summary>
    ///     Sets the various components of a <see cref="Color" /> without allocating a new object.
    /// </summary>
    public static Color Set(this Color color, float r, float g, float b, float a)
    {
        color.r = r;
        color.g = g;
        color.b = b;
        color.a = a;
        return color;
    }

    /// <summary>
    ///     Multiply a <see cref="Color" /> by a number without allocating a new object.
    /// </summary>
    public static Color Multiply(this Color color, float x)
    {
        color.r *= x;
        color.g *= x;
        color.b *= x;
        color.a *= x;
        return color;
    }

    /// <summary>
    /// Modifies a color by setting the specified satuation
    /// </summary>
    /// <remarks>
    /// This method first converts a color to its HSV equivlanet, sets the satuation, then re-converts back to RGB.
    /// </remarks>
    public static Color WithSatuation(this Color color, float saturation)
    {
        var hsv = HSVUtil.ConvertRgbToHsv(color);
        hsv.S = saturation;
        return HSVUtil.ConvertHsvToRgb(hsv.H, hsv.S, hsv.V, color.a);
    }
}
