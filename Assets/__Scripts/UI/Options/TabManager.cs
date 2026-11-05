using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Serialization;

public class TabManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tabTitle;
    [SerializeField] private LocalizeStringEvent tabTitleString;
    [SerializeField] private OptionsTabButton creditsTab;
    [SerializeField] private List<OptionsTabButton> tabs = new();

    [FormerlySerializedAs("selectedTab")] [HideInInspector] public OptionsTabButton SelectedTab;
    public string TabName => SelectedTab != null ? SelectedTab.TextMeshTabName.text : "";

    private void Start()
    {
        if (tabs.Count == 0) return;
        OnTabSelected(tabs[Mathf.Clamp(OptionsController.StartingGroup, 0, tabs.Count - 1)]);
    }

    public void OnTabSelected(OptionsTabButton tab)
    {
        if (tab == SelectedTab) return;
        SelectedTab = tab;

        foreach (var button in tabs)
        {
            button.Panel.enabled = button == tab;
            if (button == tab)
                button.Panel.BroadcastMessage("OnTabSelected", null, SendMessageOptions.DontRequireReceiver);
        }

        tabTitleString.StringReference.TableEntryReference = tab == creditsTab ? "tab.credits" : "heading";
        tabTitleString.StringReference.RefreshString();
    }
}