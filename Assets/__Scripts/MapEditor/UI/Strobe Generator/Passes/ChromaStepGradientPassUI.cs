﻿using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = System.Random;

public class ChromaStepGradientPassUI : StrobeGeneratorPassUIController
{
    private static readonly Random rand = new Random();
    private static bool flicker;
    [FormerlySerializedAs("EventType")] [SerializeField] private StrobeGeneratorEventSelector eventType;
    [FormerlySerializedAs("Values")] [SerializeField] private StrobeGeneratorEventSelector values;
    [SerializeField] private Toggle swapColors;
    [SerializeField] private TMP_InputField strobeInterval;
    [SerializeField] private TMP_Dropdown chromaEventEasings;

    private readonly Dictionary<string, Func<float, float>> extraEasings = new Dictionary<string, Func<float, float>>
    {
        {"Random", f => (float)rand.NextDouble()},
        {
            "Flicker", f =>
            {
                flicker = f != 0 && !flicker;
                return flicker ? 1 : 0;
            }
        }
    };

    private new void Start()
    {
        base.Start();
        chromaEventEasings.ClearOptions();
        chromaEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.ToList());
        chromaEventEasings.AddOptions(extraEasings.Keys.ToList());
        chromaEventEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        var picked = chromaEventEasings.captionText.text;
        var easing = extraEasings.ContainsKey(picked)
            ? extraEasings[picked]
            : Easing.Named(Easing.DisplayNameToInternalName[picked]);
        return new StrobeStepGradientPass(
            GetTypeFromEventIds(eventType.SelectedNum, values.SelectedNum),
            swapColors.isOn,
            float.Parse(strobeInterval.text),
            easing
        );
    }

    private int GetTypeFromEventIds(int eventValue, int eventColor)
    {
        return eventValue switch
        {
            0 => MapEvent.LightValueOff,
            1 => eventColor == 0 ? MapEvent.LightValueRedON : MapEvent.LightValueBlueON,
            2 => eventColor == 0 ? MapEvent.LightValueRedFlash : MapEvent.LightValueBlueFlash,
            3 => eventColor == 0 ? MapEvent.LightValueRedFade : MapEvent.LightValueBlueFade,
            _ => -1,
        };
    }
}
