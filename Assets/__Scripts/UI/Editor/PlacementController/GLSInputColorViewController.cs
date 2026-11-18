using Beatmap.Base;
using UnityEngine;

public class GLSInputColorViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent brightnessInputField;
    [SerializeField] private TextBoxFloatComponent strobeBrightnessInputField;
    [SerializeField] private TextBoxIntComponent strobeFrequencyInputField;
    [SerializeField] private ToggleComponent fadeToggle;
    [SerializeField] private ToggleComponent strobeFadeToggle;

    [SerializeField] private ButtonComponent colorDistributionPickerButton;
    [SerializeField] private GLSColorDistributionPicker colorDistributionPicker;

    public void Start()
    {
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnFadeChanged += HandleEasingChanged;
        brightnessInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleBrightnessInputChanged);
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        strobeBrightnessInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleStrobeBrightnessInputChanged);
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        strobeFrequencyInputField.OnValueChanged(HandleStrobeFrequencyInputChanged);
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        inputController.OnColorDistributionsChanged += HandleColorDistributionsChanged;
        inputController.OnStrobeColorDistributionsChanged += HandleStrobeColorDistributionsChanged;
        fadeToggle.OnValueChanged(HandleFadeInputChanged);
        easingInputController.OnEasingChanged += HandleEasingChanged;
        strobeFadeToggle.OnValueChanged(HandleStrobeFadeInputChanged);

        // Replay the placement owner's cached values after this inactive tab view has subscribed.
        inputController.RefreshViews();

        colorDistributionPicker.OnColorDistributionsChanged += s =>
        {
            inputController.NotifyColorDistributionsChanged(s, false);
            UpdateButtonText();
        };
        colorDistributionPicker.OnStrobeColorDistributionsChanged += s =>
        {
            inputController.NotifyColorDistributionsChanged(s, true);
            UpdateButtonText();
        };
        colorDistributionPickerButton.OnClick(() => colorDistributionPicker.Open());
    }

    public void OnDestroy()
    {
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnFadeChanged -= HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        inputController.OnColorDistributionsChanged -= HandleColorDistributionsChanged;
        inputController.OnStrobeColorDistributionsChanged -= HandleStrobeColorDistributionsChanged;
        easingInputController.OnEasingChanged -= HandleEasingChanged;
    }


    // TODO: turns out it's not needed but just in case i'll leave it here atm
    private void HandleColorChanged(int value)
    {
        // QueuedData.Color = value;
    }

    // Cache replayed placement values so delayed CMUI initialization cannot repaint these controls with prefab defaults.
    private void HandleBrightnessChanged(float value) => brightnessInputField.SetValueWithoutNotify(value * 100f);

    private void HandleBrightnessInputChanged(float value) => inputController.NotifyBrightnessChanged(value / 100f);

    private void HandleStrobeBrightnessChanged(float value) =>
        strobeBrightnessInputField.SetValueWithoutNotify(value * 100f);

    private void HandleStrobeBrightnessInputChanged(float value) =>
        inputController.NotifyStrobeBrightnessChanged(value / 100f);

    private void HandleStrobeFrequencyChanged(int value) => strobeFrequencyInputField.SetValueWithoutNotify(value);

    private void HandleStrobeFrequencyInputChanged(int value) => inputController.NotifyStrobeFrequencyChanged(value);

    private void HandleSoftStrobeChanged(int value) => strobeFadeToggle.SetValueWithoutNotify(value == 1);

    private void HandleStrobeFadeInputChanged(bool value) => inputController.NotifySoftStrobeChanged(value ? 1 : 0);

    private void HandleColorDistributionsChanged(string[] value)
    {
        colorDistributionPicker.SetColorDistributions(value, null);
        UpdateButtonText();
    }

    private void HandleStrobeColorDistributionsChanged(string[] value)
    {
        colorDistributionPicker.SetColorDistributions(null, value);
        UpdateButtonText();
    }

    private void HandleEasingChanged(int value) => fadeToggle.SetValueWithoutNotify(value >= 0);

    // Cache every GLS color control so opening its tab cannot repaint saved values with component defaults.
    public void ApplyEditorState(
        float brightness,
        float strobeBrightness,
        int strobeFrequency,
        int easing,
        int strobeFade,
        string[] colorDistributions = null,
        string[] strobeColorDistributions = null)
    {
        brightnessInputField.SetValueWithoutNotify(brightness * 100f);
        strobeBrightnessInputField.SetValueWithoutNotify(strobeBrightness * 100f);
        strobeFrequencyInputField.SetValueWithoutNotify(strobeFrequency);
        // Cache the CMUI values too, otherwise ToggleComponent.Start redraws its default false state after load.
        fadeToggle.SetValueWithoutNotify(easing >= 0);
        strobeFadeToggle.SetValueWithoutNotify(strobeFade == 1);

        colorDistributionPicker.SetColorDistributions(colorDistributions, strobeColorDistributions);
        UpdateButtonText();
    }

    private void UpdateButtonText()
    {
        var numColorDistributions = colorDistributionPicker.ColorDistributions.Length;
        var numStrobeColorDistributions = colorDistributionPicker.StrobeColorDistributions.Length;
        colorDistributionPickerButton.SetLabelText($"{numColorDistributions} : {numStrobeColorDistributions}");
    }

    // Fade must notify the GLS color owner directly because generic easing suppresses an unchanged cached Linear value.
    private void HandleFadeInputChanged(bool value) => inputController.NotifyFadeChanged(value ? 0 : -1);
}
