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
        customLevelField.text = Settings.BeatSaberInstallation;
        discordToggle.isOn = Settings.DiscordRPCEnabled;
        volumeSlider.value = AudioListener.volume * 10;
        volumeSliderDisplay.text = $"{volumeSlider.value * 10}%";
    }

    public void UpdateDiscordRPC(bool enable)
    {
        Settings.DiscordRPCEnabled = enable;
        PersistentUI.Instance.DisplayMessage("Change will apply after restart!", PersistentUI.DisplayMessageType.BOTTOM);
    }

    public void UpdateBeatSaberInstall(string value)
    {
        Settings.BeatSaberInstallation = value;
        installFieldErrorText.text = "All good!";
        if (Settings.ValidateDirectory((res) =>
        {
            installFieldErrorText.text = res;
        }))//Confusing if statement, but sets install string if directory is validated, and sets text if its not.
            PlayerPrefs.SetString("install", Settings.BeatSaberInstallation);
    }

    public void UpdateGameVolume(float value)
    {
        AudioListener.volume = value / 10;
        volumeSliderDisplay.text = $"{volumeSlider.value * 10}%";
    }
}
