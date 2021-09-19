using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class EnumPicker : MonoBehaviour
{
    public event Action<Enum> OnClick;
    public bool Locked { get; set; }

    [SerializeField] internal Color normalColor = Color.white;
    [SerializeField] internal Color selectedColor = Color.white;

    public void Initialize(Type type)
    {
        var enumValues = Enum.GetValues(type);

        foreach (Enum enumValue in enumValues)
        {
            CreateOptionForEnumValue(enumValue);
        }

        Select(enumValues.GetValue(0) as Enum);
    }

    public abstract void CreateOptionForEnumValue(Enum enumValue);

    public abstract void Select(Enum enumValue);

    protected void OnEnumValueSelected(Enum enumValue) => OnClick?.Invoke(enumValue);

    protected static PickerChoiceAttribute GetPickerChoice(Enum GenericEnum)
    {
        var genericEnumType = GenericEnum.GetType();
        var memberInfo = genericEnumType.GetMember(GenericEnum.ToString());

        if (memberInfo != null && memberInfo.Length > 0)
        {
            var attributes = memberInfo[0].GetCustomAttributes(typeof(PickerChoiceAttribute), false);
            if (attributes != null && attributes.Count() > 0)
            {
                return (PickerChoiceAttribute)attributes.ElementAt(0);
            }
        }
        return null;
    }
}

public abstract class EnumPicker<TGraphic> : EnumPicker where TGraphic : UIBehaviour
{
    internal Dictionary<Enum, TGraphic> items = new Dictionary<Enum, TGraphic>();

    public override void Select(Enum enumValue) => Select(items[enumValue]);

    protected abstract void Select(TGraphic selectedGraphic);
}
