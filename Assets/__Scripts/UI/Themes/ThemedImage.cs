using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ThemedImage : SingleTypeThemedObject
{
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }

    protected override void HandleTheme(ThemeSO theme)
    {
        if (image == null)
            return;
        Color newColor = theme.GetColorOfType(ColorType);
        newColor.a = image.color.a; //preserve alpha
        image.color = newColor;
    }
}