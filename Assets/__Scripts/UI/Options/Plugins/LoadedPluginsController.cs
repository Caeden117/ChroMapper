using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadedPluginsController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadedPluginCount;
    [SerializeField] private GameObject pluginInfoPrefab;
    [SerializeField] private VerticalLayoutGroup parentLayoutGroup;

    // Start is called before the first frame update
    void Start()
    {
        IEnumerable<Plugin> loadedPlugins = PluginLoader.LoadedPlugins;
        loadedPluginCount.text = $"Loaded Plugins: {loadedPlugins.Count()}";
        if (!loadedPlugins.Any())
        {
            gameObject.SetActive(false);
            return;
        }
        foreach (Plugin plugin in loadedPlugins)
        {
            PluginInfoContainer pluginInfo = Instantiate(pluginInfoPrefab, transform).GetComponent<PluginInfoContainer>();
            pluginInfo.UpdatePluginInfo(plugin);
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
