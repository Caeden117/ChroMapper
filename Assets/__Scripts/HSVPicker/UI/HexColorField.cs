using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class HexColorField : DisableActionsField
{
    [FormerlySerializedAs("hsvpicker")] public ColorPicker Hsvpicker;

    [FormerlySerializedAs("displayAlpha")] public bool DisplayAlpha;

    private InputField hexInputField;

    private void Awake()
    {
        hexInputField = GetComponent<InputField>();

        // Add listeners to keep text (and color) up to date
        hexInputField.onEndEdit.AddListener(UpdateColor);
        Hsvpicker.ONValueChanged.AddListener(UpdateHex);
    }

    private void OnDestroy()
    {
        hexInputField.onEndEdit.RemoveListener(UpdateColor);
        Hsvpicker.ONValueChanged.RemoveListener(UpdateHex);
    }

    private void UpdateHex(Color newColor) => hexInputField.text = ColorToHex(newColor);

    private void UpdateColor(string newHex)
    {
        if (!newHex.StartsWith("#"))
            newHex = "#" + newHex;
        if (ColorUtility.TryParseHtmlString(newHex, out var color))
            Hsvpicker.CurrentColor = color;
        else
        {
            Debug.Log(
                "hex value is in the wrong format, valid formats are: #RGB, #RGBA, #RRGGBB and #RRGGBBAA (# is optional)");
        }
    }

    private string ColorToHex(Color32 color) =>
        DisplayAlpha
            ? string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", color.r, color.g, color.b, color.a)
            : string.Format("#{0:X2}{1:X2}{2:X2}", color.r, color.g, color.b);
}
