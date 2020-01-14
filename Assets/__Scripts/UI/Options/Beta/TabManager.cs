using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [SerializeField] private GameObject tabsGameObject;
    private List<ButtonHelper> _tabs = new List<ButtonHelper>();
    [SerializeField] private TextMeshProUGUI tabTitle;
    
    private void Awake()
    {
        _tabs.AddRange(tabsGameObject.GetComponentsInChildren<ButtonHelper>());
    }

    private void Start()
    {
        _tabs[0].selected = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetTabId(ButtonHelper tab)
    {
        int i = 0;
        foreach (var buttonHelper in _tabs)
        {
            if (buttonHelper == tab) return i;
            i++;
        }

        return -0;
    }

    public void SelectTab(ButtonHelper tab)
    {
        foreach (var b in _tabs.Where(b => b.tabId != tab.tabId))
        {
            b.selected = false;
        }

        tab.selected = true;
        string tabName = tab.textMeshTabName.text;
        tabTitle.text = "- " + tabName;
        Debug.Log("Tab Selected: " + tabName + " : " + tab.tabId);
    }
}
