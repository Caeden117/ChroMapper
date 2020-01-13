using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHelper : UIBehaviour, IPointerExitHandler, IPointerEnterHandler
{
    [SerializeField] private string tabName;
    private GameObject _tabManager;
    private TextMeshProUGUI _tabTitle;
    private int tabId;

    private void Start()
    {
        _tabManager = transform.parent.parent.gameObject;
        _tabTitle = _tabManager.transform.GetChild(0).gameObject.GetComponent<TextMeshProUGUI>();
        
    }
    
    public void ChangeTab()
    {
        _tabTitle.text = transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponentsInChildren<Image>()[1].color = new Color(1,1,1,1);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponentsInChildren<Image>()[1].color = new Color(0,0.5f,1,1);
    }
}
