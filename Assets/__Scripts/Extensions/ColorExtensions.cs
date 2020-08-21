using UnityEngine;

static class ColorExtensions
{
    public static Color WithAlpha(this Color color, int alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }
}