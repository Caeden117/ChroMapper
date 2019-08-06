using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColourManager {

    /*
     * ColourManager Lite™ | Slimmed down version from Chroma
     */

    public const int RGB_INT_OFFSET = 2000000000;
    public const int RGB_RESET = 1900000001;
    public const int RGB_ALT = 1900000002;
    public const int RGB_WHITE = 1900000003;
    public const int RGB_TECHNI = 1900000004; //Not needed??
    public const int RGB_RANDOM = 1900000005;

    public static readonly Color DefaultLightAltA = new Color(1, 0.032f, 1, 1); //255, 8, 255
    public static readonly Color DefaultLightAltB = new Color(0.016f, 1, 0.016f, 1); //4, 255, 4
    public static readonly Color DefaultLightWhite = new Color(1, 1, 1, 1); //Color.white

    public static int ColourToInt(Color color)
    {
        int r = Mathf.FloorToInt(color.r * 255);
        int g = Mathf.FloorToInt(color.g * 255);
        int b = Mathf.FloorToInt(color.b * 255);
        return RGB_INT_OFFSET + (((r & 0x0ff) << 16) | ((g & 0x0ff) << 8) | (b & 0x0ff));
    }

    public static Color ColourFromInt(int rgb)
    {
        rgb = rgb - RGB_INT_OFFSET;
        int red = (rgb >> 16) & 0x0ff;
        int green = (rgb >> 8) & 0x0ff;
        int blue = (rgb) & 0x0ff;
        return new Color(red / 255f, green / 255f, blue / 255f, 1);
    }
}
