using System;
using TMPro;
using UnityEngine.Serialization;

/// <summary>
///     Simple Settings Binder to match a setting to a TMP Dropdown value
/// </summary>
public class IntDropdownSettingsBinder : SettingsBinder
{
    [FormerlySerializedAs("dropdown")] public TMP_Dropdown Dropdown;

    private void Start()
    {
        Dropdown.SetValueWithoutNotify((int)RetrieveValueFromSettings());
    }

    public void SendDropdownToSettings(int value) => SendValueToSettings(value);

    protected override object SettingsToUIValue(object input)
    {
        var valueStr = input.ToString();
        
        for (var i = 0; i < Dropdown.options.Count; i++)
        {
            var opt = Dropdown.options[i];

            if (opt.text == valueStr) return i;
        }

        return 0;
    }

    protected override object UIValueToSettings(object input) => Convert.ToInt32(Dropdown.options[(int)input].text);
}
