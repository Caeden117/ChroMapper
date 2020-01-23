using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    [SerializeField] private GameObject tabsGameObject;
    [SerializeField] private TextMeshProUGUI tabTitle;

    [HideInInspector] public ButtonHelper selectedTab;

    public void OnTabSelected(ButtonHelper tab)
    {
        if(tab == selectedTab) return;
        selectedTab = tab;
        
        string tabName = tab.textMeshTabName.text;
        tabTitle.text = "- " + tabName;
        Debug.Log("Tab Selected: " + tabName + " : " + selectedTab.transform.GetSiblingIndex());
    }

    private void Start()
    {
        OnTabSelected(tabsGameObject.GetComponentInChildren<ButtonHelper>());
    }
}
