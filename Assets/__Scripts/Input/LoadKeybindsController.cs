using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;

public class LoadKeybindsController : MonoBehaviour
{
    private static readonly string version = "1.0.0";

    public static List<KeybindOverride> AllOverrides = new List<KeybindOverride>();

    private string path;

    // Start is called before the first frame update
    private void Start() => Application.wantsToQuit += WantsToQuit;

    private void OnDestroy() => Application.wantsToQuit -= WantsToQuit;

    public void InputObjectCreated(object obj)
    {
        path = Application.persistentDataPath + "/ChroMapperOverrideKeybinds.json";
        if (File.Exists(path))
        {
            var keybindObject = JSON.Parse(File.ReadAllText(path));
            if (!keybindObject.HasKey("_version") || !keybindObject.HasKey("_overrides") ||
                keybindObject["_version"] != version)
            {
                Debug.LogWarning("New Keybind Override file does not exist, skipping...");
                return;
            }

            foreach (JSONNode node in keybindObject["_overrides"].AsArray)
            {
                var keybindOverride = new KeybindOverride(node);
                Debug.Log("Adding override for " + keybindOverride.InputActionName);
                AddKeybindOverride(keybindOverride);
            }
        }

        if (!Settings.Instance.InvertNoteControls)
        {
            MigrateNoteControls();
            Settings.Instance.InvertNoteControls = true;
        }
    }

    // In previous versions WASD controls were inverted. In order to change the defaults for new users
    // without messing up existing users, we manually add overrides to the previous control scheme if
    // the user had not overriden them
    private void MigrateNoteControls()
    {
        var upOverride = AllOverrides.Find(x => x.InputActionName == "Up Note");
        if (upOverride == null)
        {
            upOverride = new KeybindOverride("Up Note", "", new List<string> { "/Keyboard/s" });
            LoadKeybindsController.AddKeybindOverride(upOverride);
        }

        var rightOverride = AllOverrides.Find(x => x.InputActionName == "Right Note");
        if (rightOverride == null)
        {
            rightOverride = new KeybindOverride("Right Note", "", new List<string> { "/Keyboard/a" });
            LoadKeybindsController.AddKeybindOverride(rightOverride);
        }

        var downOverride = AllOverrides.Find(x => x.InputActionName == "Down Note");
        if (downOverride == null)
        {
            downOverride = new KeybindOverride("Down Note", "", new List<string> { "/Keyboard/w" });
            LoadKeybindsController.AddKeybindOverride(downOverride);
        }

        var leftOverride = AllOverrides.Find(x => x.InputActionName == "Left Note");
        if (leftOverride == null)
        {
            leftOverride = new KeybindOverride("Left Note", "", new List<string> { "/Keyboard/d" });
            LoadKeybindsController.AddKeybindOverride(leftOverride);
        }
    }

    /// <summary>
    ///     Adds a new <see cref="KeybindOverride" /> to the list of overrides. This will remove any already existing
    ///     overrides.
    /// </summary>
    /// <param name="keybindOverride"></param>
    public static void AddKeybindOverride(KeybindOverride keybindOverride)
    {
        // Do not override keybinds if paths are not in acceptable bounds
        if (keybindOverride.OverrideKeybindPaths.Count <= 0 || keybindOverride.OverrideKeybindPaths.Count > 4) return;
        // Remove anyexisting override to prevent duplicates
        AllOverrides.RemoveAll(x =>
            x.InputActionName == keybindOverride.InputActionName &&
            x.CompositeKeybindName == keybindOverride.CompositeKeybindName);

        // Grab our CMInput object and the map our action map is in.
        var input = CMInputCallbackInstaller.InputInstance;
        var map = input.asset.actionMaps.Where(x => x.actions.Any(y => y.name == keybindOverride.InputActionName))
            .FirstOrDefault();
        if (map is null) return;

        var action = map.FindAction(keybindOverride.InputActionName);

        // Determine what existing bindings we need to erase
        var toErase = new List<InputBinding>();
        // Grab our composite keybind
        var bindingToOverride =
            action.bindings.Where(x => x.name == keybindOverride.CompositeKeybindName).FirstOrDefault();
        if (bindingToOverride == null) // This is not a composite keybind, just grab the first one
            bindingToOverride = action.bindings.First();
        toErase.Add(bindingToOverride);
        // Grab all composite pieces
        for (var i = action.GetBindingIndex(bindingToOverride) + 1; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isPartOfComposite)
                toErase.Add(action.bindings[i]);
            else
                break;
        }
        // Reverse them so that the Composite keybind is erased last, and prevents errors.
        toErase.Reverse();
        // Erase the bindings
        foreach (var binding in toErase)
        {
            Debug.Log($"Deleting {binding.name} from {action.name}");
            action.ChangeBinding(action.GetBindingIndex(binding)).Erase();
        }

