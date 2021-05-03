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
    [SerializeField] private VerticalLayoutGroup parentLayoutGroup;
    private bool isInit = false;

    private List<OptionsActionMapController> allActionMaps = new List<OptionsActionMapController>();

    // Start is called before the first frame update
    void OnTabSelected()
    {
        if (isInit) return;
        CMInput input = CMInputCallbackInstaller.InputInstance;
        //Grab each Action Map and create our Action Map Controller, which will loop through each Input Action and create Keybinds.
        foreach (InputActionMap actionMap in input.asset.actionMaps)
        {
            if (actionMap.name.StartsWith("+")) continue; //Filter keybinds that should not be modified (Designated with + prefix)
            OptionsActionMapController map = Instantiate(prefab.gameObject, parent).GetComponent<OptionsActionMapController>();
            map.Init(actionMap.name, actionMap);
            map.gameObject.name = $"{actionMap.name} Action Map";
            allActionMaps.Add(map);
        }
        StartCoroutine(FuckingSetThisShitDirty());
        prefab.gameObject.SetActive(false);
        isInit = true;
    }

    //Trying to set an external Layout Group dirty (to re-render the scene properly) is a pain in the ass.
    //If anyone knows of a better solution that consistently works, make a PR please.
    private IEnumerator FuckingSetThisShitDirty()
    {
        yield return new WaitForSeconds(0.1f);
        parentLayoutGroup.spacing = 15;
    }

    public void AskForHardReload()
    {
        PersistentUI.Instance.ShowDialogBox("Options", "keybinds.reset.confirm", HandleHardReload,
            PersistentUI.DialogBoxPresetType.YesNo);
    }

    private void HandleHardReload(int res)
    {
        if (res == 0)
        {
            foreach(InputAction action in CMInputCallbackInstaller.InputInstance)
            {
                action.RemoveAllBindingOverrides();
            }
            isInit = false;
            foreach (OptionsActionMapController map in allActionMaps)
            {
                Destroy(map.gameObject);
            }
            prefab.gameObject.SetActive(true);
            allActionMaps.Clear();
            LoadKeybindsController.AllOverrides.Clear();
            CMInputCallbackInstaller.InputInstance = new CMInput();
            parentLayoutGroup.spacing = 0;
            OnTabSelected();
        }
    }
}
