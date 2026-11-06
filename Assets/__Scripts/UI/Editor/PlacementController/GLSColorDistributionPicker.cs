using System;
using UnityEngine;

public class GLSColorDistributionPicker : MonoBehaviour
{
    [SerializeField] private GLSColorDistributionArrayViewController colorDistributions;
    [SerializeField] private GLSColorDistributionArrayViewController strobeColorDistributions;
    [SerializeField] private ButtonComponent clearButton;
    [SerializeField] private ButtonComponent closeButton;
    [SerializeField] private FlyoutPanelController flyout;

    public event Action<string[]> OnColorDistributionsChanged;
    public event Action<string[]> OnStrobeColorDistributionsChanged;

    public string[] ColorDistributions => colorDistributions.GetColorDistributions();
    public string[] StrobeColorDistributions => strobeColorDistributions.GetColorDistributions();

    public void Open()
    {
        flyout.Open();
    }

    private void Awake()
    {
        clearButton.OnClick(HandleClearClicked);
        colorDistributions.Initialize(value => OnColorDistributionsChanged?.Invoke(value));
        strobeColorDistributions.Initialize(value => OnStrobeColorDistributionsChanged?.Invoke(value));
        closeButton.OnClick(() => flyout.Close());
    }

    public void SetColorDistributions(string[] colorDistributionsValue, string[] strobeColorDistributionsValue)
    {
        // Older maps or empty selections may have no distribution arrays. Initialize each list independently.
        if (colorDistributionsValue != null)
        {
            colorDistributions.SetColorDistributions(colorDistributionsValue);
        }

        if (strobeColorDistributionsValue != null)
        {
            strobeColorDistributions.SetColorDistributions(strobeColorDistributionsValue);
        }
    }

    private void HandleClearClicked()
    {
        colorDistributions.SetColorDistributions(Array.Empty<string>());
        strobeColorDistributions.SetColorDistributions(Array.Empty<string>());
        OnColorDistributionsChanged?.Invoke(Array.Empty<string>());
        OnStrobeColorDistributionsChanged?.Invoke(Array.Empty<string>());
    }
}
