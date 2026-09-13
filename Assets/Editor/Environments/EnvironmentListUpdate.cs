using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EnvironmentListUpdate
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string scriptPath = "Assets/__Scripts/Environments";

    #pragma warning disable IDE0051 // Remove unused private members - used by unity script
    [MenuItem("Environment/Update Environment List", false, 800)]
    private static void PopulateBuildData()
    {
        // Track processed and skipped environments so generation failures are visible in the editor.
        var updatedEnvironmentCount = 0;
        var missingSceneCount = 0;
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            // Normalize the search prefix because AssetDatabase always returns forward-slash paths.
            .Where(x => x.StartsWith(PathUtils.Combine(environmentPath, "Data")) && x.EndsWith(".json"))
            .ToList();

        // An empty source set indicates a broken path or import state and must not look like a successful update.
        if (envDataPaths.Count == 0)
            throw new InvalidOperationException(
                $"No environment JSON files found under '{PathUtils.Combine(environmentPath, "Data")}'.");

        var listSo =
            // AssetDatabase paths must use Unity's forward-slash convention on every host platform.
            AssetDatabase.LoadAssetAtPath<EnvironmentListSO>(PathUtils.Combine(scriptPath, "EnvironmentListSO.asset"));

        // Updating without the central list would either throw later or leave generated assets disconnected.
        if (listSo == null)
            throw new InvalidOperationException(
                $"Environment list asset was not found at '{PathUtils.Combine(scriptPath, "EnvironmentListSO.asset")}'.");

        var assetToReserialize = new List<Object> { listSo };

        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            var data = CreateUtils.JsonToEnvironmentData(dataAsset);

            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                PathUtils.Combine(environmentPath, data.Data.ID + ".unity"));

            if (scene == null)
            {
                missingSceneCount++;
                // Identify every omitted environment rather than hiding incomplete generation behind the final count.
                Debug.LogError($"Skipping '{data.Data.ID}': scene asset was not found.");
                continue;
            }

            var colorSchemePath = PathUtils.Combine(scriptPath, "ColorSchemes", data.Data.ID + "ColorScheme.asset");
            var colorScheme = AssetDatabase.AssetPathExists(colorSchemePath)
                ? AssetDatabase.LoadAssetAtPath<ColorSchemeSO>(colorSchemePath)
                : ScriptableObject.CreateInstance<ColorSchemeSO>();

            var trackDefinitionsPath = PathUtils.Combine(
                scriptPath,
                "TrackDefinitions",
                data.Data.ID + "TrackDefinitions.asset");
            var trackDefinitions = AssetDatabase.AssetPathExists(trackDefinitionsPath)
                ? AssetDatabase.LoadAssetAtPath<TrackDefinitionsSO>(trackDefinitionsPath)
                : ScriptableObject.CreateInstance<TrackDefinitionsSO>();

            assetToReserialize.Add(colorScheme);
            assetToReserialize.Add(trackDefinitions);

            data.Data.ColorScheme.CopyTo(colorScheme);
            if (data.Data.LightTracks != null)
                // Build component capabilities from the exported object registrations without modifying exported data.
                data.Data.LightTracks.CopyTo(trackDefinitions, data.Objects, data.Data.ID);
            else
            {
                trackDefinitions.UnregisterAll();
                new TrackDefinitionBasic[]
                    {
                        new() { Kind = BasicEventKind.Lights, Type = 0, Name = "Back Light" },
                        new() { Kind = BasicEventKind.Lights, Type = 1, Name = "Ring" },
                        new() { Kind = BasicEventKind.Lights, Type = 2, Name = "Left Lasers" },
                        new() { Kind = BasicEventKind.Lights, Type = 3, Name = "Right Lasers" },
                        new() { Kind = BasicEventKind.Lights, Type = 4, Name = "Center Light" },
                        new() { Kind = BasicEventKind.Toggle, Type = 5, Name = "Boost" },
                        new() { Kind = BasicEventKind.IntValue, Type = 12, Name = "Left Speed" },
                        new() { Kind = BasicEventKind.IntValue, Type = 13, Name = "Right Speed" }
                    }
                    .ToList()
                    .ForEach(trackDefinitions.Register);
            }

            if (listSo.List.Exists(x => x.ID == data.Data.ID))
            {
                var d = listSo.List.First(x => x.ID == data.Data.ID);
                d.Name = data.Data.Title;
                d.ColorScheme = colorScheme;
                d.TrackDefinitions = trackDefinitions;
            }
            else
            {
                listSo.List.Add(
                    new EnvironmentListInfo
                    {
                        Name = data.Data.Title,
                        ID = data.Data.ID,
                        ColorScheme = colorScheme,
                        TrackDefinitions = trackDefinitions
                    });
            }

            if (!AssetDatabase.AssetPathExists(colorSchemePath))
                AssetDatabase.CreateAsset(colorScheme, colorSchemePath);
            if (!AssetDatabase.AssetPathExists(trackDefinitionsPath))
                AssetDatabase.CreateAsset(trackDefinitions, trackDefinitionsPath);

            updatedEnvironmentCount++;
        }

        // Never serialize an apparently valid empty result when all source environments were rejected.
        if (updatedEnvironmentCount == 0)
            throw new InvalidOperationException(
                $"No environment definitions were updated from {envDataPaths.Count} source files.");

        listSo.List = listSo.List.OrderBy(x => x.ID).ToList();

        foreach (var o in assetToReserialize) EditorUtility.SetDirty(o);
        AssetDatabase.SaveAssets();
        // Use an error for partial output so skipped environments cannot be overlooked among normal editor logs.
        if (missingSceneCount > 0)
            Debug.LogError(
                $"Updated {updatedEnvironmentCount} environment definitions, but skipped {missingSceneCount} without scenes.");
        else
            Debug.Log($"Updated all {updatedEnvironmentCount} environment definitions.");
    }

}
