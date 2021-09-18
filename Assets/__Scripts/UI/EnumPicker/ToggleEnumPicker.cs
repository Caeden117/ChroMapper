using System;
using UnityEngine;
using UnityEngine.UI;

public class ToggleEnumPicker : EnumPicker<Toggle>
{
    [SerializeField] private Toggle[] toggles;

    private int i = 0;

    public override void CreateOptionForEnumValue(Enum enumValue)
    {
        var toggle = toggles[i];

        items.Add(enumValue, toggle);

        var colorBlock = toggle.colors;
        colorBlock.normalColor = normalColor;
        colorBlock.selectedColor = colorBlock.highlightedColor = colorBlock.pressedColor = selectedColor;
        toggle.colors = colorBlock;

        toggle.onValueChanged.AddListener((x) =>
        {
            if (Locked || !toggle.isOn)
                return;

            Select(toggle);
            OnEnumValueSelected(enumValue);
        });

        // Poor man's for-loop
        i++;
    }

    // Enforce toggle state
    protected override void Select(Toggle selectedGraphic)
    {
        selectedGraphic.SetIsOnWithoutNotify(true);
        SetNormalColor(selectedGraphic, selectedColor);

        foreach (var toggle in toggles)
        {
            if (toggle != selectedGraphic)
            {
                toggle.SetIsOnWithoutNotify(false);
                SetNormalColor(toggle, normalColor);
            }
        }
    }

    private void SetNormalColor(Toggle toggle, Color color)
    {
        var colorBlock = toggle.colors;
        colorBlock.normalColor = color;
        toggle.colors = colorBlock;
    }
}
