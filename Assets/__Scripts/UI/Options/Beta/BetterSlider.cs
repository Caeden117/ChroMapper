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
    
    [Header("Value Settings:")]
    [SerializeField] private bool showValue;
    
    [Header("\n")]
    [SerializeField] private int decimalPlaces;
    
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
        UpdateDisplay();
    }

    private Coroutine _moveRingCoroutine;
    public void OnHandleMove(float value)
    {
        _moveRingCoroutine = StartCoroutine(MoveRing());
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (showPercent) _valueText.text = ((_slider.value + Mathf.Abs(_slider.minValue)) / (_slider.maxValue + Mathf.Abs(_slider.minValue)) * 100).ToString("F" + decimalPlaces) + "%";
        else if (showValue) _valueText.text = _slider.value.ToString("F" + decimalPlaces);
    }
    
    private const float SLIDE_SPEED = 0.02f;
    
    private IEnumerator MoveRing()
    {
        if(_moveRingCoroutine != null) StopCoroutine(_moveRingCoroutine);
        float startTime = Time.time;
        
        while (true)
        {
            float ringVal = _ringImage.fillAmount;
            float toBe = ((_slider.value + Mathf.Abs(_slider.minValue)) / (_slider.maxValue + Mathf.Abs(_slider.minValue)));
            ringVal = Mathf.MoveTowardsAngle(ringVal, toBe, (Time.time / startTime) * SLIDE_SPEED);
            _ringImage.fillAmount = ringVal;
            if (ringVal == toBe) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
