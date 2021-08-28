using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class TabManager : MonoBehaviour
{
    [FormerlySerializedAs("_defaultTab")] [SerializeField] private OptionsTabButton defaultTab;
    [FormerlySerializedAs("_mapperTab")] [SerializeField] private OptionsTabButton mapperTab;
    [SerializeField] private TextMeshProUGUI tabTitle;
    [SerializeField] private LocalizeStringEvent tabTitleString;
    [SerializeField] private OptionsTabButton creditsTab;
    [FormerlySerializedAs("_tabsGameObject")] [SerializeField] private GameObject tabsGameObject;

    [FormerlySerializedAs("selectedTab")] [HideInInspector] public OptionsTabButton SelectedTab;

    private readonly List<Canvas> tabs = new List<Canvas>();
    public string TabName => SelectedTab.TextMeshTabName.text;

    private void Start()
    {
        tabs.AddRange(tabsGameObject.GetComponentsInChildren<Canvas>()
            .Where(canvas => canvas.name.EndsWith("Panel")));

        OnTabSelected(SceneManager.GetActiveScene().name != "03_Mapper" ? defaultTab : mapperTab);
    }

    public void OnTabSelected(OptionsTabButton tab)
    {
        if (tab == SelectedTab) return;
        SelectedTab = tab;

        foreach (var ca in tabs)
        {
            ca.enabled = ca.name.Substring(0, ca.name.LastIndexOf(" Panel")) ==
                         tab.name.Substring(0, tab.name.LastIndexOf(" Tab"));
            if (ca.enabled)
                ca.BroadcastMessage("OnTabSelected", null, SendMessageOptions.DontRequireReceiver);
        }

        tabTitleString.StringReference.TableEntryReference = tab == creditsTab ? "tab.credits" : "heading";
        tabTitleString.StringReference.RefreshString();
    }
}
