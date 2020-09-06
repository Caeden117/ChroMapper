using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;

public class StrobeGeneratorControllerUI : MonoBehaviour, CMInput.IStrobeGeneratorActions
{
    [SerializeField] private StrobeGeneratorEventSelector[] EventTypes;
    [SerializeField] private Toggle placeRegularEvents;
    [SerializeField] private Toggle placeChromaEvents;
    [SerializeField] private Toggle dynamicallyChangeTypeA;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private StrobeGeneratorEventSelector Values;
    [SerializeField] private StrobeGenerator strobeGen;
    [SerializeField] private StrobeGeneratorBeatSliderUI strobeInterval;
    [SerializeField] private TMP_Dropdown regularEventEasings;
    [SerializeField] private TMP_Dropdown chromaEventEasings;

    // The following functions are filtered for the following reasons:
    // "Back" results in times outside the bounds set by the user
    // "Elastic" results in times outside the bounds set by the user
    // "Bounce" visits the start and end point multiple times which causes a very weird effect that people might report as an error.
    // I do not expect many people to want to use these easing types at all, so I think I am safe if I just filter them entirely.
    private string[] FilteredEasings = new[] { "Back", "Elastic", "Bounce" };

    private void Start()
    {
        regularEventEasings.ClearOptions();
        regularEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.Where(x => !FilteredEasings.Any(y => x.Contains(y))).ToList());
        regularEventEasings.value = 0;

        chromaEventEasings.ClearOptions();
        chromaEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.ToList());
        chromaEventEasings.value = 0;
    }

    public void GenerateStrobeWithUISettings()
    {
        List<StrobeGeneratorPass> passes = new List<StrobeGeneratorPass>();

        if (placeRegularEvents.isOn)
        {
            List<int> values = new List<int>();
            foreach (StrobeGeneratorEventSelector selector in EventTypes)
            {
                values.Add(GetTypeFromEventIDS(selector.SelectedNum, Values.SelectedNum));
            }
            string internalName = Easing.DisplayNameToInternalName[regularEventEasings.captionText.text];
            passes.Add(new StrobeLightingPass(values, swapColors.isOn, dynamicallyChangeTypeA.isOn, strobeInterval.BeatPrecision, internalName));
        }

        if (placeChromaEvents.isOn)
        {
            string internalName = Easing.DisplayNameToInternalName[chromaEventEasings.captionText.text];
            passes.Add(new StrobeChromaPass(internalName));
        }

        strobeGen.GenerateStrobe(passes);
    }

    public void OnQuickStrobeGen(InputAction.CallbackContext context)
    {
        if (context.performed) GenerateStrobeWithUISettings();
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
