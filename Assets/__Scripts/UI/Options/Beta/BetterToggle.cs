using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BetterToggle : UIBehaviour, IPointerExitHandler, IPointerEnterHandler, IPointerClickHandler
{
    public TextMeshProUGUI description;
    public Image background;
    public RectTransform switchTransform;

    public bool IsOn;

    private readonly Vector3 offPos = new Vector3(-35, 0, 0);//No idea why these are these numbers.
    private readonly Vector3 onPos = new Vector3(-15, 0, 0);

    [HideInInspector] public Color OnColor; 
    [HideInInspector] public Color OffColor;

    private Coroutine _slideButtonCoroutine = null;
    private Coroutine _slideColorCoroutine = null;

    protected override void Start()
    {
        
    }

    // Update is called once per frame
    public void Update()
    {
        /*if (IsOn)
                 {
                     switchTransform.localPosition = onPos;
                     background.color = OnColor;
                 }
                 else
                 {
                     switchTransform.localPosition = offPos;
                     background.color = OffColor;
                 }*/
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("bbb");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("aaa");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        IsOn = !IsOn;
        _slideButtonCoroutine = StartCoroutine(SlideToggle());
        _slideColorCoroutine = StartCoroutine(SlideColor());
    }

    private readonly float _slideSpeed = 0.2f;
    
    private IEnumerator SlideToggle()
    {
        if(_slideButtonCoroutine != null) StopCoroutine(_slideButtonCoroutine);
        
        float startTime = Time.time;
        
        while (true)
        {
            Vector3 localPosition = switchTransform.localPosition;
            localPosition = Vector3.Lerp(localPosition, IsOn ? onPos : offPos, (Time.time / startTime) * _slideSpeed);
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
            color = Color.Lerp(color, IsOn ? OnColor : OffColor, (Time.time / startTime) * _slideSpeed);
            background.color = color;
            if (background.color == OnColor || background.color == OffColor) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
