using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;

public class ValidateUpdateServerSettingsBinder : SettingsBinder
{
    [SerializeField] private LocalizeStringEvent errorText;
    [FormerlySerializedAs("inputField")] public TMP_InputField InputField;
    private string pending;

    private void Start() => InputField.text = RetrieveValueFromSettings().ToString() ?? "";

    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input)
    {
        pending = input.ToString();

        var old = Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance).ToString();

        if (pending != old)
        {
            errorText.StringReference.TableEntryReference = "misc.releaseserver.pending";
            StartCoroutine(UpdateChecker.GetLatestVersion(pending, "stable", VersionCheckCb));
        }
        else
        {
            VersionCheckCb(1);
        }

        return old;
    }

    private void VersionCheckCb(int v)
    {
        if (v > 0)
        {
            Settings.AllFieldInfos[BindedSetting].SetValue(Settings.Instance, pending);
            errorText.StringReference.TableEntryReference = "misc.releaseserver.good";
            return;
        }

        errorText.StringReference.TableEntryReference = "misc.releaseserver.bad";
    }

    public void SendValueToSettings() => SendValueToSettings(InputField.text);
}
