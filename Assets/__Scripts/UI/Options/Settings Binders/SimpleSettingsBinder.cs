/// <summary>
/// A simple Settings Binder that stores and retrieves data without any kind of modification.
/// Use cases include simple toggles, sliders, and text inputs.
/// </summary>
public class SimpleSettingsBinder : SettingsBinder
{
    public override object SettingsToUIValue(object input) => input;

    public override object UIValueToSettings(object input) => input;
}
