using SimpleJSON;
using System;

public class JSONDictionarySettingsBinder : SettingsBinder
{
    public string dictionaryKey;

    protected override object SettingsToUIValue(object input)
    {
        var settings = (JSONDictionarySetting)input;
        var setting = settings[dictionaryKey];

        switch (setting.Tag)
        {
            case JSONNodeType.String:
                return (string)setting;
            case JSONNodeType.Number:
                return (double)setting;
            case JSONNodeType.Boolean:
                return (bool)setting;
            default:
                return null;
        }
    }

    protected override object UIValueToSettings(object input)
    {
        var settings = (JSONDictionarySetting)Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance);

        // must be dynamic for casting to JSONNode to work properly
        dynamic setting = Convert.ChangeType(input, input.GetType());
        settings[dictionaryKey] = (JSONNode)setting;
        return settings;
    }
}