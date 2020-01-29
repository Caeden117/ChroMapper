using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BetterToggle : UIBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI description;
    public Image background;
    public RectTransform switchTransform;

    public bool isOn;

    private readonly Vector3 offPos = new Vector3(-35, 0, 0); //No idea why these are the numbers.
    private readonly Vector3 onPos = new Vector3(-15, 0, 0);

    [HideInInspector] public Color OnColor; 
    [HideInInspector] public Color OffColor;

    private Coroutine _slideButtonCoroutine;
    private Coroutine _slideColorCoroutine;

    [SerializeField] private bool defaultValue;
    [SerializeField] private Image switchImage;

    public ToggleEvent onValueChanged = new ToggleEvent();

    public void OnPointerClick(PointerEventData eventData)
    {
        isOn = !isOn;
        _slideButtonCoroutine = StartCoroutine(SlideToggle());
        _slideColorCoroutine = StartCoroutine(SlideColor());
        //switchImage.color = isOn == defaultValue ? new Color(0.2f, 1f, 0f) : Color.white;
        onValueChanged?.Invoke(isOn);
    }

    public void Set(bool b) //ONLY USE FOR SETUP
    {
        StartCoroutine(Setup(b));
    }

    private IEnumerator Setup(bool b)
    {
        int i = 0;
        while (i<50) //there has to be a better way doing this
        {
            i++;
            yield return new WaitForEndOfFrame();
        }
        isOn = b;
        //switchImage.color = isOn == defaultValue ? new Color(0.2f, 1f, 0f) : Color.white;
        _slideButtonCoroutine = StartCoroutine(SlideToggle());
        _slideColorCoroutine = StartCoroutine(SlideColor());
    }

    private const float SLIDE_SPEED = 0.2f;

    private IEnumerator SlideToggle()
    {
        if(_slideButtonCoroutine != null) StopCoroutine(_slideButtonCoroutine);
        
        float startTime = Time.time;
        
        while (true)
        {
            Vector3 localPosition = switchTransform.localPosition;
            localPosition = Vector3.Lerp(localPosition, isOn ? onPos : offPos, (Time.time / startTime) * SLIDE_SPEED);
            switchTransform.localPosition = localPosition;
            if (switchTransform.localPosition == onPos || switchTransform.localPosition == offPos) break;
            yield return new WaitForFixedUpdate();
        }
    }
    private IEnumerator SlideColor()
    {
        if(_slideColorCoroutine != null) StopCoroutine(_slideColorCoroutine);
        
        float startTime = Time.time;

        while (true)
        {
            Color color = background.color;
            color = Color.Lerp(color, isOn ? OnColor : OffColor, (Time.time / startTime) * SLIDE_SPEED);
            background.color = color;
            if (background.color == OnColor || background.color == OffColor) break;
            yield return new WaitForFixedUpdate();
        }
    }

    [Serializable]
    public class ToggleEvent : UnityEvent<bool> {}
}
