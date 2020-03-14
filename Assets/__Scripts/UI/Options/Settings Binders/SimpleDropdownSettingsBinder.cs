using System;
using TMPro;

/// <summary>
/// Simple Settings Binder to take an integer value from a TextMeshPro dropdown.
/// </summary>
public class SimpleDropdownSettingsBinder : SettingsBinder
{
    public TMP_Dropdown dropdown;

    private bool firstSet = true;

    private void Start()
    {
        dropdown.value = (int)RetrieveValueFromSettings();
    }

    public void SendDropdownToSettings(int value)
    {
        SendValueToSettings(value);
    }

    protected override object SettingsToUIValue(object input) => Convert.ToInt32(input);

    protected override object UIValueToSettings(object input) => Convert.ToInt32(input);
}
