using UnityEngine;
using UnityEngine.UI;

public abstract class StrobeGeneratorPassUIController : MonoBehaviour
{
    [SerializeField] private Toggle strobePassToggle;
    [SerializeField] private Toggle extendedOptionsToggle;
    [SerializeField] private GameObject extendedOptionsPanel;

    public bool WillGenerate => strobePassToggle.isOn;

    internal void Start()
    {
        extendedOptionsToggle.isOn = false;
        strobePassToggle.isOn = false;
    }

    public void ToggleExtendedOptions(bool enabled)
    {
        extendedOptionsPanel.SetActive(enabled);
        extendedOptionsToggle.transform.localEulerAngles = (enabled ? 1 : 0) * 180f * Vector3.forward;
        SendMessageUpwards("DirtySettingsList");
    }

    public abstract StrobeGeneratorPass GetPassForGeneration();
}
