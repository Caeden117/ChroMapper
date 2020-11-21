using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChromaStepGradientPassUI : StrobeGeneratorPassUIController
{
    [SerializeField] private StrobeGeneratorEventSelector EventType;
    [SerializeField] private StrobeGeneratorEventSelector Values;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private TMP_InputField strobeInterval;

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        return new StrobeStepGradientPass(
            GetTypeFromEventIDS(EventType.SelectedNum, Values.SelectedNum),
            swapColors.isOn,
            float.Parse(strobeInterval.text)
            );
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
