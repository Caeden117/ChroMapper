using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BetterSlider : MonoBehaviour
{
    [Header("Percent Settings:")]
    [SerializeField] public bool showPercent;
    [SerializeField, Tooltip("Allows for percents that are negative and greater than 100%.")] public bool percentMatchesValues;
    [SerializeField] public float multipleOffset = 10;
    
    [Header("Value Settings:")]
    [SerializeField] public bool showValue;
    
    [Header("\n")]
    [SerializeField] public int decimalPlaces;

    [SerializeField, Header("Other Settings"), Tooltip("Must be value slider shows.")] public float defaultSliderValue = 12345.12f;
    [SerializeField] public bool _decimalsMustMatchForDefault = true;
    [SerializeField] public bool _endTextEnabled;
    [SerializeField] public string _endText = "";
    
    [SerializeField] public Slider slider;
    [SerializeField] public TextMeshProUGUI description;
    [SerializeField] private Image ringImage;
    [SerializeField] public TextMeshProUGUI valueText;

    public float value
    {
        get => slider.value;
        set => slider.value = value;
    }

    private void Awake()
    {
        slider.onValueChanged.AddListener(OnHandleMove);
    }

    private void Start()
    {
        OnHandleMove(-0f);
    }

    private Coroutine _moveRingCoroutine;

    private void OnHandleMove(float value)
    {
        _moveRingCoroutine = StartCoroutine(MoveRing());
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (showPercent && !percentMatchesValues) valueText.text = ((value + Mathf.Abs(slider.minValue)) / (slider.maxValue + Mathf.Abs(slider.minValue)) * 100).ToString("F" + decimalPlaces) + "%";
        else if (percentMatchesValues) valueText.text = (value*multipleOffset).ToString("F" + decimalPlaces);
        else if (showValue) valueText.text = value.ToString("F" + decimalPlaces);

        if (_endTextEnabled) valueText.text += _endText;
        else if (showPercent) valueText.text += "%";
        
        if(_decimalsMustMatchForDefault)
        valueText.color = (defaultSliderValue == value) ? new Color(1f, 0.75f, 0.23f) : Color.white;
        else valueText.color = (defaultSliderValue.ToString("F0") == value.ToString("F0")) ? new Color(1f, 0.75f, 0.23f) : Color.white;
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
