using UnityEngine;
using TMPro;

public class ValidateDirectorySettingsBinder : SettingsBinder
{
    [SerializeField] private TMP_Text errorText;

    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input)
    {
        string old = Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance).ToString();
        Settings.AllFieldInfos[BindedSetting].SetValue(Settings.Instance, input);
        errorText.text = "All good!";
        if (!Settings.ValidateDirectory(ErrorFeedback))
        {
            return old;
        }
        else
        {
            return input;
        }
    }

    private void ErrorFeedback(string feedback)
    {
        errorText.text = feedback;
    }
}
