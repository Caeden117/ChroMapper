using System;
using UnityEngine;

/// <summary>
/// Generic CMUI Component that handles type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// Type handled by this CMUI Component. Usually this is a primitive type, but it can be anything.
/// </typeparam>
public abstract class CMUIComponent<T> : CMUIComponentBase
{
    public T Value
    {
        get => internalValue;
        set
        {
            internalValue = value;
            onValueChanged?.Invoke(internalValue);
            OnValueUpdated(internalValue);
        }
    }

    private Func<T> valueAccessor;
    private Action<T> onValueChanged;

    private T internalValue;

    internal void SetValueAccessor(Func<T> valueAccessor)
    {
        if (this.valueAccessor != null)
        {
            throw new InvalidOperationException($"{nameof(this.valueAccessor)} has already been assigned.");
        }

        this.valueAccessor = valueAccessor;
    }

    internal void SetOnValueChanged(Action<T> onValueChanged)
    {
        if (this.onValueChanged != null)
        {
            throw new InvalidOperationException($"{nameof(this.onValueChanged)} has already been assigned.");
        }

        this.onValueChanged = onValueChanged;
    }

    protected virtual void OnValueUpdated(T updatedValue) { }

    private void Awake()
    {
        if (valueAccessor != null)
        {
            internalValue = valueAccessor();
        }
        else
        {
            Debug.LogWarning("Value accessor was not assigned. Is this intentional?");
        }
    }
}

/// <summary>
/// Base, non-generic CMUI Component.
/// </summary>
public abstract class CMUIComponentBase : MonoBehaviour
{
}
