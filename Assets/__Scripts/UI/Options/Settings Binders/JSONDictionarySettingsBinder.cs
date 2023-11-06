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
        var setting = settings[DictionaryKey];

        // dynamic causes compile errors on .NET Standard
        settings[DictionaryKey] = setting.Tag switch
        {
            JSONNodeType.String => (string)input,
            JSONNodeType.Number => (double)input,
            JSONNodeType.Boolean => (bool)input,
            _ => throw new InvalidOperationException($"Unknown JSON Tag '{setting.Tag}'.")
        };

        return settings;
    }
}
