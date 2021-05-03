using HarmonyLib;
using UnityEngine;

static class ColorExtensions
{
    /// <summary>
    /// Sets the alpha component of a <see cref="Color"/> without allocating a new object.
    /// </summary>
    public static Color WithAlpha(this Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    /// <summary>
    /// Sets the various components of a <see cref="Color"/> without allocating a new object.
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
    /// Multiply a <see cref="Color"/> by a number without allocating a new object.
    /// </summary>
    public static Color Multiply(this Color color, float x)
    {
        color.r *= x;
        color.g *= x;
        color.b *= x;
        color.a *= x;
        return color;
    }
}