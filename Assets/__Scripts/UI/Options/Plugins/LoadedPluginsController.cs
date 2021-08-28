﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LoadedPluginsController : MonoBehaviour
{
    [SerializeField] private GameObject pluginInfoPrefab;
    [SerializeField] private VerticalLayoutGroup parentLayoutGroup;
    [SerializeField] private SearchableTab searchableTab;

    public int Count => PluginLoader.LoadedPlugins.Count;

    // Start is called before the first frame update
    private void Start()
    {
        IEnumerable<Plugin> loadedPlugins = PluginLoader.LoadedPlugins;
        if (!loadedPlugins.Any())
        {
            gameObject.SetActive(false);
            return;
        }

        foreach (var plugin in loadedPlugins)
        {
            var pluginInfo = Instantiate(pluginInfoPrefab, transform).GetComponent<PluginInfoContainer>();
            pluginInfo.UpdatePluginInfo(plugin);
            searchableTab.RegisterSection(pluginInfo.SearchableSection);
        }

        StartCoroutine(FuckingSetThisShitDirty());
    }

    //Trying to set an external Layout Group dirty (to re-render the scene properly) is a pain in the ass.
    //If anyone knows of a better solution that consistently works, make a PR please.
    private IEnumerator FuckingSetThisShitDirty()
    {
        yield return new WaitForSeconds(0.1f);
        parentLayoutGroup.spacing = 15;
    }
}
