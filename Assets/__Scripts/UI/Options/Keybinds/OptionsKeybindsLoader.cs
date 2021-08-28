using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsKeybindsLoader : MonoBehaviour
{
    [SerializeField] private OptionsActionMapController prefab;
    [SerializeField] private RectTransform parent;
    [SerializeField] private GameObject warning;
    [SerializeField] private RectTransform parentLayoutGroup;
    [SerializeField] private SearchableTab searchableTab;
    [SerializeField] private SearchInputField searchInputField;

    private readonly List<OptionsActionMapController> allActionMaps = new List<OptionsActionMapController>();

    private bool loadInProgress;
    private bool isInit;

    // Start is called before the first frame update
    internal void OnTabSelected()
    {
        if (isInit) return;

        StartCoroutine(LoadKeybindsAsync());
        prefab.gameObject.SetActive(false);
    }

    private IEnumerator LoadKeybindsAsync()
    {
        if (loadInProgress) yield break;
        loadInProgress = true;

        var input = CMInputCallbackInstaller.InputInstance;
        //Grab each Action Map and create our Action Map Controller, which will loop through each Input Action and create Keybinds.
        foreach (var actionMap in input.asset.actionMaps)
        {
            if (actionMap.name.StartsWith("+"))
                continue; //Filter keybinds that should not be modified (Designated with + prefix)
            var map = Instantiate(prefab.gameObject, parent).GetComponent<OptionsActionMapController>();
            map.Init(actionMap.name, actionMap);
            map.gameObject.name = $"{actionMap.name} Action Map";
            searchableTab.RegisterSection(map.SearchableSection);
            allActionMaps.Add(map);
            yield return null;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(parentLayoutGroup);

        loadInProgress = false;
        isInit = true;
        searchableTab.UpdateSearch(searchInputField.InputField.text);
    }

    public void AskForHardReload() =>
        PersistentUI.Instance.ShowDialogBox("Options", "keybinds.reset.confirm", HandleHardReload,
            PersistentUI.DialogBoxPresetType.YesNo);

    private void HandleHardReload(int res)
    {
        if (res == 0)
        {
            foreach (var action in CMInputCallbackInstaller.InputInstance) action.RemoveAllBindingOverrides();
            isInit = false;
            foreach (var map in allActionMaps) Destroy(map.gameObject);
            prefab.gameObject.SetActive(true);
            allActionMaps.Clear();
            LoadKeybindsController.AllOverrides.Clear();
            CMInputCallbackInstaller.InputInstance = new CMInput();
            OnTabSelected();
        }
    }
}
