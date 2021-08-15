using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class BetterSlider : MonoBehaviour
{
    [Header("Percent Settings:")]
    [SerializeField] public bool showPercent;
    [SerializeField, Tooltip("Allows for percents that are negative and greater than 100%.")] public bool percentMatchesValues;
    [SerializeField] public float multipleOffset = 10;
    [SerializeField] public bool power;

    [Header("Value Settings:")]
    [SerializeField] public bool showValue;
    
    [Header("\n")]
    [SerializeField] public int decimalPlaces;

    [SerializeField, Header("Other Settings"), Tooltip("Must be value slider shows.")] public float defaultSliderValue = 12345.12f;
    [SerializeField] public bool _decimalsMustMatchForDefault = true;
    
    [SerializeField] public Slider slider;
    [SerializeField] public TextMeshProUGUI description;
    [SerializeField] private Image ringImage;
    [SerializeField] public LocalizeStringEvent valueString;
    [SerializeField] public TextMeshProUGUI valueText;

    public string TextValue
    {
        get {
            var result = "";

            if (showPercent && !percentMatchesValues) result = ((value + Mathf.Abs(slider.minValue)) / (slider.maxValue + Mathf.Abs(slider.minValue)) * 100).ToString("F" + decimalPlaces) + "%";
            else if (percentMatchesValues) result = (power ? Math.Pow(multipleOffset, value) : value * multipleOffset).ToString("F" + decimalPlaces);
            else if (showValue) result = value.ToString("F" + decimalPlaces);

            if (showPercent) result += "%";

            return result;
        }
    }

    public float value
    {
        get => slider.value;
        set => slider.value = value;
    }

    private void Awake()
    {
    }

    protected virtual void Start()
    {
        slider.onValueChanged.AddListener(OnHandleMove);

        var settingValue = Convert.ToSingle(GetComponent<SettingsBinder>()?.RetrieveValueFromSettings());

        slider.SetValueWithoutNotify(settingValue);
        
        UpdateDisplay(false);
    }

    private Coroutine _moveRingCoroutine;

    private void OnHandleMove(float value)
    {
        _moveRingCoroutine = StartCoroutine(MoveRing());
        UpdateDisplay();
    }

    protected virtual void UpdateDisplay(bool sendToSettings = true)
    {
        valueString.StringReference.RefreshString();
        
        if(_decimalsMustMatchForDefault)
        valueText.color = (defaultSliderValue == value) ? new Color(1f, 0.75f, 0.23f) : Color.white;
        else valueText.color = (defaultSliderValue.ToString("F0") == value.ToString("F0")) ? new Color(1f, 0.75f, 0.23f) : Color.white;

        if (sendToSettings) SendMessage("SendValueToSettings", value);
    }
    
    private const float SLIDE_SPEED = 0.02f;
    
    private IEnumerator MoveRing()
    {
        if(_moveRingCoroutine != null) StopCoroutine(_moveRingCoroutine);
        float startTime = Time.time;
        
        while (true)
        {
            float ringVal = ringImage.fillAmount;
            float toBe = (value - slider.minValue) / (slider.maxValue - slider.minValue);
            ringVal = Mathf.MoveTowardsAngle(ringVal, toBe, (Time.time / startTime) * SLIDE_SPEED);
            ringImage.fillAmount = ringVal;
            //if (ringVal == toBe) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
