using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using SimpleJSON;

//TODO: Rename to KeybindsController when 100% converted
public class LoadKeybindsController : MonoBehaviour, CMInput.IUtilsActions
{
    public static bool ShiftHeld { get; private set; }
    public static bool CtrlHeld { get; private set; }
    public static bool AltHeld { get; private set; }

    private CMInput input;
    private JSONNode keybindsObject;
    private string path;

    // Start is called before the first frame update
    void Start()
    {
        Application.wantsToQuit += WantsToQuit;
        path = Application.persistentDataPath + "/ChroMapperKeybinds.json";
    }

    public void InputObjectCreated(object obj)
    {
        input = obj as CMInput;
        input.Utils.SetCallbacks(this);
        keybindsObject = new JSONObject();
        if (!File.Exists(path))
        {
            foreach (InputAction action in input)
            {
                JSONNode binding = ResetKeybindsForAction(action);
                keybindsObject[action.actionMap.name][action.name] = binding;
            }
        }
        else
        {
            keybindsObject = JSON.Parse(File.ReadAllText(path));
            foreach (InputAction action in input)
            {
                if (keybindsObject[action.actionMap.name][action.name] != null)
                {
                    JSONNode keybind = keybindsObject[action.actionMap.name][action.name];
                    if (keybind.IsString)
                    {
                        action.ApplyBindingOverride(0, keybind.Value);
                    }
                    else
                    {
                        for (int i = 0; i < keybind.Count; i++)
                        {
                            int bindingIndex = i + 1;
                            action.ApplyBindingOverride(bindingIndex, keybind[i].Value);
                        }
                    }
                }
                else
                {
                    JSONNode binding = ResetKeybindsForAction(action);
                    keybindsObject[action.actionMap.name][action.name] = binding;
                }
            }
        }
    }

    private bool WantsToQuit()
    {
        File.WriteAllText(path, keybindsObject.ToString(2));
        return true;
    }

    private JSONNode ResetKeybindsForAction(InputAction action)
    {
        JSONNode binding = new JSONObject();
        foreach (InputBinding bind in action.bindings)
        {
            if (action.bindings.Count == 1)
            {
                binding = bind.path;
                break;
            }
            else if (bind.isPartOfComposite)
            {
                binding[bind.name] = bind.path;
            }
        }
        return binding;
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= WantsToQuit;
    }

    public void OnControlModifier(InputAction.CallbackContext context)
    {
        CtrlHeld = context.performed;
    }

    public void OnAltModifier(InputAction.CallbackContext context)
    {
        AltHeld = context.performed;
    }

    public void OnShiftModifier(InputAction.CallbackContext context)
    {
        ShiftHeld = context.performed;
    }
}
