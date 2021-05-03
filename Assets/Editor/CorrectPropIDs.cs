using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SFB;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using SimpleJSON;

// Yeah, I'm a lazy ass who probably spent more time writing this script to automate the process rather than doing it manually
public class CorrectPropIDs
{
    [MenuItem("Edit/Correct Platform Prop IDs")]
    [SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "Unity doesn't let me do fun stuff")]
    public static void CorrectSelectedPlatformPropIDs()
    {
        var editor = new StandaloneFileBrowserEditor();

        var directories = editor.OpenFolderPanel("Open directory of proprietary prop data files", Environment.CurrentDirectory, false);
        var directory = directories[0];

        foreach (var assetRoot in Selection.gameObjects)
        {
            if (!assetRoot.TryGetComponent<PlatformDescriptor>(out _))
            {
                EditorUtility.DisplayDialog("ChroMapper", $"{assetRoot.name} is not a platform!", "OK");
                continue;
            }

            var assetPath = AssetDatabase.GetAssetPath(assetRoot);

            var file = Path.Combine(directory, assetRoot.name + "Environment.json");

            if (!File.Exists(file))
            {
                EditorUtility.DisplayDialog("ChroMapper", $"The file {file} doesn't exist!", "OK");
                continue;
            }

            /*var parsedData = ParseData(file);


            using (var editScope = new EditPrefabAssetScope(assetPath))
            {
                var prefabRoot = editScope.prefabRoot;
                var platformDescriptor = prefabRoot.GetComponent<PlatformDescriptor>();

                for (int eventID = 0; eventID < platformDescriptor.LightingManagers.Length; eventID++)
                {
                    var manager = platformDescriptor.LightingManagers[eventID];
                    CalculateIncorrectPropIDs(parsedData[eventID], manager);
                    var baseGameLights = parsedData[eventID].DistinctBy(l => l.IncorrectPropID);

                    manager.EditorToGamePropIDMap.Clear();

                    for (int i = 0; i < baseGameLights.Count(); i++)
                    {
                        manager.EditorToGamePropIDMap.Add(baseGameLights.ElementAt(i).PropID);
                    }
                }
            }*/
        }
    }

    private static Dictionary<int, List<BaseGameLight>> ParseData(string path)
    {
        var propInfo = JSON.Parse(File.ReadAllText(path))["props"];
        var dictionary = new Dictionary<int, List<BaseGameLight>>();

        foreach (var props in propInfo)
        {
            // I'm abusing the fact that Beat Games never hits 2 digits with Event IDs, so I can take this shortcut.
            // With Prop IDs, it's a bit more complex but still relatively simple.
            var eventId = int.Parse(props.Key);
            foreach (var propPair in props.Value)
            {
                var prop = propPair.Value;
                var propId = int.Parse(prop["_propId"]);
                var location = prop["position"].ReadVector3();

                if (!dictionary.TryGetValue(eventId, out var list))
                {
                    list = new List<BaseGameLight>();
                    dictionary.Add(eventId, list);
                }

                list.Add(new BaseGameLight
                {
                    PropID = propId,
                    WorldLocation = location
                });
            }
        }

        return dictionary;
    }

    private static void CalculateIncorrectPropIDs(List<BaseGameLight> lights, LightsManager manager)
    {
        Dictionary<int, List<BaseGameLight>> pregrouped = new Dictionary<int, List<BaseGameLight>>();
        foreach (BaseGameLight light in lights)
        {
            int z = Mathf.RoundToInt((light.WorldLocation.z * manager.GroupingMultiplier) + manager.GroupingOffset);
            if (pregrouped.TryGetValue(z, out List<BaseGameLight> list))
            {
                list.Add(light);
            }
            else
            {
                list = new List<BaseGameLight>();
                list.Add(light);
                pregrouped.Add(z, list);
            }
        }
        //We gotta squeeze the distance between Z positions into a nice 0-1-2-... array
        int i = 0;
        foreach (var group in pregrouped.Values)
        {
            if (group is null) continue;
            foreach (var light in group)
            {
                light.IncorrectPropID = i;
            }
            i++;
        }
    }

    private class BaseGameLight
    {
        public int PropID;
        public int IncorrectPropID;
        public Vector3 WorldLocation;
    }
}
