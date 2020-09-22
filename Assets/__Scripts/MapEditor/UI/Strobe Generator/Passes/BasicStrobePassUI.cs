using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BasicStrobePassUI : StrobeGeneratorPassUIController
{
    [SerializeField] private StrobeGeneratorEventSelector[] EventTypes;
    [SerializeField] private Toggle dynamicallyChangeTypeA;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private StrobeGeneratorEventSelector Values;
    [SerializeField] private TMP_InputField strobeInterval;
    [SerializeField] private TMP_Dropdown regularEventEasings;

    // The following functions are filtered from this Pass for the following reasons:
    // "Back" results in times outside the bounds set by the user
    // "Elastic" results in times outside the bounds set by the user
    // "Bounce" visits the start and end point multiple times which causes a very weird effect that people might report as an error.
    // I do not expect many people to want to use these easing types at all, so I think I am safe if I just filter them entirely.
    private string[] FilteredEasings = new[] { "Back", "Elastic", "Bounce" };

    private new void Start()
    {
        base.Start();
        regularEventEasings.ClearOptions();
        regularEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.Where(x => !FilteredEasings.Any(y => x.Contains(y))).ToList());
        regularEventEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        List<int> values = new List<int>();
        foreach (StrobeGeneratorEventSelector selector in EventTypes)
        {
            values.Add(GetTypeFromEventIDS(selector.SelectedNum, Values.SelectedNum));
        }
        float precision = float.Parse(strobeInterval.text);
        string internalName = Easing.DisplayNameToInternalName[regularEventEasings.captionText.text];
        return new StrobeLightingPass(values, swapColors.isOn, dynamicallyChangeTypeA.isOn, precision, internalName);
    }

    private int GetTypeFromEventIDS(int eventValue, int eventColor)
    {
        switch (eventValue)
        {
            case 0: return MapEvent.LIGHT_VALUE_OFF;
            case 1: return eventColor == 0 ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON;
            case 2: return eventColor == 0 ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH;
            case 3: return eventColor == 0 ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE;
            default: return -1;
        }
    }
}
