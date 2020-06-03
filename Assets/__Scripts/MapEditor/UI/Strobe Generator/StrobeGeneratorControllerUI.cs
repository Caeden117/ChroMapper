using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class StrobeGeneratorControllerUI : MonoBehaviour
{
    [SerializeField] private StrobeGeneratorEventSelector[] EventTypes;
    [SerializeField] private Toggle placeRegularEvents;
    [SerializeField] private Toggle placeChromaEvents;
    [SerializeField] private Toggle dynamicallyChangeTypeA;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private StrobeGeneratorEventSelector Values;
    [SerializeField] private StrobeGenerator strobeGen;
    [SerializeField] private StrobeGeneratorBeatSliderUI strobeInterval;
    [SerializeField] private TMP_Dropdown easingDropdown;

    private void Start()
    {
        easingDropdown.ClearOptions();
        easingDropdown.AddOptions(Easing.byName.Keys.ToList());
        easingDropdown.value = 0;
    }

    public void GenerateStrobeWithUISettings()
    {
        /*PersistentUI.Instance.ShowDialogBox("<u><b>Strobe Generator settings:</b></u>\n\n" +
            $"Will alternate between {TextForEventValueID(A.SelectedNum)} and {TextForEventValueID(B.SelectedNum)}\n\n" +
            $"{(placeRegularEvents.isOn ? "Will place vanilla events" : "Will not place vanilla events")}\n\n" + 
            $"{(placeChromaEvents.isOn ? "Will place Chroma RGB events" : "Will not place Chroma RGB events")}\n\n" +
            $"{(dynamicallyChangeTypeA.isOn ? "Will dynamically change Type A according to conflicting events" : "Conflicting events will not have impact on the strobe")}\n\n" +
            $"{TextForEventColor(Values.SelectedNum)}\n\n" +
            "Are you sure you want to generate this strobe?",
            HandleGenerateStrobeDialog, PersistentUI.DialogBoxPresetType.YesNo);*/
        HandleGenerateStrobeDialog(0);
    }

    private void HandleGenerateStrobeDialog(int res)
    {
        if (res > 0) return;
        List<int> values = new List<int>();
        foreach (StrobeGeneratorEventSelector selector in EventTypes)
        {
            values.Add(GetTypeFromEventIDS(selector.SelectedNum, Values.SelectedNum));
        }
        strobeGen.GenerateStrobe(values, placeRegularEvents.isOn, placeChromaEvents.isOn, dynamicallyChangeTypeA.isOn, swapColors.isOn, strobeInterval.BeatPrecision, easingDropdown.captionText.text);
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

    private string TextForEventValueID(int valueID)
    {
        switch (valueID)
        {
            case 1: return "On";
            case 2: return "Flash";
            case 3: return "Fade";
            default: return "Off";
        }
    }

    private string TextForEventColor(int color)
    {
        switch (color)
        {
            case 1: return "Will place blue events";
            case 2: return "Will alternate between red and blue events when necessary";
            default: return "Will place red events";
        }
    }
}
