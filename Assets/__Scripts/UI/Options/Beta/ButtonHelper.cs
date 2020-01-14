using System;
using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHelper : UIBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    private TabManager _tabManager;
    [HideInInspector] public int tabId;
    [HideInInspector] public bool selected;
    
    [HideInInspector] public bool hovering;

    [SerializeField] public TextMeshProUGUI textMeshTabName;
    [SerializeField] public RectTransform discordPopout;
    [SerializeField] public Image icon;
    
    private readonly Color _iconColorHover = new Color(0, 0.5f, 1, 1);
    private readonly Color _iconColorSelected = new Color(.78f, 0.47f, 0, 1);

    private void Start()
    {
        _tabManager = transform.parent.parent.GetComponent<TabManager>();//Help

        tabId = _tabManager.GetTabId(this);
    }
    
    public void ChangeTab()
    {
        _tabManager.SelectTab(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!selected) icon.color = Color.white;
        hovering = false;
        StopCoroutine(SlideText());
        StartCoroutine(SlideText());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!selected) icon.color = _iconColorHover;
        hovering = true;
        StopCoroutine(SlideText());
        StartCoroutine(SlideText());
    }

    private IEnumerator SlideText()
    {
        
        
        //This does not work. It needs to be remade, I'm tired.
        /*float startTime = Time.time;

        Vector3 zero = new Vector3(0, 1, 1);
        Vector3 one = new Vector3(1, 1, 1);

        float length;
        if(hovering) length = Vector3.Distance(zero, one);
        else length = Vector3.Distance(one, zero);
        while (true)
        {
            discordPopout.localScale = Vector3.Lerp(discordPopout.localScale, hovering ? one : zero, (Time.time - startTime) * 0.0001f / length);
            if (discordPopout.localScale == one || discordPopout.localScale == zero)
            {
                yield return this;
            }
        }*/
        yield return this;
    }
    
    private void LateUpdate()
    {
        if (selected)
        {
            icon.color = _iconColorSelected;
        }
        else if(!hovering)
        {
            icon.color = Color.white;
        }
    }
}
