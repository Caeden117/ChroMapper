using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using SimpleJSON;
using System.Linq;

//TODO: Rename to KeybindsController when 100% converted
public class LoadKeybindsController : MonoBehaviour
{
    private CMInput input;
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
        if (File.Exists(path))
        {
            //TODO uncomment when keybinds format is finalized.
            //input.asset.LoadFromJson(File.ReadAllText(path));
        }
    }

    private bool WantsToQuit()
    {
        SaveInputKeybinds();
        return true;
    }

    public void SaveInputKeybinds()
    {
        File.WriteAllText(path, input.asset.ToJson());
    }

    private void OnDestroy()
    {
        Application.wantsToQuit -= WantsToQuit;
    }
}
