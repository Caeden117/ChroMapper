using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Dropdown Component that handles an integer, whether that be the index into a list of options,
/// or the numerical representation of an enum.
/// </summary>
public class DropdownComponent : CMUIComponentWithLabel<int>
{
    [SerializeField] private TMP_Dropdown dropdown;

    /// <summary>
    /// Populates the dropdown list with the provided values.
    /// </summary>
    /// <remarks>
    /// All options will have <see cref="object.ToString()"/> called before being displayed.
    /// </remarks>
    /// <param name="enumerable">List of options</param>
    /// <returns>Itself, for chaining methods.</returns>
    public DropdownComponent WithOptionsList<T>(IEnumerable<T> enumerable)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(enumerable.Select(x => x.ToString()).ToList());
        return this;
    }

    /// <summary>
    /// Populates the dropdown list with the names of all constants from the provided Enum type.
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <returns>Itself, for chaining methods</returns>
    public DropdownComponent WithEnumValues<T>() where T : Enum
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(Enum.GetNames(typeof(T)).ToList());
        return this;
    }

    protected override void OnValueUpdated(int updatedValue) => dropdown.SetValueWithoutNotify(updatedValue);

    private void Start() => dropdown.onValueChanged.AddListener(DropdownValueChanged);        

    private void DropdownValueChanged(int value) => Value = value;

    private void OnDestroy() => dropdown.onValueChanged.RemoveAllListeners();
}
