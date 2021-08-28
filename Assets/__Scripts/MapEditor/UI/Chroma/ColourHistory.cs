using System;
using System.IO;
using Assets.HSVPicker;
using SimpleJSON;
using UnityEngine;

public class ColourHistory : MonoBehaviour
{
    public static void Save()
    {
        var obj = new JSONObject();
        var colors = new JSONArray();
        foreach (var color in ColorPresetManager.Get().Colors)
        {
            var node = new JSONObject();
            node.WriteColor(color);
            colors.Add(node);
        }

        obj.Add("colors", colors);
        using (var writer = new StreamWriter(Application.persistentDataPath + "/ChromaColors.json", false))
        {
            writer.Write(obj.ToString());
        }

        Debug.Log("Chroma Colors saved!");
    }

    public static void Load()
    {
        if (!File.Exists(Application.persistentDataPath + "/ChromaColors.json"))
        {
            Debug.Log("Chroma Colors file doesn't exist! Skipping loading...");
            return;
        }

        try
        {
            ColorPresetManager.Presets.Clear();
            var presetList = new ColorPresetList("default");
            using (var reader = new StreamReader(Application.persistentDataPath + "/ChromaColors.json"))
            {
                var mainNode = JSON.Parse(reader.ReadToEnd());
                foreach (JSONNode n in mainNode["colors"].AsArray)
                {
                    var color = n.IsObject ? n.ReadColor(Color.black) : ColourManager.ColourFromInt(n.AsInt);
                    presetList.Colors.Add(color);
                }
            }

            Debug.Log($"Loaded {presetList.Colors.Count} colors!");
            ColorPresetManager.Presets.Add("default", presetList);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
