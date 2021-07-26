using UnityEngine;
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
    [SerializeField] internal SearchableOption SearchableOption;
    private string sectionName = null;
    private InputAction action = null;
    private string compositeName = null;

    private bool rebinding = false;

    private Dictionary<TMP_InputField, InputBinding> binds = new Dictionary<TMP_InputField, InputBinding>();
    private Dictionary<string, TMP_InputField> keybindNameToInputField = new Dictionary<string, TMP_InputField>();

    private List<string> overrideKeybindPaths = new List<string>();
    private bool isAxisComposite = false;
    private int minKeys = 1;
    private int maxKeys = 3;

    public void Init(string sName, InputAction inputAction, List<InputBinding> bindings, string compositeName = null, bool useCompositeName = false)
    {
        sectionName = sName;
        action = inputAction;
        this.compositeName = compositeName;
        keybindName.text = useCompositeName ? $"{inputAction.name} ({compositeName})" : inputAction.name;
        SearchableOption.Keywords = (keybindName.text + " " + sectionName).Split(' ');
        for (int i = 0; i < bindings.Count; i++)
        {
            binds.Add(keybindInputFields[i], bindings[i]);
            // TODO replace with "prettify text"
            string name = PrettifyName(bindings[i].path.Split('/').Last());
            keybindNameToInputField.Add(name, keybindInputFields[i]);

            keybindInputFields[i].text = name;
            keybindInputFields[i].onSelect.AddListener(OnKeybindSelected);
            keybindInputFields[i].onDeselect.AddListener(CancelKeybindRebind);
        }
        foreach (TMP_InputField input in keybindInputFields)
        {
            input.gameObject.SetActive(binds.ContainsKey(input));
        }

        InputBinding compositeBinding = action.bindings.Where(x => x.name == compositeName).FirstOrDefault();
        if (compositeBinding != null && !string.IsNullOrEmpty(compositeBinding.path))
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

        GetComponentInParent<OptionsKeybindsLoader>().BroadcastMessage("CancelKeybindRebind", "LULW", SendMessageOptions.DontRequireReceiver);
        StartCoroutine(PerformRebinding(minKeys, maxKeys, isAxisComposite));
    }

    private IEnumerator PerformRebinding(int minKeys, int maxKeys, bool isAxisComposite = false)
    {
        rebinding = true;
        List<ButtonControl> allControls = new List<ButtonControl>();
        foreach (InputDevice device in InputSystem.devices)
        {
            allControls.AddRange(device.allControls.Where(x => x is ButtonControl).Cast<ButtonControl>());
        }

        overrideKeybindPaths.Clear();
        int keys = 0;
        while (keys < maxKeys)
        {
            yield return new WaitUntil(() => allControls.Find(x => x.wasPressedThisFrame) != null);
            InputControl control = allControls.Find(x => x.wasPressedThisFrame && x != Keyboard.current.anyKey);
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
    }

    private void CompleteRebind()
    {
        Debug.Log($"Completed rebinding.");
        var keybindOverride = new LoadKeybindsController.KeybindOverride(action.name, compositeName, overrideKeybindPaths);
        keybindOverride.IsAxisComposite = isAxisComposite;
        action.Disable();
        LoadKeybindsController.AddKeybindOverride(keybindOverride);
        action.Enable();
        rebinding = false;
    }

    public void CancelKeybindRebind(string _)
    {
        if (!rebinding) return;
        Debug.Log($"Cancelling rebinding for {action.name}");
        StopAllCoroutines();

        if (overrideKeybindPaths.Count > 0) CompleteRebind();

        binds.Clear();
        keybindNameToInputField.Clear();

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
                    Init(sectionName, action, bindings, compositeName, useCompositeName);
                    break;
                }
                else if (action.bindings[i].isPartOfComposite)
                {
                    bindings.Add(action.bindings[i]);
                }
            }
            Init(sectionName, action, bindings, compositeName, useCompositeName);
        }
        else
        {
            Init(sectionName, action, action.bindings.ToList());
        }
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
