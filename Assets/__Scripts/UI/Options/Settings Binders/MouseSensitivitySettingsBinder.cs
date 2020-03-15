using System;
using UnityEngine;

public class MouseSensitivitySettingsBinder : SettingsBinder
{
    protected override object SettingsToUIValue(object input)
    {
        return Mathf.Round((Convert.ToSingle(input) - 0.5f) * 2);
    }

    protected override object UIValueToSettings(object input)
    {
        return (Convert.ToSingle(input) / 2) + 0.5f;
    }
}
