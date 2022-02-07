using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class TextBoxComponent : CMUIComponentWithLabel<string>
{
    [SerializeField] private TMP_InputField inputField;

    private Action<string> onEndEdit;
    private Action<string> onSelect;
    private Action<string> onDeselect;

    /// <summary>
    /// Assigns a callback when the user deselects the textbox after making changes.
    /// </summary>
    public TextBoxComponent OnEndEdit(Action<string> onEndEdit)
    {
        this.onEndEdit = onEndEdit;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user selects text.
    /// </summary>
    public TextBoxComponent OnSelect(Action<string> onSelect)
    {
        this.onSelect = onSelect;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user deselects text.
    /// </summary>
    public TextBoxComponent OnDeselect(Action<string> onDeselect)
    {
        this.onDeselect = onDeselect;
        return this;
    }

    /// <summary>
    /// Restricts allowed characters to match certain types of content (such as numbers, email addresses, passwords, etc.)
    /// </summary>
    /// <param name="contentType">Content type to apply to this text box.</param>
    public TextBoxComponent WithContentType(TMP_InputField.ContentType contentType)
    {
        inputField.contentType = contentType;
        return this;
    }

    /// <summary>
    /// Configures whether or not this textbox can support multiple lines of text.
    /// </summary>
    /// <param name="lineType">Line type to apply to this text box.</param>
    public TextBoxComponent WithLineType(TMP_InputField.LineType lineType)
    {
        inputField.lineType = lineType;
        return this;
    }

    /// <summary>
    /// Sets the maximum character length for this textbox.
    /// </summary>
    /// <param name="characterLength">Maximum character length.</param>
    public TextBoxComponent WithMaximumLength(int characterLength)
    {
        inputField.characterLimit = characterLength;
        return this;
    }

    /// <summary>
    /// Assigns a localized initial value.
    /// </summary>
    /// <param name="table">Table which holds the localized text</param>
    /// <param name="key">Key for the localized text</param>
    /// <param name="args">Additional arguments if string formatting is involved.</param>
    public TextBoxComponent WithInitialValue(string table, string key, params object[] args)
    {
        var str = LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        return this.WithInitialValue(str);
    }

    private void Start()
    {
        OnValueUpdated(Value);
        inputField.onValueChanged.AddListener(InputFieldValueChanged);
        inputField.onEndEdit.AddListener(InputFieldEndEdit);
        inputField.onSelect.AddListener(InputFieldSelect);
        inputField.onDeselect.AddListener(InputFieldDeselect);
    }

    private void InputFieldValueChanged(string res) => Value = res;

    private void InputFieldEndEdit(string res) => onEndEdit?.Invoke(res);

    private void InputFieldSelect(string res) => onSelect?.Invoke(res);

    private void InputFieldDeselect(string res) => onDeselect?.Invoke(res);

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveAllListeners();
        inputField.onEndEdit.RemoveAllListeners();
        inputField.onSelect.RemoveAllListeners();
        inputField.onDeselect.RemoveAllListeners();
    }

    protected override void OnValueUpdated(string updatedValue) => inputField.SetTextWithoutNotify(updatedValue);
}
