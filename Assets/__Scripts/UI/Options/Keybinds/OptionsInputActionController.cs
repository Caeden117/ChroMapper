using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Serialization;

public class OptionsInputActionController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keybindName;
    [SerializeField] private TMP_InputField[] keybindInputFields;
    [FormerlySerializedAs("SearchableOption")][SerializeField] internal SearchableOption searchableOption;

    private readonly Dictionary<TMP_InputField, InputBinding> binds = new Dictionary<TMP_InputField, InputBinding>();

    private readonly Dictionary<string, TMP_InputField> keybindNameToInputField =
        new Dictionary<string, TMP_InputField>();

    private readonly List<string> overrideKeybindPaths = new List<string>();
    private InputAction action;
    private string compositeName;
    private bool isAxisComposite;
    private int maxKeys = 3;
    private int minKeys = 1;

    private Color unselectedTextColor = new Color(0.792f, 0.792f, 0.792f); // 4F4F4F
    private Color selectedTextColor = new Color(0.162f, 0.629f, 0.802f);

    private Color unselectedImageColor = new Color(0.310f, 0.310f, 0.310f);
    private Color selectedImageColor = new Color(0.162f, 0.629f, 0.802f).Multiply(0.5f);

    private bool rebinding;
    private string sectionName;

    public void Init(string sName, InputAction inputAction, List<InputBinding> bindings, string compositeName = null,
        bool useCompositeName = false)
    {
        sectionName = sName;
        action = inputAction;
        this.compositeName = compositeName;
        
        var keybindNameText = useCompositeName ? $"{inputAction.name} ({compositeName})" : inputAction.name;
        keybindName.text = keybindNameText.StartsWith(KeybindsController.PersistentKeybindIdentifier)
            ? keybindNameText[1..] // Remove non-blocking prefix identifier
            : keybindNameText;
        
        UnselectKeybindUIs();
        searchableOption.Keywords = (keybindName.text + " " + sectionName).Split(' ');

        var splitSlashParam = new[] { '/' };

        for (var i = 0; i < bindings.Count; i++)
        {
            binds.Add(keybindInputFields[i], bindings[i]);

            // Default paths have the format "<Device>/Key"
            // Overriden paths have the format "/Device/Key"
            var name = (bindings[i].path.FirstOrDefault() == '<')
                ? PrettifyName(bindings[i].path.Split(splitSlashParam, 2).Last())
                : PrettifyName(bindings[i].path.Split(splitSlashParam, 3).Last());
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
        SelectKeybindUIs();
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

        // Finalize a rebind iff rebind operation was successful (keys == maxKeys || enter key pressed)
        CompleteRebind();

        // We need this deselect otherwise selecting the input afterwards 
        // will edit the text instead of performing the rebind
        EventSystem.current.SetSelectedGameObject(null);
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
        Reinitialize();
    }

    public void CancelKeybindRebind(string _)
    {
        if (!rebinding) return;
        Debug.Log($"Cancelling rebinding for {action.name}");
        StopAllCoroutines();
        Reinitialize();
    }

    public string PrettifyName(string name)
    {
        var gamemodeCharacters = name.ToCharArray();
        var builtFormat = new StringBuilder();
        for (var i = 0; i < gamemodeCharacters.Length; i++)
        {
            if (i == 0)
            {
                builtFormat.Append(char.ToUpper(gamemodeCharacters[i]));
                continue;
            }

            if (char.IsUpper(gamemodeCharacters[i]))
            {
                builtFormat.Append(" ");
                builtFormat.Append(gamemodeCharacters[i]);
            }
            else if (char.IsLetterOrDigit(gamemodeCharacters[i]))
            {
                builtFormat.Append(gamemodeCharacters[i]);
            }
            else
            {
                builtFormat.Append(" ");
            }
        }

        return builtFormat.ToString();
    }

    private void Reinitialize()
    {
        rebinding = false;
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

    private void UnselectKeybindUIs()
    {
        keybindName.color = unselectedTextColor;
        keybindName.fontStyle = FontStyles.Normal;

        foreach (var inputField in keybindInputFields)
        {
            inputField.image.color = unselectedImageColor;
            inputField.textComponent.fontStyle = FontStyles.Normal;
        }
    }

    private void SelectKeybindUIs()
    {
        keybindName.color = selectedTextColor;
        keybindName.fontStyle = FontStyles.Italic;

        foreach (var inputField in keybindInputFields)
        {
            inputField.image.color = selectedImageColor;
            inputField.textComponent.fontStyle = FontStyles.Italic;
        }
    }
}
