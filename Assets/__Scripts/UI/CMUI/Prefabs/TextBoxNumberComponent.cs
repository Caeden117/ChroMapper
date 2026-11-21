using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class TextBoxNumberComponent<T> : CMUIComponentWithLabel<T>, INavigable, IQuickSubmitComponent
{
    [SerializeField] protected TMP_InputField InputField;
    [SerializeField] public ScrollableInput ScrollableInput;

    protected Func<float> GetPrecisionValue = () => 1f;
    private Func<bool> getInvertScroll = () => false;

    private Action<T> onEndEdit;
    private Action<bool, T> onEditingValueChanged;
    private Action<T> onSelect;
    private Action<T> onDeselect;

    [field: SerializeField] public Selectable Selectable { get; set; }

    public override T Value
    {
        get => base.Value;
        set
        {
            if (InputField.isFocused) return;
            base.Value = value;
        }
    }

    public enum NumberClamping
    {
        None,
        Min,
        Max,
        Clamp
    }

    [Header("Input Validation")] public NumberClamping Clamping;
    public T MinValue;
    public T MaxValue;

    public bool LoopAround;
    public T LoopThreshold;

    /// <summary>
    /// Assigns a callback when the user deselects the textbox after making changes.
    /// </summary>
    public TextBoxNumberComponent<T> OnEndEdit(Action<T> onEndEdit)
    {
        this.onEndEdit = onEndEdit;
        return this;
    }

    /// <summary>
    /// Reports whether focused text is valid and its parsed value without committing the edit.
    /// </summary>
    public TextBoxNumberComponent<T> OnEditingValueChanged(Action<bool, T> onEditingValueChanged)
    {
        this.onEditingValueChanged = onEditingValueChanged;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user selects text.
    /// </summary>
    public TextBoxNumberComponent<T> OnSelect(Action<T> onSelect)
    {
        this.onSelect = onSelect;
        return this;
    }

    /// <summary>
    /// Assigns a callback when the user deselects text.
    /// </summary>
    public TextBoxNumberComponent<T> OnDeselect(Action<T> onDeselect)
    {
        this.onDeselect = onDeselect;
        return this;
    }

    /// <summary>
    /// Restricts allowed characters to match certain types of content (such as numbers, email addresses, passwords, etc.)
    /// </summary>
    /// <param name="contentType">Content type to apply to this text box.</param>
    public TextBoxNumberComponent<T> WithContentType(TMP_InputField.ContentType contentType)
    {
        InputField.contentType = contentType;
        return this;
    }

    /// <summary>
    /// Configures whether or not this textbox can support multiple lines of text.
    /// </summary>
    /// <param name="lineType">Line type to apply to this text box.</param>
    public TextBoxNumberComponent<T> WithLineType(TMP_InputField.LineType lineType)
    {
        InputField.lineType = lineType;
        return this;
    }

    /// <summary>
    /// Sets the maximum character length for this textbox.
    /// </summary>
    /// <param name="characterLength">Maximum character length.</param>
    public TextBoxNumberComponent<T> WithMaximumLength(int characterLength)
    {
        InputField.characterLimit = characterLength;
        return this;
    }

    /// <summary>
    /// Assigns an initial value.
    /// </summary>
    /// <param name="value"></param>
    public TextBoxNumberComponent<T> WithInitialValue(float value)
    {
        InputField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    /// <summary>
    /// Assigns a scroll precision delegate
    /// </summary>
    /// <param name="fn"></param>
    public TextBoxNumberComponent<T> WithScrollPrecision(Func<float> fn)
    {
        GetPrecisionValue = fn;
        return this;
    }

    /// <summary>
    /// Assigns a scroll precision delegate
    /// </summary>
    /// <param name="fn"></param>
    public TextBoxNumberComponent<T> WithInvertScroll(Func<bool> fn)
    {
        getInvertScroll = fn;
        return this;
    }

    private void Start()
    {
        OnValueUpdated(Value);
        InputField.onValueChanged.AddListener(InputFieldValueChanged);
        InputField.onEndEdit.AddListener(InputFieldEndEdit);
        InputField.onSelect.AddListener(InputFieldSelect);
        InputField.onDeselect.AddListener(InputFieldDeselect);
        ScrollableInput.OnScrolled += HandleOnScrolled;
    }

    private void OnDestroy()
    {
        InputField.onValueChanged.RemoveAllListeners();
        InputField.onEndEdit.RemoveAllListeners();
        InputField.onSelect.RemoveAllListeners();
        InputField.onDeselect.RemoveAllListeners();
        ScrollableInput.OnScrolled -= HandleOnScrolled;
    }

    private void InputFieldValueChanged(string res)
    {
        var valid = ParseAndValidate(res, out var val);
        if (InputField.isFocused)
        {
            onEditingValueChanged?.Invoke(valid, val);
        }
        if (valid)
        {
            Value = val;
        }
    }

    private void InputFieldEndEdit(string res)
    {
        if (!ParseAndValidate(res, out var val))
        {
            Value = Value;
            return;
        }

        onEndEdit?.Invoke(val);
        Value = val;
    }

    private void InputFieldSelect(string res)
    {
        if (ParseAndValidate(res, out var val)) onSelect?.Invoke(val);
    }

    private void InputFieldDeselect(string res)
    {
        if (ParseAndValidate(res, out var val)) onDeselect?.Invoke(val);
    }

    private void HandleOnScrolled(Vector2 delta)
    {
        if (ParseAndValidate(InputField.text, out var val))
            Value = AddValue(val, getInvertScroll() ? -Mathf.Sign(delta.y) : Mathf.Sign(delta.y));
    }

    protected abstract bool ParseAndValidate(string res, out T val);
    protected abstract T AddValue(T val, float delta);
}
