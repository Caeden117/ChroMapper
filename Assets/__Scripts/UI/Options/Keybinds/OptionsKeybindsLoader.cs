using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsKeybindsLoader : MonoBehaviour
{
    [SerializeField] private OptionsActionMapController prefab;
    [SerializeField] private RectTransform parent;
    [SerializeField] private VerticalLayoutGroup parentLayoutGroup;
    private bool isInit = false;

    // Start is called before the first frame update
    void OnTabSelected()
    {
        if (isInit) return;
        CMInput input = CMInputCallbackInstaller.InputInstance;
        foreach (InputActionMap actionMap in input.asset.actionMaps)
        {
            OptionsActionMapController map = Instantiate(prefab.gameObject, parent).GetComponent<OptionsActionMapController>();
            map.Init(actionMap.name, actionMap);
            map.gameObject.name = $"{actionMap.name} Action Map";
        }
        parentLayoutGroup.spacing = parentLayoutGroup.spacing;
        prefab.gameObject.SetActive(false);
        isInit = true;
    }
}
