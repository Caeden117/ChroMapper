using System;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTheme", menuName = "ScriptableObjects/Theme", order = 1)]
public class ThemeSO : ScriptableObject
{
    public Color backgroundPrimary;
    public Color backgroundPrimaryText;
    public Color backgroundDark;
    public Color backgroundDarkText;
    public Color backgroundLight;
    public Color backgroundLightText;
    public Color accentPrimary;
    public Color accentPrimaryText;
    public Color accentDark;
    public Color accentDarkText;
    public Color accentLight;
    public Color accentLightText;

    public TMP_FontAsset font;

    public Color GetColorOfType(ColorType type)
    {
        switch (type)
        {
            case ColorType.BACKGROUND_PRIMARY:
                return backgroundPrimary;
            case ColorType.BACKGROUND_DARK:
                return backgroundDark;
            case ColorType.BACKGROUND_LIGHT:
                return backgroundLight;
            case ColorType.ACCENT_PRIMARY:
                return accentPrimary;
            case ColorType.ACCENT_DARK:
                return accentDark;
            case ColorType.ACCENT_LIGHT:
                return accentLight;
            case ColorType.BACKGROUND_PRIMARY_TEXT:
                return backgroundPrimaryText;
            case ColorType.BACKGROUND_DARK_TEXT:
                return backgroundDarkText;
            case ColorType.BACKGROUND_LIGHT_TEXT:
                return backgroundLightText;
            case ColorType.ACCENT_PRIMARY_TEXT:
                return accentPrimaryText;
            case ColorType.ACCENT_DARK_TEXT:
                return accentDarkText;
            case ColorType.ACCENT_LIGHT_TEXT:
                return accentLightText;
        }
        throw new Exception($"No color found for type: {type}");
    }

    public enum ColorType
    {
        BACKGROUND_PRIMARY,
        BACKGROUND_PRIMARY_TEXT,
        BACKGROUND_DARK,
        BACKGROUND_DARK_TEXT,
        BACKGROUND_LIGHT,
        BACKGROUND_LIGHT_TEXT,
        ACCENT_PRIMARY,
        ACCENT_PRIMARY_TEXT,
        ACCENT_DARK,
        ACCENT_DARK_TEXT,
        ACCENT_LIGHT,
        ACCENT_LIGHT_TEXT
    }
}