        // Add a new binding depending on some conditions
        switch (keybindOverride.OverrideKeybindPaths.Count)
        {
            case 1: // With one override path, make a regular binding
                action.AddBinding(keybindOverride.OverrideKeybindPaths[0]);
                break;
            case 2 when keybindOverride.IsAxisComposite: // Create a 1D Axis if we need to.
                action.AddCompositeBinding("1DAxis")
                    .With("positive", keybindOverride.OverrideKeybindPaths[0])
                    .With("negative", keybindOverride.OverrideKeybindPaths[1]);
                RenameCompositeBinding(action, keybindOverride);
                break;
            case 2 when !keybindOverride.IsAxisComposite: // Else, create a composite.
                action.AddCompositeBinding("ButtonWithOneModifier")
                    .With("modifier", keybindOverride.OverrideKeybindPaths[0])
                    .With("button", keybindOverride.OverrideKeybindPaths[1]);
                RenameCompositeBinding(action, keybindOverride);
                break;
            case 3: // No 1.5D Axis, so just a composite with two modifiers.
                action.AddCompositeBinding("ButtonWithTwoModifiers")
                    .With("modifier2", keybindOverride.OverrideKeybindPaths[0])
                    .With("modifier1", keybindOverride.OverrideKeybindPaths[1])
                    .With("button", keybindOverride.OverrideKeybindPaths[2]);
                RenameCompositeBinding(action, keybindOverride);
                break;
            case 4 when keybindOverride.IsAxisComposite: // 4 paths means a 2D Axis composite
                action.AddCompositeBinding("2DVector(mode=2)")
                    .With("up", keybindOverride.OverrideKeybindPaths[0])
                    .With("left", keybindOverride.OverrideKeybindPaths[1])
                    .With("down", keybindOverride.OverrideKeybindPaths[2])
                    .With("right", keybindOverride.OverrideKeybindPaths[3]);
                RenameCompositeBinding(action, keybindOverride);
                break;
        }

        Debug.Log($"Added keybind override for {keybindOverride.InputActionName}.");
        AllOverrides.Add(keybindOverride);
    }

    // Renames override binding to match original
    private static void RenameCompositeBinding(InputAction action, KeybindOverride keybindOverride)
    {
        var compositeBinding = action.bindings.Last(x => x.isComposite);
        action.ChangeBinding(compositeBinding).WithName(keybindOverride.CompositeKeybindName ?? "Override");
    }

    private bool WantsToQuit()
    {
        JSONNode keybindObject = new JSONObject();
        keybindObject["_version"] = version;

        var overridesArray = new JSONArray();
        foreach (var @override in AllOverrides) overridesArray.Add(@override.ToJsonNode());
        keybindObject["_overrides"] = overridesArray;

        File.WriteAllText(path, keybindObject.ToString(2));
        return true;
    }

    public class KeybindOverride
    {
        public string CompositeKeybindName;
        public string InputActionName;
        public bool IsAxisComposite;
        public List<string> OverrideKeybindPaths = new List<string>();

        public KeybindOverride(JSONNode obj)
        {
            if (!obj.HasKey("_actionName")) throw new ArgumentException("Keybind Override must have node \"_name\"");
            if (!obj.HasKey("_overridePaths"))
                throw new ArgumentException("Keybind Override must have node \"_overridePaths\"");
            if (obj["_overridePaths"].Count < 1) throw new ArgumentException("\"_overridePaths\" must not be empty.");

            InputActionName = obj["_actionName"];
            if (obj.HasKey("_compositeName")) CompositeKeybindName = obj["_compositeName"];

            if (obj.HasKey("_axisComposite")) IsAxisComposite = obj["_axisComposite"];
            foreach (JSONNode node in obj["_overridePaths"].AsArray) OverrideKeybindPaths.Add(node);
        }

        public KeybindOverride(string actionName, string compositeName, List<string> keybindPaths)
        {
            InputActionName = actionName;
            CompositeKeybindName = compositeName;
            OverrideKeybindPaths = keybindPaths;
        }

        public JSONNode ToJsonNode()
        {
            var obj = new JSONObject();
            obj["_actionName"] = InputActionName;
            obj["_axisComposite"] = IsAxisComposite;
            if (CompositeKeybindName != null) obj["_compositeName"] = CompositeKeybindName;

            var array = new JSONArray();
            foreach (var path in OverrideKeybindPaths) array.Add(path);
            obj["_overridePaths"] = array;

            return obj;
        }
    }
}
