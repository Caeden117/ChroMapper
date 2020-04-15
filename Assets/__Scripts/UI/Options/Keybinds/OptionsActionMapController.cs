using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class OptionsActionMapController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private OptionsInputActionController keybindPrefab;
    [SerializeField] private VerticalLayoutGroup layoutGroup;

    private InputActionMap actionMap;
    private bool hasInit = false;

    public void Init(string name, InputActionMap map)
    {
        if (hasInit) return;
        title.text = name;
        actionMap = map;
        foreach (InputAction action in actionMap.actions)
        {
            //Spawn a copy of the keybind object.
            OptionsInputActionController keybind = Instantiate(keybindPrefab.gameObject, transform)
                .GetComponent<OptionsInputActionController>();
            keybind.Init(action);
        }
        keybindPrefab.gameObject.SetActive(false);
        layoutGroup.spacing = layoutGroup.spacing;
        hasInit = true;
    }
}
