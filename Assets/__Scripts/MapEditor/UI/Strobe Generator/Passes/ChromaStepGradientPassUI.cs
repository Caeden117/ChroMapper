using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = System.Random;

public class ChromaStepGradientPassUI : StrobeGeneratorPassUIController
{
    [SerializeField] private StrobeGeneratorEventSelector EventType;
    [SerializeField] private StrobeGeneratorEventSelector Values;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private TMP_InputField strobeInterval;
    [SerializeField] private TMP_Dropdown chromaEventEasings;

    private static readonly Random Rand = new Random();
    private static bool _flicker = false;
    private readonly Dictionary<string, Func<float, float>> _extraEasings = new Dictionary<string, Func<float, float>>()
    {
        { "Random", f => (float) Rand.NextDouble() },
        { "Flicker", f =>
            {
                _flicker = f != 0 && !_flicker;
                return _flicker ? 1 : 0;
            }
        }
    };

    private new void Start()
    {
        base.Start();
        chromaEventEasings.ClearOptions();
        chromaEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.ToList());
        chromaEventEasings.AddOptions(_extraEasings.Keys.ToList());
        chromaEventEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        var picked = chromaEventEasings.captionText.text;
        var easing = _extraEasings.ContainsKey(picked) ? _extraEasings[picked] : Easing.named(Easing.DisplayNameToInternalName[picked]);
        return new StrobeStepGradientPass(
            GetTypeFromEventIDS(EventType.SelectedNum, Values.SelectedNum),
            swapColors.isOn,
            float.Parse(strobeInterval.text),
            easing
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
