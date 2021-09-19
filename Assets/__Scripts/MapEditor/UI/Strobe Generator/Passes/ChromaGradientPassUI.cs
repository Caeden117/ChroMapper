﻿using System.Linq;
using TMPro;
using UnityEngine;

public class ChromaGradientPassUI : StrobeGeneratorPassUIController
{
    [SerializeField] private TMP_Dropdown chromaEventEasings;

    private new void Start()
    {
        base.Start();
        chromaEventEasings.ClearOptions();
        chromaEventEasings.AddOptions(Easing.DisplayNameToInternalName.Keys.ToList());
        chromaEventEasings.value = 0;
    }

    public override StrobeGeneratorPass GetPassForGeneration()
    {
        var internalName = Easing.DisplayNameToInternalName[chromaEventEasings.captionText.text];
        return new StrobeChromaPass(internalName);
    }
}
