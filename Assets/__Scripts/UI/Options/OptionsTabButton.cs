using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsTabButton : UIBehaviour, IPointerExitHandler, IPointerEnterHandler //Should be renamed to TabHelper or something
{
    private TabManager _tabManager;
    
    [HideInInspector] public bool hovering;

    [SerializeField] public TextMeshProUGUI textMeshTabName;
    [SerializeField] public RectTransform discordPopout;
    [SerializeField] public CanvasGroup discordPopoutCanvas;
    [SerializeField] public Image icon;
    
    private readonly Color _iconColorHover = new Color(0, 0.5f, 1, 1);
    private readonly Color _iconColorSelected = new Color(.78f, 0.47f, 0, 1);

    private Coroutine _discordPopoutCoroutine;

    protected override void Start()
    {
        _tabManager = transform.GetComponentInParent<TabManager>(); //this exists please use it
    }

    public void RefreshWidth()
    {
        Vector2 discordPopoutSize = discordPopout.sizeDelta;
        discordPopout.sizeDelta = new Vector2(textMeshTabName.preferredWidth + 5, discordPopoutSize.y);
    }
    
    public void ChangeTab()
    {
        _tabManager.OnTabSelected(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(_tabManager.selectedTab != this) icon.color = Color.white;
        hovering = false;
        _discordPopoutCoroutine = StartCoroutine(SlideText());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(_tabManager.selectedTab != this) icon.color = _iconColorHover;
        hovering = true;
        _discordPopoutCoroutine = StartCoroutine(SlideText());
    }

    private IEnumerator SlideText()
    {
        if(_discordPopoutCoroutine != null) StopCoroutine(_discordPopoutCoroutine);
        
        float startTime = Time.time;
        Vector3 zero = new Vector3(0, 1, 1);
        Vector3 one = new Vector3(1, 1, 1);
        
        while (true)
        {
            Vector3 localScale = discordPopout.localScale;
            localScale = Vector3.MoveTowards(localScale, hovering ? one : zero, (Time.time / startTime) * .2f);
            discordPopout.localScale = localScale;
            discordPopoutCanvas.alpha = localScale.x;
            if (localScale.x >= 1f)
            {
                discordPopout.localScale = one;
                discordPopoutCanvas.alpha = 1f;
                break;
            }
            if (localScale.x <= 0f)
            {
                discordPopout.localScale = zero;
                discordPopoutCanvas.alpha = 0f;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
    }
    
    private void LateUpdate()
    {
        if (_tabManager.selectedTab == this)
        {
            icon.color = _iconColorSelected;
        }
        else if(!hovering)
        {
            icon.color = Color.white;
        }
    }
}
