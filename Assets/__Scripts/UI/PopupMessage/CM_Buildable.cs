using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base Buildable class for CM Pop Up messages.
/// </summary>
public abstract class CM_Buildable : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;

    public void AssignText(string text)
    {
        label.text = text;
    }
}

public abstract class CM_Buildable<T> : CM_Buildable where T : struct, IComparable, IFormattable, IConvertible, IComparable<T>, IEquatable<T>
{
    public Action<T> Result;

    public void InvokeResult(T res)
    {
        Result?.Invoke(res);
    }
}
