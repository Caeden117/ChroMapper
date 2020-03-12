/// <summary>
/// A simple Settings Binder that stores and retrieves data without any kind of modification.
/// Use cases include simple toggles, sliders, and text inputs.
/// </summary>
public class SimpleSettingsBinder : SettingsBinder
{
    protected override object SettingsToUIValue(object input) => input;

    protected override object UIValueToSettings(object input) => input;
}
