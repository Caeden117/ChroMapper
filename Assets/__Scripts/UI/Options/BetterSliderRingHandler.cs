using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

public class BetterSliderRingHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    
    private RectTransform _ringTransform;
    private Image _ringImage;

    private void Start()
    {
        _ringImage = GetComponentsInChildren<Image>().First(i => i.name == "Ring");
        _ringTransform = _ringImage.rectTransform;
    }

    private Coroutine _onHandleCoroutine;
    public void OnPointerEnter(PointerEventData eventData)
    {
        _onHandleCoroutine = StartCoroutine(ScaleRing(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _onHandleCoroutine = StartCoroutine(ScaleRing(false));
    }
    
    private readonly Vector3 _ringHidden = new Vector3(0, 0, 1);
    private readonly Vector3 _ringVisible = new Vector3(2.5f, 2.5f, 1);
    
    private IEnumerator ScaleRing(bool grow)
    {
        if(_onHandleCoroutine != null) StopCoroutine(_onHandleCoroutine);
        
        float startTime = Time.time;
        
        while (true)
        {
            Vector3 ringSize = _ringTransform.localScale;
            
            if(grow) try{PersistentUI.Instance.HideTooltip();} catch (NullReferenceException){}
            else if(ringSize.x == _ringVisible.x) try{PersistentUI.Instance.ShowTooltip();} catch (NullReferenceException){}
            Vector3 toBe = grow ? _ringVisible : _ringHidden;
            
            ringSize = Vector3.MoveTowards(ringSize, toBe, (Time.time / startTime) * 0.1f);
            _ringTransform.localScale = ringSize;
            
            if (ringSize.x == toBe.x) break;
            yield return new WaitForFixedUpdate();
        }
    }
}
