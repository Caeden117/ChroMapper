using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assets.HSVPicker;
using SimpleJSON;
using System.IO;

public class ColourHistory : MonoBehaviour {
    private static ColourHistory Instance;

    public static void Save()
    {
        JSONObject obj = new JSONObject();
        JSONArray colors = new JSONArray();
        foreach (Color color in ColorPresetManager.Get().Colors)
        {
            var node = new JSONObject();
            node.WriteColor(color, true);
            colors.Add(node);
        }
        obj.Add("colors", colors);
        using (StreamWriter writer = new StreamWriter(Application.persistentDataPath + "/ChromaColors.json", false))
            writer.Write(obj.ToString());
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
            ColorPresetList presetList = new ColorPresetList("default");
            using (StreamReader reader = new StreamReader(Application.persistentDataPath + "/ChromaColors.json"))
            {
                JSONNode mainNode = JSON.Parse(reader.ReadToEnd());
                foreach (JSONNode n in mainNode["colors"].AsArray)
                {
                    Color color = n.IsObject ? n.ReadColor(Color.black) : ColourManager.ColourFromInt(n.AsInt);
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
