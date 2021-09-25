using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsActionMapController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private OptionsInputActionController keybindPrefab;
    [SerializeField] private VerticalLayoutGroup layoutGroup;
    public SearchableSection SearchableSection;

    private InputActionMap actionMap;
    private bool hasInit;

    public void Init(string name, InputActionMap map)
    {
        if (hasInit) return;
        title.text = name;
        actionMap = map;
        foreach (var action in actionMap.actions)
        {
            if (action.name.StartsWith("+"))
                continue; //Filter keybinds that should not be modified (Designated with + prefix)
            if (action.bindings.Any(x => x.isComposite))
            {
                var compositeName = action.bindings.First(x => x.isComposite).name;
                var useCompositeName = action.bindings.Count(x => x.isComposite) > 1;
                var bindings = new List<InputBinding>();
                for (var i = 0; i < action.bindings.Count; i++)
                {
                    if (action.bindings[i].isComposite && bindings.Any())
                    {
                        //Spawn a copy of the keybind object, and init them with input action data.
                        var keybind = Instantiate(keybindPrefab.gameObject, transform)
                            .GetComponent<OptionsInputActionController>();
                        keybind.Init(name, action, bindings, compositeName, useCompositeName);
                        SearchableSection.RegisterOption(keybind.searchableOption);

                        bindings.Clear();
                        compositeName = action.bindings[i].name;
                    }
                    else if (action.bindings[i].isPartOfComposite)
                    {
                        bindings.Add(action.bindings[i]);
                    }
                }

                var lastKeybind = Instantiate(keybindPrefab.gameObject, transform)
                    .GetComponent<OptionsInputActionController>();
                lastKeybind.Init(name, action, bindings, compositeName, useCompositeName);
                SearchableSection.RegisterOption(lastKeybind.searchableOption);
            }
            else
            {
                //Spawn a copy of the keybind object, and init them with input action data.
                var keybind = Instantiate(keybindPrefab.gameObject, transform)
                    .GetComponent<OptionsInputActionController>();
                keybind.Init(name, action, action.bindings.ToList());
                SearchableSection.RegisterOption(keybind.searchableOption);
            }
        }

        keybindPrefab.gameObject.SetActive(false);
        layoutGroup.spacing = layoutGroup.spacing;
        hasInit = true;
    }
}
