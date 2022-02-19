using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class ButtonComponent : CMUIComponentBase
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    private Action onClick;

    /// <summary>
    /// Assigns a callback when this button is clicked.
    /// </summary>
    /// <param name="onClick">On click callback</param>
    /// <returns>Itself, for chaining methods.</returns>
    public ButtonComponent OnClick(Action onClick)
    {
        this.onClick = onClick;
        return this;
    }

    /// <summary>
    /// Assigns localized text as the label for the button.
    /// </summary>
    /// <param name="table">Table which holds the localized text</param>
    /// <param name="key">Key for the localized text</param>
    /// <param name="args">Additional arguments if string formatting is involved.</param>
    /// <returns>Itself, for chaining methods.</returns>
    public ButtonComponent WithLabel(string table, string key, params object[] args)
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        label.text = str;
        return this;
    }

    /// <summary>
    /// Assigns unlocalized text as a label for the button.
    /// </summary>
    /// <remarks>
    /// For CM development, it is *HIGHLY* recommended to use <see cref="WithLocalizedText(string, string, object[])"/>,
    /// so any and all text can be localized to different languages.
    /// 
    /// For plugin developers, feel free to continue using this.
    /// </remarks>
    /// <param name="text">Unlocalized text to assign to the label.</param>
    /// <returns>Itself, for chaining methods.</returns>
    public ButtonComponent WithLabel(string text)
    {
        Debug.LogWarning("ButtonComponent using unlocalized text.");
        label.text = text;
        return this;
    }

    /// <summary>
    /// Assigns a custom color to the background of this button. This does not affect the color of the label text.
    /// </summary>
    /// <param name="color">Background color</param>
    /// <returns>Itself, for chaining methods.</returns>
    public ButtonComponent WithBackgroundColor(Color color)
    {
        button.targetGraphic.color = color;
        return this;
    }

    private void Start() => button.onClick.AddListener(() => onClick?.Invoke());

    private void OnDestroy() => button.onClick.RemoveAllListeners();
}
