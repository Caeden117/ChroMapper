using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    [SerializeField] private TabButton _defaultTab;
    [SerializeField] private TabButton _mapperTab;
    [SerializeField] private TextMeshProUGUI tabTitle;
    [SerializeField] private LocalizeStringEvent tabTitleString;
    [SerializeField] private TabButton creditsTab;
    [SerializeField] private GameObject _tabsGameObject;

    [HideInInspector] public TabButton selectedTab;
    public string tabName = "";

    private List<Canvas> _tabs = new List<Canvas>();

    public void OnTabSelected(TabButton tab)
    {
        if (tab == selectedTab) return;
        selectedTab = tab;

        tabName = tab.textMeshTabName.text;

        foreach (Canvas ca in _tabs)
        {
            ca.enabled = ca.name.Substring(0, ca.name.LastIndexOf(" Panel")) == tab.name.Substring(0, tab.name.LastIndexOf(" Tab"));
            if (ca.enabled)
                ca.BroadcastMessage("OnTabSelected", null, SendMessageOptions.DontRequireReceiver);
        }

        tabTitleString.StringReference.TableEntryReference = tab == creditsTab ? "tab.credits" : "heading";
        tabTitleString.StringReference.RefreshString();
    }

    private void Start()
    {
        _tabs.AddRange(_tabsGameObject.GetComponentsInChildren<Canvas>().Where(canvas => canvas.name.EndsWith("Panel")));
        
        OnTabSelected(SceneManager.GetActiveScene().name != "03_Mapper" ? _defaultTab : _mapperTab);
    }
}
