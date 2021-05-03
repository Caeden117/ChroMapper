using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Settings binder for localization
/// </summary>
public class LanguageDropdownSettingsBinder : SettingsBinder
{
    public TMP_Dropdown dropdown;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        var available = (LocalesProvider)LocalizationSettings.AvailableLocales;
        yield return available.PreloadOperation;

        // Generate list of available Locales
        var options = new List<TMP_Dropdown.OptionData>();
        int selected = 0;
        for (int i = 0; i < available.Locales.Count; ++i)
        {
            var locale = available.Locales[i];
            if (LocalizationSettings.SelectedLocale.Identifier.Code.Equals(locale.Identifier.Code))
            {
                selected = i;
            }
            options.Add(new TMP_Dropdown.OptionData(locale.name));
        }

        dropdown.options = options;
        dropdown.value = selected;
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
