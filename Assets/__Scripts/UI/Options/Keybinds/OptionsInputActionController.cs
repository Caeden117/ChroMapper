using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

public class OptionsInputActionController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keybindName;
    [SerializeField] private TMP_InputField[] keybindInputFields;
    [FormerlySerializedAs("SearchableOption")] [SerializeField] internal SearchableOption searchableOption;

    private readonly Dictionary<TMP_InputField, InputBinding> binds = new Dictionary<TMP_InputField, InputBinding>();

    private readonly Dictionary<string, TMP_InputField> keybindNameToInputField =
        new Dictionary<string, TMP_InputField>();

    private readonly List<string> overrideKeybindPaths = new List<string>();
    private InputAction action;
    private string compositeName;
    private bool isAxisComposite;
    private int maxKeys = 3;
    private int minKeys = 1;

    private bool rebinding;
    private string sectionName;

    public void Init(string sName, InputAction inputAction, List<InputBinding> bindings, string compositeName = null,
        bool useCompositeName = false)
    {
        sectionName = sName;
        action = inputAction;
        this.compositeName = compositeName;
        keybindName.text = useCompositeName ? $"{inputAction.name} ({compositeName})" : inputAction.name;
        searchableOption.Keywords = (keybindName.text + " " + sectionName).Split(' ');
        for (var i = 0; i < bindings.Count; i++)
        {
            binds.Add(keybindInputFields[i], bindings[i]);
            // TODO replace with "prettify text"
            var name = PrettifyName(bindings[i].path.Split('/').Last());
            keybindNameToInputField.Add(name, keybindInputFields[i]);

            keybindInputFields[i].text = name;
            keybindInputFields[i].onSelect.AddListener(OnKeybindSelected);
            keybindInputFields[i].onDeselect.AddListener(CancelKeybindRebind);
        }

        foreach (var input in keybindInputFields) input.gameObject.SetActive(binds.ContainsKey(input));

        var compositeBinding = action.bindings.Where(x => x.name == compositeName).FirstOrDefault();
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
        for (var i = 1; i < keybindInputFields.Length; i++) keybindInputFields[i].gameObject.SetActive(false);

        GetComponentInParent<OptionsKeybindsLoader>()
            .BroadcastMessage("CancelKeybindRebind", "LULW", SendMessageOptions.DontRequireReceiver);
        StartCoroutine(PerformRebinding(minKeys, maxKeys, isAxisComposite));
    }

    private IEnumerator PerformRebinding(int minKeys, int maxKeys, bool isAxisComposite = false)
    {
        rebinding = true;
        var allControls = new List<ButtonControl>();
        foreach (var device in InputSystem.devices)
            allControls.AddRange(device.allControls.Where(x => x is ButtonControl).Cast<ButtonControl>());

        overrideKeybindPaths.Clear();
        var keys = 0;
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

            // Combine some common left/right keys into their generic counterparts
            if (control.path.ToUpper().Contains("SHIFT"))
                control = Keyboard.current.shiftKey;
            else if (control.path.ToUpper().Contains("CTRL"))
                control = Keyboard.current.ctrlKey;
            else if (control.path.ToUpper().Contains("ALT"))
                control = Keyboard.current.altKey;
            else if (control.path.ToUpper().Contains("PRESS")) control = Mouse.current.leftButton;
            Debug.Log($"Detected key {control.path}");
            var name = PrettifyName(control.path.Split('/').Last());
            if (keybindNameToInputField.ContainsKey(name)) continue;
            overrideKeybindPaths.Add(control.path);

            var field = keybindInputFields[keys];
            field.gameObject.SetActive(true);
            field.text = name;

            keybindNameToInputField.Add(name, field);
            keys++;
        }
    }

    private void CompleteRebind()
    {
        Debug.Log("Completed rebinding.");
        var keybindOverride =
            new LoadKeybindsController.KeybindOverride(action.name, compositeName, overrideKeybindPaths)
            {
                IsAxisComposite = isAxisComposite
            };
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
            var compositeName = action.bindings.First(x => x.isComposite).name;
            var useCompositeName = action.bindings.Count(x => x.isComposite) > 1;
            var bindings = new List<InputBinding>();
            for (var i = 0; i < action.bindings.Count; i++)
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
        var gamemodeCharacters = name.ToCharArray();
        var builtFormat = new StringBuilder();
        for (var i = 0; i < gamemodeCharacters.Length; i++)
        {
            if (i == 0)
            {
                builtFormat.Append(gamemodeCharacters[i].ToString().ToUpper());
                continue;
            }

            if (gamemodeCharacters[i].ToString().ToUpper() == gamemodeCharacters[i].ToString() &&
                !char.IsNumber(gamemodeCharacters[i]))
            {
                builtFormat.Append(" ");
            }

            builtFormat.Append(gamemodeCharacters[i]);
        }

        return builtFormat.ToString();
    }
}
