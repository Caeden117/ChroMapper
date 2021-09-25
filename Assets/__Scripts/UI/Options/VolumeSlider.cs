using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dbValueText;
    [SerializeField] private Slider slider;

    public float Value
    {
        get => slider.value;
        set => slider.value = value;
    }

    private void Start()
    {
        if (TryGetComponent<SettingsBinder>(out var settingsBinder))
        {
            slider.onValueChanged.AddListener(OnHandleMove);
            slider.SetValueWithoutNotify((float?)settingsBinder.RetrieveValueFromSettings() ?? 0);
            UpdateDisplay(false);
        }
    }

    private void OnHandleMove(float value) => UpdateDisplay();

    private void UpdateDisplay(bool sendToSettings = true)
    {
        dbValueText.text = Value == 0f ? "Off" : (20.0f * Mathf.Log10(Value)).ToString("F0") + " dB";

        if (sendToSettings) SendMessage("SendValueToSettings", Value);
    }
}
