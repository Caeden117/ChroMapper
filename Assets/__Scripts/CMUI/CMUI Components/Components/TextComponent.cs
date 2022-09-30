using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class TextComponent : CMUIComponent<string>
{
    [SerializeField] private TextMeshProUGUI textMeshProUGUI;

    private void Start() => OnValueUpdated(Value);

    /// <summary>
    /// Use localized text.
    /// </summary>
    /// <param name="table">Localization table</param>
    /// <param name="key">Key within localization table</param>
    /// <param name="args">String arguments, if any</param>
    /// <returns>Itself, for method chaining.</returns>
    public TextComponent WithInitialValue(string table, string key, params object[] args)
    {
        Value = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        return this;
    }

    protected override void OnValueUpdated(string updatedValue) => textMeshProUGUI.text = updatedValue;
}
