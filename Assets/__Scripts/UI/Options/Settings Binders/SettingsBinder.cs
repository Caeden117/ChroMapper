using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for Settings Binders, which abstract settings from one huge ass super class.
/// </summary>
public abstract class SettingsBinder : MonoBehaviour
{
    public SettingsType BindedSettingType = SettingsType.ALL;

    public string BindedSetting = "None";

    public void SendValueToSettings(object value)
    {
        if (!string.IsNullOrEmpty(BindedSetting) && BindedSetting != "None")
        {
            Settings.ApplyOptionByName(BindedSetting, UIValueToSettings(value));
        }
    }

    public object RetrieveValueFromSettings()
    {
        if (string.IsNullOrEmpty(BindedSetting) || BindedSetting == "None") return null;
        return SettingsToUIValue(Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance));
    }

    /// <summary>
    /// Takes an input from an outside UI Element and transforms it to a value ready to be stored into settings.
    /// </summary>
    /// <param name="input">Value from a UI Element, such as a Slider.</param>
    /// <returns>A modified version designed to be intepreted internally, and be saved into the settings file.</returns>
    protected abstract object UIValueToSettings(object input);

    /// <summary>
    /// Takes an input from the settings file and transforms it to a value ready to be used with a UI Element.
    /// </summary>
    /// <param name="input">Value from Settings.</param>
    /// <returns>A modified version designed to be used witrh UI Elements, such as a Slider.</returns>
    protected abstract object SettingsToUIValue(object input);
    
    public enum SettingsType
    {
        ALL,
        STRING,
        INT,
        SINGLE,
        BOOL
    }
}
