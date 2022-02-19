using UnityEngine;

public class ColourManager
{
    /*
     * ColourManager Lite™ | Slimmed down version from Chroma
     */

    public const int RgbintOffset = 2000000000;
    public const int RGBReset = 1900000001;
    public const int RGBAlt = 1900000002;
    public const int RGBWhite = 1900000003;
    public const int RGBTechni = 1900000004; //Not needed??
    public const int RGBRandom = 1900000005;

    public static readonly Color DefaultLightAltA = new Color(1, 0.032f, 1, 1); //255, 8, 255
    public static readonly Color DefaultLightAltB = new Color(0.016f, 1, 0.016f, 1); //4, 255, 4
    public static readonly Color DefaultLightWhite = new Color(1, 1, 1, 1); //Color.white

    public static int ColourToInt(Color color)
    {
        var r = Mathf.FloorToInt(color.r * 255);
        var g = Mathf.FloorToInt(color.g * 255);
        var b = Mathf.FloorToInt(color.b * 255);
        return RgbintOffset + (((r & 0x0ff) << 16) | ((g & 0x0ff) << 8) | (b & 0x0ff));
    }

    public static Color ColourFromInt(int rgb)
    {
        rgb -= RgbintOffset;
        var red = (rgb >> 16) & 0x0ff;
        var green = (rgb >> 8) & 0x0ff;
        var blue = rgb & 0x0ff;
        return new Color(red / 255f, green / 255f, blue / 255f, 1);
    }
}
