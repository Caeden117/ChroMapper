using System;
using TMPro;
using UnityEngine;

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
