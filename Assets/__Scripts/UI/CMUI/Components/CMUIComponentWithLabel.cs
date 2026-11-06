using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// Generic CMUI component with an included label.
/// </summary>
/// <typeparam name="T">Type being handled by this component.</typeparam>
public abstract class CMUIComponentWithLabel<T> : CMUIComponent<T>
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject labelContainer;

    internal Color LabelColor
    {
        get => labelText.color;
        set => labelText.color = value;
    }

    internal override void SetLabelEnabled(bool enabled) => labelContainer.SetActive(enabled);

    internal override void SetLabelText(string text) => labelText.text = text;

    public CMUIComponentWithLabel<T> SetLabelText(string table, string key, params object[] args)
    {
        labelText.text = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        return this;
    }
}
