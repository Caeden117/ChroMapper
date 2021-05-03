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
    }

    public void ToggleExtendedOptions(bool enabled)
    {
        extendedOptionsPanel.SetActive(enabled);
        extendedOptionsToggle.transform.localEulerAngles = Vector3.forward * (enabled ? 1 : 0) * 180f;
        SendMessageUpwards("DirtySettingsList");
    }

    public abstract StrobeGeneratorPass GetPassForGeneration();
}
