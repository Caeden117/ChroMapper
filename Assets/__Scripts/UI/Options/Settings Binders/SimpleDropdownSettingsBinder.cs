using System;
using TMPro;
using UnityEngine.Serialization;

/// <summary>
///     Simple Settings Binder to take an integer value from a TextMeshPro dropdown.
/// </summary>
public class SimpleDropdownSettingsBinder : SettingsBinder
{
    [FormerlySerializedAs("dropdown")] public TMP_Dropdown Dropdown;

    private void Start()
    {
        Dropdown.SetValueWithoutNotify((int)RetrieveValueFromSettings());
    }

    public void SendDropdownToSettings(int value) => SendValueToSettings(value);

    protected override object SettingsToUIValue(object input) => Convert.ToInt32(input);

    protected override object UIValueToSettings(object input) => Convert.ToInt32(input);
}
