using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Uses Unity's localization/serialization APIs, never hand-edits generated locale tables.</summary>
public static class TempoMapperBuild
{
    private static readonly Dictionary<string, string> strings = new()
    {
        ["title"] = "Tempo Mapper",
        ["settings"] = "Tempo Mapper settings",
        ["timeline.display"] = "Timeline overview",
        ["editor.display"] = "Editing display",
        ["waveform"] = "Waveform",
        ["spectrogram"] = "Spectograph",
        ["both"] = "Both",
        ["waveform.rectify"] = "Rectify peaks",
        ["follow"] = "Follow playback",
        ["shift"] = "Shift BPM",
        ["precision"] = "Beat subdivision",
        ["reset"] = "Reset",
        ["zoom"] = "Zoom",
        ["volume"] = "Music volume",
        ["statistics"] = "{0:00}:{1:00.000}   |   Beat {2:F2}   |   Region {3}/{4}",
        ["play"] = "Play",
        ["pause"] = "Pause",
        ["bpm"] = "Region BPM",
        ["beats"] = "Region beats",
        ["undo"] = "Undo",
        ["redo"] = "Redo",
        ["metronome"] = "Metronome",
        ["speed"] = "Playback speed",
        ["invalid"] = "Cannot apply this edit. Keep positive region lengths and BPM between 1 and 1000.",
        ["unsaved"] = "Unsaved timing changes",
        ["error"] = "Tempo mapping failed: {0}",
        ["save"] = "Save",
        ["close"] = "Back to song setup",
        ["discard.prompt"] = "Discard the unsaved tempo changes and return to song setup?",
        ["discard"] = "Discard changes",
        ["keep.editing"] = "Keep editing"
    };

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        // Asset import/build callbacks must not load a developer's incompatible saved application settings.
        if (Application.isBatchMode) Settings.TestMode = true;
        EditorApplication.delayCall += EnsureLocalization;
    }

    private static void EnsureLocalization()
    {
        var collection = LocalizationEditorSettings.GetStringTableCollection("SongEditMenu");
        if (collection == null) return;
        var changed = false;
        // Remove strings for controls replaced during the Tempo Mapper implementation.
        foreach (var entry in collection.SharedData.Entries.Where(entry => entry.Key.StartsWith("tempo.")
                     && !strings.ContainsKey(entry.Key.Substring(6))).ToArray())
        {
            collection.RemoveEntry(entry.Key);
            foreach (var table in collection.StringTables) EditorUtility.SetDirty(table);
            changed = true;
        }
        foreach (var pair in strings)
        {
            var key = "tempo." + pair.Key;
            foreach (var table in collection.StringTables)
            {
                if (table.GetEntry(key) != null)
                {
                    var existing = table.GetEntry(key).Value;
                    if (!((pair.Key == "title" && (existing == "Tempo mapping" || existing == "Tempo Mapping"))
                        || (pair.Key == "precision" && existing == "Cursor precision")
                        || (pair.Key == "spectrogram" && existing == "Spectrogram")
                        || (pair.Key == "save" && existing == "Save tempo"))) continue;
                    table.GetEntry(key).Value = pair.Value;
                    EditorUtility.SetDirty(table);
                    changed = true;
                    continue;
                }
                table.AddEntry(key, pair.Value);
                EditorUtility.SetDirty(table);
                changed = true;
            }
        }
        if (!changed) return;
        EditorUtility.SetDirty(collection.SharedData);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureTempoOptionsCategory()
    {
        var scene = EditorSceneManager.OpenScene("Assets/__Scenes/04_Options.unity");
        var manager = UnityEngine.Object.FindAnyObjectByType<TabManager>();
        var serialized = new SerializedObject(manager);
        var mapper = (OptionsTabButton)serialized.FindProperty("mapperTab").objectReferenceValue;
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
            foreach (var item in root.GetComponentsInChildren<Transform>(true).ToArray())
                if (item != null && (item.name == "Tempo Mapper Tab" || item.name == "Tempo Mapper Panel")) UnityEngine.Object.DestroyImmediate(item.gameObject);
        var panelName = mapper.name.Substring(0, mapper.name.LastIndexOf(" Tab", StringComparison.Ordinal)) + " Panel";
        var panel = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).First(canvas => canvas.name == panelName);
        var english = LocalizationEditorSettings.GetStringTableCollection("SongEditMenu").StringTables.First(table => table.LocaleIdentifier.Code == "en");
        var metronomeIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Graphics/Textures/UI/Editor UI/metronome.png");
        var tempo = TempoMapperOptions.Create(mapper, panel, key => english.GetEntry("tempo." + key)?.Value ?? key, metronomeIcon);
        serialized.FindProperty("tempoTab").objectReferenceValue = tempo;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("ChroMapper/Build Tempo Mapping MVP")]
    public static void Build()
    {
        EnsureLocalization();
        EnsureTempoOptionsCategory();
        var scene = EditorSceneManager.OpenScene("Assets/__Scenes/02_SongEditMenu.unity");
        var menu = UnityEngine.Object.FindAnyObjectByType<SongInfoEditUI>();
        var serialized = new SerializedObject(menu);
        var sound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Audio/Metronome/metronome.wav");
        var property = serialized.FindProperty("tempoMetronomeSound");
        if (property.objectReferenceValue != sound)
        {
            property.objectReferenceValue = sound;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        var iconProperty = serialized.FindProperty("tempoMapperIcon");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Graphics/Textures/UI/Editor UI/metronome.png");
        if (iconProperty.objectReferenceValue != icon)
        {
            iconProperty.objectReferenceValue = icon;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }

        var icons = serialized.FindProperty("tempoToolIcons");
        icons.arraySize = 5;
        var iconNames = new[] { "undo", "redo", "settings", "play_arrow", "pause" };
        for (var i = 0; i < iconNames.Length; i++)
        {
            var iconPath = "Assets/_Graphics/Textures/UI/" + iconNames[i] + ".png";
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null) throw new Exception("Missing toolbar icon: " + iconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            var pixels = texture.GetPixels32();
            var recolored = false;
            for (var pixel = 0; pixel < pixels.Length; pixel++)
            {
                if (pixels[pixel].a == 0 || pixels[pixel].r == 255) continue;
                pixels[pixel].r = pixels[pixel].g = pixels[pixel].b = 255;
                recolored = true;
            }
            if (recolored)
            {
                var white = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                white.SetPixels32(pixels);
                white.Apply();
                File.WriteAllBytes(iconPath, white.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(white);
                AssetDatabase.ImportAsset(iconPath);
            }
            icons.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }

        var buttons = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool Calls(UnityEngine.UI.Button button, string method) => Enumerable.Range(0, button.onClick.GetPersistentEventCount())
            .Any(i => button.onClick.GetPersistentMethodName(i) == method);
        var back = buttons.First(b => Calls(b, "ReturnToSongList") && ((RectTransform)b.transform).sizeDelta.x <= 30);
        var save = buttons.First(b => Calls(b, "SaveToSong"));
        var slider = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/Settings/Slider Container.prefab")
            .GetComponentInChildren<UnityEngine.UI.Slider>(true);
        var resetImage = serialized.FindProperty("revertInfoButtonImage").objectReferenceValue as UnityEngine.UI.Image;
        serialized.FindProperty("tempoResetIcon").objectReferenceValue = resetImage != null ? resetImage.sprite : null;
        serialized.FindProperty("tempoBackTemplate").objectReferenceValue = back;
        serialized.FindProperty("tempoSaveTemplate").objectReferenceValue = save;
        serialized.FindProperty("tempoSliderTemplate").objectReferenceValue = slider;
        if (serialized.ApplyModifiedPropertiesWithoutUndo()) EditorSceneManager.SaveScene(scene);

        var previousBackend = PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone);
        try
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            // This project disables automatic Addressables builds. Locales and string tables must ship with the player.
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var addressables);
            if (!string.IsNullOrEmpty(addressables.Error))
                throw new Exception("Addressables build failed: " + addressables.Error);
            var output = Path.GetFullPath("Builds/TempoMapper/ChroMapper.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = output,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("ChroMapper build failed: " + report.summary.result);
            var notices = Path.Combine(Path.GetDirectoryName(output), "ThirdPartyNotices", "MaterialDesignIcons");
            Directory.CreateDirectory(notices);
            foreach (var name in new[] { "LICENSE.txt", "NOTICE.txt" })
                File.Copy("Assets/_Graphics/Textures/UI/TempoMapper/" + name, Path.Combine(notices, name), true);
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, previousBackend);
        }
        Debug.Log("Tempo mapping build: " + Path.GetFullPath("Builds/TempoMapper/ChroMapper.exe"));
    }
}
