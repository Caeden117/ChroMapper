using System;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarComponent : CMUIComponentWithLabel<Progress<float>>
{
    [SerializeField] private Slider progressSlider;

    private Func<float, string> progressTextFormatter;

    /// <summary>
    /// Manually updates the progress bar with the provided progress value.
    /// The progress bar's value will be overwritten if a <see cref="Progress{T}"/> has been assigned,
    /// and if <see cref="Progress{T}.ProgressChanged"/> was invoked.
    /// </summary>
    /// <param name="progress">Progress value, between 0-1.</param>
    public void UpdateProgressBar(float progress) => ProgressChanged(null, progress);

    /// <summary>
    /// Assigns a custom text formatter for the progress bar label.
    /// </summary>
    public ProgressBarComponent WithCustomLabelFormatter(Func<float, string> formatter)
    {
        progressTextFormatter = formatter;
        return this;
    }

    protected override void OnValueUpdated(Progress<float> updatedValue)
    {
        Value.ProgressChanged -= ProgressChanged;
        Value.ProgressChanged += ProgressChanged;
    }

    private void Start()
    {
        if (Value != null)
        {
            Value.ProgressChanged += ProgressChanged;
        }

        ProgressChanged(null, 0);
    }

    private void ProgressChanged(object _, float progress)
    {
        if (progressTextFormatter != null)
        {
            SetLabelText(progressTextFormatter(progress));
        }
        else
        {
            SetLabelText($"{progress * 100:F1}% complete.");
        }

        progressSlider.value = progress;
    }

    private void OnDestroy()
    {
        if (Value != null)
        {
            Value.ProgressChanged -= ProgressChanged;
        }
    }
}
