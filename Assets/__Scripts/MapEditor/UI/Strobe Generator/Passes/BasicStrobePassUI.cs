using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BasicStrobePassUI : StrobeGeneratorPassUIController
{
    [FormerlySerializedAs("EventTypes")] [SerializeField] private StrobeGeneratorEventSelector[] eventTypes;
    [SerializeField] private Toggle dynamicallyChangeTypeA;
    [SerializeField] private Toggle swapColors;
    [FormerlySerializedAs("Values")] [SerializeField] private StrobeGeneratorEventSelector values;
    [SerializeField] private TMP_InputField strobeInterval;
    [SerializeField] private TMP_Dropdown regularEventEasings;
    [SerializeField] private Toggle easingTime;
    [SerializeField] private Toggle easingFloatValue;

    // The following functions are filtered from this Pass for the following reasons:
    // "Back" results in times outside the bounds set by the user
    // "Elastic" results in times outside the bounds set by the user
    // "Bounce" visits the start and end point multiple times which causes a very weird effect that people might report as an error.
    // I do not expect many people to want to use these easing types at all, so I think I am safe if I just filter them entirely.
    private readonly string[] filteredEasings = { "Back", "Elastic", "Bounce" };

    private new void Start()
    {
        base.Start();
        regularEventEasings.ClearOptions();
        regularEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys
            .Where(x => !filteredEasings.Any(y => x.Contains(y))).ToList());
        regularEventEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        var values = new List<int>();
        foreach (var selector in eventTypes) values.Add(GetTypeFromEventIds(selector.SelectedNum, this.values.SelectedNum));
        var precision = float.Parse(strobeInterval.text);
        var internalName = Easing.DisplayNameToInternalName[regularEventEasings.captionText.text];
        return new StrobeLightingPass(values, swapColors.isOn, dynamicallyChangeTypeA.isOn, precision, internalName, easingTime.isOn, easingFloatValue.isOn);
    }

    private int GetTypeFromEventIds(int eventValue, int eventColor)
    {
        return eventValue switch
        {
            0 => (int)LightValue.Off,
            1 => eventColor == 0 ? (int)LightValue.RedOn : (int)LightValue.BlueOn,
            2 => eventColor == 0 ? (int)LightValue.RedFlash : (int)LightValue.BlueFlash,
            3 => eventColor == 0 ? (int)LightValue.RedFade : (int)LightValue.BlueFade,
            4 => eventColor == 0 ? (int)LightValue.RedTransition : (int)LightValue.BlueTransition,
            _ => -1,
        };
    }
}
