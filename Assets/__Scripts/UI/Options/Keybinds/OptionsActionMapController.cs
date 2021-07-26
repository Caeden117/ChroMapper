using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;

public class OptionsActionMapController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private OptionsInputActionController keybindPrefab;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    public SearchableSection SearchableSection;

    private InputActionMap actionMap;
    private bool hasInit = false;

    public void Init(string name, InputActionMap map)
    {
        if (hasInit) return;
        title.text = name;
        actionMap = map;
        foreach (InputAction action in actionMap.actions)
        {
            if (action.name.StartsWith("+")) continue; //Filter keybinds that should not be modified (Designated with + prefix)
            if (action.bindings.Any(x => x.isComposite))
            {
                string compositeName = action.bindings.First(x => x.isComposite).name;
                bool useCompositeName = action.bindings.Count(x => x.isComposite) > 1;
                List<InputBinding> bindings = new List<InputBinding>();
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite && bindings.Any())
                    {
                        //Spawn a copy of the keybind object, and init them with input action data.
                        OptionsInputActionController keybind = Instantiate(keybindPrefab.gameObject, transform)
                            .GetComponent<OptionsInputActionController>();
                        keybind.Init(name, action, bindings, compositeName, useCompositeName);
                        SearchableSection.RegisterOption(keybind.SearchableOption);
                        
                        bindings.Clear();
                        compositeName = action.bindings[i].name;
                    }
                    else if (action.bindings[i].isPartOfComposite)
                    {
                        bindings.Add(action.bindings[i]);
                    }
                }
                OptionsInputActionController lastKeybind = Instantiate(keybindPrefab.gameObject, transform)
                    .GetComponent<OptionsInputActionController>();
                lastKeybind.Init(name, action, bindings, compositeName, useCompositeName);
                SearchableSection.RegisterOption(lastKeybind.SearchableOption);
            }
            else
            {
                //Spawn a copy of the keybind object, and init them with input action data.
                OptionsInputActionController keybind = Instantiate(keybindPrefab.gameObject, transform)
                    .GetComponent<OptionsInputActionController>();
                keybind.Init(name, action, action.bindings.ToList());
                SearchableSection.RegisterOption(keybind.SearchableOption);
            }
        }
        keybindPrefab.gameObject.SetActive(false);
        layoutGroup.spacing = layoutGroup.spacing;
        hasInit = true;
    }
}
