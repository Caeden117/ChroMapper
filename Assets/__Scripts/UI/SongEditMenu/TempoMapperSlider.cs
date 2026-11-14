using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Runtime value binding for a slider, readout and reset button configured in the workspace prefab.</summary>
public class TempoMapperSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextComponent readout;
    [SerializeField] private ButtonComponent reset;
    private Func<float, string> format;

    public Slider Slider => slider;

    public void Initialize(float initialValue, Action<float> changed, Func<float, string> formatter)
    {
        format = formatter;
        var component = slider.GetComponentInParent<SliderComponent>();
        if (component != null) component.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.AddListener(value =>
        {
            changed(value);
            readout.Value = format(value);
        });
        reset.OnClick(() =>
        {
            SetValueWithoutNotify(initialValue);
            changed(initialValue);
        });
        SetValueWithoutNotify(initialValue);
    }

    public void SetValueWithoutNotify(float value)
    {
        slider.SetValueWithoutNotify(value);
        readout.Value = format(value);
    }
}
