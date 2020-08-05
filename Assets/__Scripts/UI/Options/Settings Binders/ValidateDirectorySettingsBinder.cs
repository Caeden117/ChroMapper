using UnityEngine;
using UnityEngine.Localization.Components;

public class ValidateDirectorySettingsBinder : SettingsBinder
{
    [SerializeField] private LocalizeStringEvent errorText;

    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input)
    {
        string old = Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance).ToString();
        Settings.AllFieldInfos[BindedSetting].SetValue(Settings.Instance, input);
        errorText.StringReference.TableEntryReference = "validate.good";
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
        errorText.StringReference.TableEntryReference = feedback;
    }
}
