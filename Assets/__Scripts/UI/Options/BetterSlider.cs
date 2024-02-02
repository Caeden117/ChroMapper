using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BetterSlider : MonoBehaviour
{
    private const float slideSpeed = 0.02f;

    [FormerlySerializedAs("showPercent")]
    [Header("Percent Settings:")]
    public bool ShowPercent;

    [FormerlySerializedAs("percentMatchesValues")]
    [Tooltip("Allows for percents that are negative and greater than 100%.")]
    public bool PercentMatchesValues;

    [FormerlySerializedAs("multipleOffset")] public float MultipleOffset = 10;
    [FormerlySerializedAs("power")] public bool Power;

    [FormerlySerializedAs("showValue")]
    [Header("Value Settings:")]
    public bool ShowValue;

    [FormerlySerializedAs("decimalPlaces")][Header("\n")] public int DecimalPlaces;

    [FormerlySerializedAs("defaultSliderValue")]
    [Header("Other Settings")]
    [Tooltip("Must be value slider shows.")]
    public float DefaultSliderValue = 12345.12f;

    [FormerlySerializedAs("_decimalsMustMatchForDefault")] public bool DecimalsMustMatchForDefault = true;

    [FormerlySerializedAs("slider")] public Slider Slider;
    [FormerlySerializedAs("description")] public TextMeshProUGUI Description;
    [SerializeField] private Image ringImage;
    [FormerlySerializedAs("valueString")] public LocalizeStringEvent ValueString;
    [FormerlySerializedAs("valueText")] public TextMeshProUGUI ValueText;

    private Coroutine moveRingCoroutine;

    public string TextValue
    {
        get
        {
            var result = "";

            if (ShowPercent && !PercentMatchesValues)
            {
                result = ((Value + Mathf.Abs(Slider.minValue)) / (Slider.maxValue + Mathf.Abs(Slider.minValue)) * 100)
                    .ToString("F" + DecimalPlaces) + "%";
            }
            else if (PercentMatchesValues)
            {
                result =
                    (Power ? Math.Pow(MultipleOffset, Value) : Value * MultipleOffset).ToString("F" + DecimalPlaces);
            }
            else if (ShowValue)
            {
                result = Value.ToString("F" + DecimalPlaces);
            }

            if (ShowPercent) result += "%";

            return result;
        }
    }

    public float Value
    {
        get => Slider.value;
        set => Slider.value = value;
    }

    protected virtual void Start()
    {
        if (TryGetComponent<SettingsBinder>(out var settingsBinder))
        {
            Slider.onValueChanged.AddListener(OnHandleMove);

            var settingValue = Convert.ToSingle(settingsBinder.RetrieveValueFromSettings());

            Slider.SetValueWithoutNotify(settingValue);

            UpdateDisplay(false);
        }
    }

    private void OnHandleMove(float value)
    {
        moveRingCoroutine = StartCoroutine(MoveRing());
        UpdateDisplay();
    }

    protected virtual void UpdateDisplay(bool sendToSettings = true)
    {
        ValueString.StringReference.RefreshString();

        var percentOffset = PercentMatchesValues ? MultipleOffset : 1;

        if (DecimalsMustMatchForDefault)
        {
            ValueText.color = (DefaultSliderValue * percentOffset).ToString($"F{DecimalPlaces}") ==
                              (Value * percentOffset).ToString($"F{DecimalPlaces}")
                ? new Color(1f, 0.75f, 0.23f)
                : Color.white;
        }
        else
        {
            ValueText.color = (DefaultSliderValue * percentOffset).ToString("F0") ==
                              (Value * percentOffset).ToString("F0")
                ? new Color(1f, 0.75f, 0.23f)
                : Color.white;
        }

        if (sendToSettings) SendMessage("SendValueToSettings", Value);
    }

    private IEnumerator MoveRing()
    {
        if (moveRingCoroutine != null) StopCoroutine(moveRingCoroutine);
        var startTime = Time.time;

        while (true)
        {
            var ringVal = ringImage.fillAmount;
            var toBe = (Value - Slider.minValue) / (Slider.maxValue - Slider.minValue);
            ringVal = Mathf.MoveTowardsAngle(ringVal, toBe, Time.time / startTime * slideSpeed);
            ringImage.fillAmount = ringVal;
            //if (ringVal == toBe) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
