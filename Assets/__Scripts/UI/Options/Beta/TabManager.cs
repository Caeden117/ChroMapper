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

    public ButtonHelper selectedTab;

    public void OnTabSelected(ButtonHelper tab)
    {
        if(tab == selectedTab) return;
        selectedTab = tab;
        
        string tabName = tab.textMeshTabName.text;
        tabTitle.text = "- " + tabName;
        Debug.Log("Tab Selected: " + tabName + " : " + selectedTab.transform.GetSiblingIndex());
    }


    private void Awake()
    {
        _tabs.AddRange(tabsGameObject.GetComponentsInChildren<ButtonHelper>());//could be better
    }

    private void Start()
    {
        OnTabSelected(_tabs[0]);
    }
}
