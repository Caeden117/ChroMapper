using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ColorLabel : MonoBehaviour
{
    [FormerlySerializedAs("picker")] public ColorPicker Picker;

    [FormerlySerializedAs("type")] public ColorValues Type;

    [FormerlySerializedAs("prefix")] public string Prefix = "R: ";
    [FormerlySerializedAs("minValue")] public float MINValue;
    [FormerlySerializedAs("maxValue")] public float MAXValue = 255;

    [FormerlySerializedAs("precision")] public int Precision;

    private Text label;

    private void Awake() => label = GetComponent<Text>();

    private void OnEnable()
    {
        if (Application.isPlaying && Picker != null)
        {
            Picker.ONValueChanged.AddListener(ColorChanged);
            Picker.OnhsvChanged.AddListener(HSVChanged);
        }
    }

    private void OnDestroy()
    {
        if (Picker != null)
        {
            Picker.ONValueChanged.RemoveListener(ColorChanged);
            Picker.OnhsvChanged.RemoveListener(HSVChanged);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        label = GetComponent<Text>();
        UpdateValue();
    }
#endif

    private void ColorChanged(Color color) => UpdateValue();

    private void HSVChanged(float hue, float sateration, float value) => UpdateValue();

    private void UpdateValue()
    {
        if (Picker == null)
        {
            label.text = Prefix + "-";
        }
        else
        {
            var value = MINValue + (Picker.GetValue(Type) * (MAXValue - MINValue));

            label.text = Prefix + ConvertToDisplayString(value);
        }
    }

    private string ConvertToDisplayString(float value)
    {
        if (Precision > 0)
            return value.ToString("f " + Precision);
        return Mathf.FloorToInt(value).ToString();
    }
}
