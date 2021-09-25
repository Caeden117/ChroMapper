using System;
using SimpleJSON;
using UnityEngine.Serialization;

public class JSONDictionarySettingsBinder : SettingsBinder
{
    [FormerlySerializedAs("dictionaryKey")] public string DictionaryKey;

    protected override object SettingsToUIValue(object input)
    {
        var settings = (JSONDictionarySetting)input;
        var setting = settings[DictionaryKey];

        return setting.Tag switch
        {
            JSONNodeType.String => (string)setting,
            JSONNodeType.Number => (double)setting,
            JSONNodeType.Boolean => (bool)setting,
            _ => null,
        };
    }

    protected override object UIValueToSettings(object input)
    {
        var settings = (JSONDictionarySetting)Settings.AllFieldInfos[BindedSetting].GetValue(Settings.Instance);

        // must be dynamic for casting to JSONNode to work properly
        dynamic setting = Convert.ChangeType(input, input.GetType());
        settings[DictionaryKey] = (JSONNode)setting;
        return settings;
    }
}
