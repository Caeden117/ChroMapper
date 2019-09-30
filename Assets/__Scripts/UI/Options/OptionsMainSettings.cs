using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionsMainSettings : MonoBehaviour
{
    [SerializeField] private TMP_InputField customLevelField;
    [SerializeField] private TextMeshProUGUI installFieldErrorText;
    [SerializeField] private Toggle discordToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeSliderDisplay;

    private void Start()
    {
        customLevelField.text = Settings.Instance.BeatSaberInstallation;
        discordToggle.isOn = Settings.Instance.DiscordRPCEnabled;
        volumeSlider.value = AudioListener.volume * 10;
        volumeSliderDisplay.text = $"{volumeSlider.value * 10}%";
    }

    public void UpdateDiscordRPC(bool enable)
    {
        Settings.Instance.DiscordRPCEnabled = enable;
        PersistentUI.Instance.DisplayMessage("Change will apply after restart!", PersistentUI.DisplayMessageType.BOTTOM);
    }

    public void UpdateBeatSaberInstall(string value)
    {
        string old = Settings.Instance.BeatSaberInstallation;
        Settings.Instance.BeatSaberInstallation = value;
        installFieldErrorText.text = "All good!";
        if (!Settings.ValidateDirectory(ErrorFeedback))
            Settings.Instance.BeatSaberInstallation = old;
    }

    private void ErrorFeedback(string feedback)
    {
        installFieldErrorText.text = feedback;
    }

    public void UpdateGameVolume(float value)
    {
        AudioListener.volume = value / 10;
        Settings.Instance.Volume = value / 10;
        volumeSliderDisplay.text = $"{volumeSlider.value * 10}%";
    }
}
