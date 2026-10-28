using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>Serialized binding for the persistent Tempo Mapper category in the global options scene.</summary>
public class TempoMapperSettingBinding : MonoBehaviour
{
    public string SettingName;
    public string LabelKey;
    public TMP_Text Caption;
    public TMP_Dropdown Dropdown;
    public BetterToggle Toggle;
    public string[] Values;

    private void Start()
    {
        if (Caption != null) Caption.text = LocalizationSettings.StringDatabase.GetLocalizedString("SongEditMenu", "tempo." + LabelKey);
        if (string.IsNullOrEmpty(SettingName)) return;
        var field = typeof(Settings).GetField(SettingName);
        if (field == null) throw new ArgumentException("Unknown tempo setting: " + SettingName);
        var current = field.GetValue(Settings.Instance);
        if (Dropdown != null)
        {
            Dropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();
            Dropdown.SetValueWithoutNotify(Math.Max(0, Array.FindIndex(Values, value =>
                Equals(Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture), current))));
            Dropdown.onValueChanged.AddListener(index =>
            {
                Settings.ApplyOptionByName(SettingName, Convert.ChangeType(Values[index], field.FieldType, CultureInfo.InvariantCulture));
                Settings.Instance.Save();
            });
        }
        if (Toggle != null)
        {
            Toggle.IsOn = (bool)current;
            Toggle.OnValueChanged = new BetterToggle.ToggleEvent();
            Toggle.OnValueChanged.AddListener(value => { Settings.ApplyOptionByName(SettingName, value); Settings.Instance.Save(); });
            Toggle.UpdateUI();
        }
    }
}
