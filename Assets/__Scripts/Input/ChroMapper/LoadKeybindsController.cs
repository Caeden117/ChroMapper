using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using SimpleJSON;
using System.Linq;
using System.Collections.Generic;

public class LoadKeybindsController : MonoBehaviour
{
    private CMInput input;
    private string path;

    private Dictionary<Guid, InputAction> idToAction = new Dictionary<Guid, InputAction>();

    // Start is called before the first frame update
    void Start()
    {
        Application.wantsToQuit += WantsToQuit;
        //Since we're not storing every keybind, just the overrides for them, its fitting.
        path = Application.persistentDataPath + "/ChroMapperOverrideKeybinds.json";
    }

    public void InputObjectCreated(object obj)
    {
        input = obj as CMInput;
        if (File.Exists(path))
        {
            JSONNode keybindObject = JSON.Parse(File.ReadAllText(path));
            foreach (string key in keybindObject.Keys)
            {
                input.asset[key].ApplyBindingOverride(0, keybindObject[key]);
            }
        }
    }

    private bool WantsToQuit()
    {
        JSONNode keybindObject = new JSONObject();
        foreach (InputAction action in input)
        {
            foreach (InputBinding binding in action.bindings)
            {
                if (!string.IsNullOrEmpty(binding.overridePath))
                {
                    keybindObject[action.id.ToString()] = binding.overridePath;
                }
            }
        }
        File.WriteAllText(path, keybindObject.ToString(2));
        return true;
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= WantsToQuit;
    }
}
