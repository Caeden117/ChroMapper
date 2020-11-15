using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

public class ValidateUpdateServerSettingsBinder : SettingsBinder
{
    [SerializeField] private LocalizeStringEvent errorText;
    public TMP_InputField inputField;
    private string pending;

    private void Start()
    {
        inputField.text = RetrieveValueFromSettings().ToString() ?? "";
    }

    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input)
    {
        pending = input.ToString();

        string old = Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance).ToString();

        if (pending != old)
        {
            errorText.StringReference.TableEntryReference = "misc.releaseserver.pending";
            StartCoroutine(UpdateChecker.GetLatestVersion(pending, "stable", VersionCheckCB));
        }
        else
        {
            VersionCheckCB(1);
        }

        return old;
    }

    private void VersionCheckCB(int v)
    {
        if (v > 0)
        {
            Settings.AllFieldInfos[BindedSetting].SetValue(Settings.Instance, pending);
            errorText.StringReference.TableEntryReference = "misc.releaseserver.good";
            return;
        }
        errorText.StringReference.TableEntryReference = "misc.releaseserver.bad";
    }

    public void SendValueToSettings()
    {
        SendValueToSettings(inputField.text);
    }
}
