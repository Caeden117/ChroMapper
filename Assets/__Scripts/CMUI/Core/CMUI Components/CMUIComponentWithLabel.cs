using TMPro;
using UnityEngine;

/// <summary>
/// Generic CMUI component with an included label.
/// </summary>
/// <typeparam name="T">Type being handled by this component.</typeparam>
public abstract class CMUIComponentWithLabel<T> : CMUIComponent<T>
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject labelContainer;

    /// <summary>
    /// Enables or disables the label.
    /// </summary>
    /// <remarks>
    /// To chain this with other methods,
    /// use <see cref="CMUIComponentExtensions.WithLabelText{TComponent, TValue}(TComponent, string?)"/>.
    /// </remarks>
    public void SetLabelEnabled(bool enabled) => labelContainer.SetActive(enabled);

    /// <summary>
    /// Sets the text for the component label.
    /// </summary>
    /// <remarks>
    /// To chain this with other methods,
    /// use <see cref="CMUIComponentExtensions.WithLabelText{TComponent, TValue}(TComponent, string?)"/>.
    /// </remarks>
    public void SetLabelText(string text) => labelText.text = text;
}
