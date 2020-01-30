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
    [SerializeField] private bool showPercent;
    [SerializeField, Tooltip("Allows for percents that are negative and greater than 100%.")] private bool percentMatchesValues;
    [SerializeField] private float multipleOffset = 10;
    
    [Header("Value Settings:")]
    [SerializeField] private bool showValue;
    
    [Header("\n")]
    [SerializeField] private int decimalPlaces;

    [SerializeField, Header("Other Settings"), Tooltip("Must be value slider shows.")] private float defaultSliderValue = 12345.12f;
    [SerializeField] private bool _decimalsMustMatchForDefault = true;
    [SerializeField] private bool _endTextEnabled;
    [SerializeField] private string _endText = "";
    
    private Slider _slider;
    private Image _ringImage;
    private TextMeshProUGUI _valueText;

    private void Awake()
    {
        _slider = GetComponentInChildren<Slider>();
        _ringImage = gameObject.GetComponentsInChildren<Image>().First(i => i.name == "Ring");
        _valueText = gameObject.GetComponentsInChildren<TextMeshProUGUI>().First(t => t.name == "Value");
    }

    private void Start()
    {
        _slider.onValueChanged.AddListener(OnHandleMove);
    }

    public void Set(float value) //ONLY USE FOR SETUP
    {
        StartCoroutine(Setup(value));
    }

    private IEnumerator Setup(float value)
    {
        int i = 0;
        while (i<100) //there has to be a better way doing this
        {
            i++;
            yield return new WaitForEndOfFrame();
        }

        _slider.value = value;
        OnHandleMove(value);
    }
    
    
    private Coroutine _moveRingCoroutine;

    private void OnHandleMove(float value)
    {
        _moveRingCoroutine = StartCoroutine(MoveRing());
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (showPercent && !percentMatchesValues) _valueText.text = ((_slider.value + Mathf.Abs(_slider.minValue)) / (_slider.maxValue + Mathf.Abs(_slider.minValue)) * 100).ToString("F" + decimalPlaces) + "%";
        else if (percentMatchesValues) _valueText.text = (_slider.value*multipleOffset).ToString("F" + decimalPlaces);
        else if (showValue) _valueText.text = _slider.value.ToString("F" + decimalPlaces);

        if (_endTextEnabled) _valueText.text += _endText;
        else if (showPercent) _valueText.text += "%";
        
        if(_decimalsMustMatchForDefault)
        _valueText.color = (defaultSliderValue == _slider.value) ? new Color(1f, 0.75f, 0.23f) : Color.white;
        else _valueText.color = (defaultSliderValue.ToString("F0") == _slider.value.ToString("F0")) ? new Color(1f, 0.75f, 0.23f) : Color.white;
    }
    
    private const float SLIDE_SPEED = 0.02f;
    
    private IEnumerator MoveRing()
    {
        if(_moveRingCoroutine != null) StopCoroutine(_moveRingCoroutine);
        float startTime = Time.time;
        
        while (true)
        {
            float ringVal = _ringImage.fillAmount;
            float toBe = (_slider.value - _slider.minValue) / (_slider.maxValue - _slider.minValue);
            ringVal = Mathf.MoveTowardsAngle(ringVal, toBe, (Time.time / startTime) * SLIDE_SPEED);
            _ringImage.fillAmount = ringVal;
            if (ringVal == toBe) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
