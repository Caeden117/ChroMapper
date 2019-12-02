using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StrobeGeneratorBeatSliderUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayTMP;
    [SerializeField] private int startingPrecision = 1;

    public int BeatPrecision { get; private set; } = 1;

    private void Start()
    {
        BeatPrecision = startingPrecision;
    }

    public void UpdateValue(float v)
    {
        BeatPrecision = Mathf.RoundToInt(Mathf.Pow(2, v));
        displayTMP.text = $"1/{BeatPrecision}";
    }
}
