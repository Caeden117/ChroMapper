using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using UnityEngine.InputSystem.Controls;

public class OptionsInputActionController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keybindName;
    [SerializeField] private TMP_InputField[] keybindInputFields;
    private InputAction action = null;
    private bool isInit = false;
    private string compositeName = null;

    private Dictionary<TMP_InputField, InputBinding> binds = new Dictionary<TMP_InputField, InputBinding>();
    private Dictionary<string, TMP_InputField> keybindNameToInputField = new Dictionary<string, TMP_InputField>();

    public void Init(InputAction inputAction, List<InputBinding> bindings, string compositeName = null, bool useCompositeName = false)
    {
        if (isInit) return;
        action = inputAction;
        this.compositeName = compositeName;
        keybindName.text = useCompositeName ? $"{inputAction.name} ({compositeName})" : inputAction.name;
        for (int i = 0; i < bindings.Count; i++)
        {
            Debug.Log($"{inputAction.name}|{bindings[i].path}");
            binds.Add(keybindInputFields[i], bindings[i]);
            // TODO replace with "prettify text"
            string name = PrettifyName(bindings[i].path.Split('/').Last());
            keybindNameToInputField.Add(name, keybindInputFields[i]);

            keybindInputFields[i].text = name;
            keybindInputFields[i].onSelect.AddListener(OnKeybindSelected);
        }
        foreach (TMP_InputField input in keybindInputFields)
        {
            input.gameObject.SetActive(binds.ContainsKey(input));
        }
        isInit = true;
    }

    public void OnKeybindSelected(string text)
    {
        if (!keybindNameToInputField.ContainsKey(text)) return;
        keybindNameToInputField[text].text = "";
        Debug.Log($"Performing rebind for {action.name} ({compositeName})");
        keybindNameToInputField.Clear();
        for (int i = 1; i < keybindInputFields.Length; i++)
        {
            keybindInputFields[i].gameObject.SetActive(false);
        }

        int maxKeys = 3;
        int minKeys = 1;
        bool isAxisComposite = false;
        InputBinding compositeBinding = action.bindings.FirstOrDefault(x => x.name == compositeName);
        if (compositeBinding != null)
        {
            // Modify minKeys and maxKeys according to some path types.
            if (compositeBinding.path.Contains("2DVector"))
            {
                minKeys = maxKeys = 4;
                isAxisComposite = true;
            }
            else if (compositeBinding.path.Contains("1DAxis"))
            {
                minKeys = maxKeys = 2;
                isAxisComposite = true;
            }
        }

        StartCoroutine(PerformRebinding(minKeys, maxKeys, isAxisComposite));
    }

    private IEnumerator PerformRebinding(int minKeys, int maxKeys, bool isAxisComposite = false)
    {
        List<InputControl> allControls = new List<InputControl>();
        foreach (InputDevice device in InputSystem.devices)
        {
            allControls.AddRange(device.allControls);
        }
        IEnumerable<ButtonControl> allButtons = allControls.Where(x => x is ButtonControl).Cast<ButtonControl>();

        List<string> overrideKeybindPaths = new List<string>();
        int keys = 0;
        while (keys < maxKeys)
        {
            yield return new WaitUntil(() => allButtons.Any(x => x.wasPressedThisFrame));
            InputControl control = allButtons.FirstOrDefault(x => x.wasPressedThisFrame && x != Keyboard.current.anyKey);
            if (control is null || control.path.ToUpper().Contains("POSITION")) continue;
            if (control.path == Keyboard.current.enterKey.path)
            {
                // Do not stop rebinding if we do not reach minimum keys
                // This prevents 1DAxis and 2DVector keybinds from breaking
                if (keys < minKeys) continue; 
                break;
            }
            else
            {
                // Combine some common left/right keys into their generic counterparts
                if (control.path.ToUpper().Contains("SHIFT"))
                {
                    control = Keyboard.current.shiftKey; 
                }
                else if (control.path.ToUpper().Contains("CTRL"))
                {
                    control = Keyboard.current.ctrlKey;
                }
                else if (control.path.ToUpper().Contains("ALT"))
                {
                    control = Keyboard.current.altKey;
                }
                else if (control.path.ToUpper().Contains("PRESS"))
                {
                    control = Mouse.current.leftButton;
                }
                Debug.Log($"Detected key {control.path}");
                string name = PrettifyName(control.path.Split('/').Last());
                if (keybindNameToInputField.ContainsKey(name)) continue;
                overrideKeybindPaths.Add(control.path);

                TMP_InputField field = keybindInputFields[keys];
                field.gameObject.SetActive(true);
                field.text = name;

                keybindNameToInputField.Add(name, field);
            }
            keys++;
        }
        Debug.Log($"Completed rebinding.");
        var keybindOverride = new LoadKeybindsController.KeybindOverride(action.name, compositeName, overrideKeybindPaths);
        keybindOverride.IsAxisComposite = isAxisComposite;
        action.Disable();
        LoadKeybindsController.AddKeybindOverride(keybindOverride);
        action.Enable();
    }

    // this code totally not yoinked from ChromaToggle reloaded
    public string PrettifyName(string name)
    {
        char[] gamemodeCharacters = name.ToCharArray();
        StringBuilder builtFormat = new StringBuilder();
        for (int i = 0; i < gamemodeCharacters.Length; i++)
        {
            if (i == 0)
            {
                builtFormat.Append(gamemodeCharacters[i].ToString().ToUpper());
                continue;
            }
            if (gamemodeCharacters[i].ToString().ToUpper() == gamemodeCharacters[i].ToString() && !char.IsNumber(gamemodeCharacters[i]))
            {
                builtFormat.Append(" ");
            }
            builtFormat.Append(gamemodeCharacters[i]);
        }

        return builtFormat.ToString();
    }
}
