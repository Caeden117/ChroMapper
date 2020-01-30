using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [SerializeField] private TabButton _defaultTab;
    [SerializeField] private TextMeshProUGUI tabTitle;
    [SerializeField] private GameObject _tabsGameObject;

    [HideInInspector] public TabButton selectedTab;

    private List<Canvas> _tabs = new List<Canvas>();
    
    public void OnTabSelected(TabButton tab)
    {
        if(tab == selectedTab) return;
        selectedTab = tab;

        foreach (Canvas ca in _tabs)
        {
            ca.enabled = ca.name.Substring(0, ca.name.LastIndexOf(" Panel")) == tab.textMeshTabName.text;
        }
        
        tabTitle.text = "- " + tab.textMeshTabName.text;
    }

    private void Start()
    {
        _tabs.AddRange(_tabsGameObject.GetComponentsInChildren<Canvas>().Where(canvas => canvas.name.EndsWith("Panel")));
        
        OnTabSelected(_defaultTab);
    }
}
