/// <summary>
/// Use this Settings Binder if your setting does not exist in Settings, 
/// but requires Editor objects to be notified of its update.
/// </summary>
public class NonPersistentSettingsBinder : SettingsBinder
{
    public string OptionName;
    public string DefaultValue;

    protected override object SettingsToUIValue(object input) => input;
    protected override object UIValueToSettings(object input) => input;

    public override object RetrieveValueFromSettings()
    {
        if (Settings.NonPersistentSettings.TryGetValue(OptionName, out object value))
        {
            return value;
        }
        Settings.NonPersistentSettings.Add(OptionName, DefaultValue);
        return DefaultValue;
    }

    public override void SendValueToSettings(object value)
    {
        Settings.ManuallyNotifySettingUpdatedEvent(OptionName, value);
    }
}
