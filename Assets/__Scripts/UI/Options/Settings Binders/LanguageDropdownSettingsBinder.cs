using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;

/// <summary>
///     Settings binder for localization
/// </summary>
public class LanguageDropdownSettingsBinder : SettingsBinder
{
    [FormerlySerializedAs("dropdown")] public TMP_Dropdown Dropdown;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        var available = (LocalesProvider)LocalizationSettings.AvailableLocales;
        yield return available.PreloadOperation;

        // Generate list of available Locales
        var options = new List<TMP_Dropdown.OptionData>();
        var selected = 0;
        for (var i = 0; i < available.Locales.Count; ++i)
        {
            var locale = available.Locales[i];
            if (LocalizationSettings.SelectedLocale.Identifier.Code.Equals(locale.Identifier.Code)) selected = i;
            options.Add(new TMP_Dropdown.OptionData(locale.name));
        }

        Dropdown.options = options;
        Dropdown.value = selected;
    }

    public void SendDropdownToSettings(int value)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales[value];
        LocalizationSettings.SelectedLocale = locale;
        SendValueToSettings(locale.Identifier.Code);
    }

    protected override object SettingsToUIValue(object input) => Convert.ToString(input);

    protected override object UIValueToSettings(object input) => Convert.ToString(input);
}
