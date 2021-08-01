using UnityEngine;
using TMPro;

public class PluginInfoContainer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI versionText;

    public SearchableSection SearchableSection;
    [SerializeField] private SearchableOption searchableOption;

    public void UpdatePluginInfo(Plugin plugin)
    {
        nameText.text = plugin.Name;
        versionText.text = $"v{plugin.Version}";

        searchableOption.Keywords = plugin.Name.Split(' ');
    }
}
