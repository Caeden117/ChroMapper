using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderComponent : CMUIComponentWithLabel<float>
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI display;

    private float precision = 0;
    private Func<float, string> sliderTextFormatter;

    /// <summary>
    /// Define slider parameters, such as the bounds and snapping precision.
    /// </summary>
    /// <param name="minValue">Minimum value for the slider.</param>
    /// <param name="maxValue">Maximum value for the slider.</param>
    /// <param name="precision">If non-zero, round slider value to the nearest precision value.</param>
    public SliderComponent WithSliderParams(float minValue = 0, float maxValue = 1, float precision = 0)
    {
        slider.minValue = minValue / (precision == 0 ? 1 : precision);
        slider.maxValue = maxValue / (precision == 0 ? 1 : precision);
        slider.wholeNumbers = precision != 0;
        this.precision = precision;

        return this;
    }

    /// <summary>
    /// Defines visibility of the slider display.
    /// </summary>
    /// <param name="visible">Visibility of the slider display text.</param>
    public SliderComponent WithDisplay(bool visible)
    {
        display.gameObject.SetActive(visible);
        return this;
    }

    /// <summary>
    /// Assigns a custom text formatter for the slider display.
    /// </summary>
    public SliderComponent WithCustomDisplayFormatter(Func<float, string> formatter)
    {
        sliderTextFormatter = formatter;
        return this;
    }

    protected override void OnValueUpdated(float updatedValue)
    {
        slider.SetValueWithoutNotify(updatedValue);
        
        UpdateText();
    }

    private void Start() => slider.onValueChanged.AddListener(SliderValueChanged);

    private void SliderValueChanged(float value)
        => Value = precision == 0
            ? value
            : Mathf.Round(value / precision) * precision;

    private void UpdateText()
        => display.text = sliderTextFormatter == null
            ? $"{Value * 100:F1}%"
            : sliderTextFormatter(Value);

    private void OnDestroy() => slider.onValueChanged.RemoveAllListeners();
}
