using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ThemedText : SingleTypeThemedObject
{
    private TextMeshProUGUI text;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    protected override void HandleTheme(ThemeSO theme)
    {
        if (text == null)
            return;
        Color newColor = theme.GetColorOfType(ColorType);
        newColor.a = text.color.a; //preserve alpha
        text.color = newColor;
        text.font = theme.font;
    }
}