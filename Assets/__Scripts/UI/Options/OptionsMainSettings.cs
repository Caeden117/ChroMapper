using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OptionsMainSettings : MonoBehaviour
{
    [SerializeField] private TMP_InputField customLevelField;
    [SerializeField] private Toggle discordToggle;

    private void Start()
    {
        customLevelField.text = Settings.BeatSaberInstallation;
        discordToggle.isOn = Settings.DiscordRPCEnabled;
    }

    public void UpdateDiscordRPC(bool enable)
    {
        Settings.DiscordRPCEnabled = enable;
        PersistentUI.Instance.DisplayMessage("Change will apply after restart!", PersistentUI.DisplayMessageType.BOTTOM);
    }
}
