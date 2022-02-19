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
    public DropdownComponent WithOptions<T>(IEnumerable<T> enumerable)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(enumerable.Select(x => x.ToString()).ToList());
        return this;
    }

    /// <summary>
    /// Populates the dropdown list with the provided list of <see cref="TMP_Dropdown.OptionData"/>.
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <returns>Itself, for chaining methods</returns>
    public DropdownComponent WithOptions(List<TMP_Dropdown.OptionData> optionData)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(optionData);
        return this;
    }

    /// <summary>
    /// Populates the dropdown list with the provided list of sprites.
    /// </summary>
    /// <typeparam name="T">Enum type</typeparam>
    /// <returns>Itself, for chaining methods</returns>
    public DropdownComponent WithOptions(List<Sprite> sprites)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(sprites);
        return this;
    }

    /// <summary>
    /// Populates the dropdown list with the names of all constants from the provided Enum type.
    /// </summary>
    /// <remarks>
    /// The <see cref="int"/> value returned by the <see cref="DropdownComponent"/> is the index into the list of names,
    /// not the enum value assigned to the name.
    /// </remarks>
    /// <typeparam name="T">Enum type</typeparam>
    /// <returns>Itself, for chaining methods</returns>
    public DropdownComponent WithOptions<T>() where T : Enum
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
