using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BetterSlider : MonoBehaviour
{

    private Slider _slider;
    private RectTransform _ringTransform;
    private Image _ringImage;

    private Coroutine _moveRingCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        _slider = GetComponentInChildren<Slider>();
        _ringImage = _slider.GetComponentsInChildren<Image>().First(i => i.name == "Ring");
        _ringTransform = _ringImage.rectTransform;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnHandleMove(float value)
    {
        StartCoroutine(MoveRing());
    }
    
    private const float SLIDE_SPEED = 0.01f;
    
    private IEnumerator MoveRing()
    {
        if(_moveRingCoroutine != null) StopCoroutine(_moveRingCoroutine);
        
        float startTime = Time.time;
        
        while (true)
        {
            float ringVal = _ringImage.fillAmount;
            float toBe = _slider.value / _slider.maxValue;
            ringVal = Mathf.Lerp(ringVal, toBe, (Time.time / startTime) * SLIDE_SPEED);
            _ringImage.fillAmount = ringVal;
            if (ringVal == toBe) break;
            yield return new WaitForFixedUpdate();
        }
    }
    
    
}
