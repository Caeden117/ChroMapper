using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SearchableTab : MonoBehaviour
{
    [SerializeField] private List<SearchableSection> sections;
    [SerializeField] private RectTransform layoutGroup;
    [SerializeField] private GameObject tab;

    public void RegisterSection(SearchableSection section)
    {
        sections.Add(section);
    }

    public bool UpdateSearch(string text)
    {
        var result = sections.Select(it => it.UpdateSearch(text)).ToList().Any(it => it);
        tab.SetActive(result);
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup);
        return result;
    }
}