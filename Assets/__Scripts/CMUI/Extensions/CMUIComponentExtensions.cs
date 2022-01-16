using System;

public static class CMUIComponentExtensions
{
    /// <summary>
    /// Specifies an accessor to get the initial value from.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponent{T}"/>.</typeparam>
    /// <param name="initialValue">Initial value accessor</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithInitialValue<TComponent, TValue>(this TComponent component, Func<TValue> initialValue)
        where TComponent : CMUIComponent<TValue>
    {
        component.SetValueAccessor(initialValue);
        return component;
    }

    /// <summary>
    /// Specifies a callback that is triggered when the component value has changed.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponent{T}"/>.</typeparam>
    /// <param name="onValueChanged">Callback triggered on value changed</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent OnChanged<TComponent, TValue>(this TComponent component, Action<TValue> onValueChanged)
        where TComponent : CMUIComponent<TValue>
    {
        component.SetOnValueChanged(onValueChanged);
        return component;
    }

    /// <summary>
    /// Specifies text for the label that accompanies the component.
    /// If <paramref name="labelText"/> is <c>null</c>, the label itself will be disabled.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponentWithLabel{T}"/></typeparam>
    /// <param name="labelText">Text to display, if non-null.</param>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithLabelText<TComponent, TValue>(this TComponent component, string labelText)
        where TComponent : CMUIComponentWithLabel<TValue>
    {
        component.SetLabelEnabled(string.IsNullOrWhiteSpace(labelText));
        component.SetLabelText(labelText ?? "null");
        return component;
    }

    /// <summary>
    /// Specifies that the component will have no label displayed.
    /// </summary>
    /// <typeparam name="TComponent">Inherited <see cref="CMUIComponentWithLabel{T}"/></typeparam>
    /// <returns>Itself, for use in chaining methods.</returns>
    public static TComponent WithNoLabelText<TComponent, TValue>(this TComponent component)
        where TComponent : CMUIComponentWithLabel<TValue>
    {
        component.SetLabelEnabled(false);
        return component;
    }
}
