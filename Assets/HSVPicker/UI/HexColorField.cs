using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class HexColorField : MonoBehaviour
{
    public ColorPicker hsvpicker;

    public bool displayAlpha;

    private InputField hexInputField;

    private void Awake()
    {
        hexInputField = GetComponent<InputField>();

        // Add listeners to keep text (and color) up to date
        hexInputField.onValueChanged.AddListener(TextValueChanged);
        hexInputField.onEndEdit.AddListener(UpdateColor);
        hsvpicker.onValueChanged.AddListener(UpdateHex);
    }

    private void OnDestroy()
    {
        hexInputField.onValueChanged.RemoveListener(TextValueChanged);
        hexInputField.onEndEdit.RemoveListener(UpdateColor);
        hsvpicker.onValueChanged.RemoveListener(UpdateHex);
    }

    private void TextValueChanged(string arg0)
    {
        if (!hexInputField.isFocused) return;
        CMInputCallbackInstaller.DisableActionMaps(typeof(HexColorField), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
    }

    private void UpdateHex(Color newColor)
    {
        hexInputField.text = ColorToHex(newColor);
    }

    private void UpdateColor(string newHex)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(HexColorField), typeof(CMInput).GetNestedTypes().Where(x => x.IsInterface));
        Color color;
        if (!newHex.StartsWith("#"))
            newHex = "#"+newHex;
        if (ColorUtility.TryParseHtmlString(newHex, out color))
            hsvpicker.CurrentColor = color;
        else
            Debug.Log("hex value is in the wrong format, valid formats are: #RGB, #RGBA, #RRGGBB and #RRGGBBAA (# is optional)");
    }

    private string ColorToHex(Color32 color)
    {
        return displayAlpha
            ? string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.r, color.g, color.b, color.a)
            : string.Format("#{0:X2}{1:X2}{2:X2}", color.r, color.g, color.b);
    }
}
