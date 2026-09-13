using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor utility to create a new Unity scene from an EnvironmentInfo JSON file.
/// </summary>
public partial class EnvironmentSceneCreator
{
    private const string environmentPath = "Assets/__Scenes/Environments";
    private const string editorPath = "Assets/Editor/Environments";

    [MenuItem("Environment/Create from Data", false, 1000)]
    private static void CreateEnvironmentFromDataWithScript() => ReadSelectedAndCreateEnvironment(true);

    [MenuItem("Environment/Create from Data (No Script)", false, 1000)]
    private static void CreateEnvironmentFromDataWithoutScript() => ReadSelectedAndCreateEnvironment(false);

    [MenuItem("Environment/Create All from Data", false, 1000)]
    private static void CreateAllEnvironmentFromData()
    {
        // Materialize and validate the complete source set before opening or overwriting any environment scene.
        var environmentDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(PathUtils.Combine(environmentPath, "Data")) && x.EndsWith(".json"))
            .ToList();
        if (environmentDataPaths.Count == 0)
        {
            const string message = "Create All from Data found no environment JSON assets; no scenes were changed.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        foreach (var dataPath in environmentDataPaths)
        {
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            if (textAsset == null)
            {
                var message = $"Create All from Data could not load '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            CreateEnvironmentFromData(textAsset, true);
        }

        Debug.Log($"Created all {environmentDataPaths.Count} environment scenes from data.");
    }

    private static void ReadSelectedAndCreateEnvironment(bool allowScript)
    {
        var textAsset = Selection.activeObject switch
        {
            TextAsset tempTextAsset => tempTextAsset,
            SceneAsset tempSceneAsset => AssetDatabase.LoadAssetAtPath<TextAsset>(
                PathUtils.Combine(
                    Path.GetDirectoryName(AssetDatabase.GetAssetPath(tempSceneAsset))!,
                    "Data",
                    tempSceneAsset.name + ".json")),
            _ => null
        };

        if (textAsset == null)
        {
            var scenePath = SceneManager.GetActiveScene().path;
            var dir = Path.GetDirectoryName(scenePath);
            var name = Path.GetFileNameWithoutExtension(scenePath);

            var textAssetPath = PathUtils.Combine(dir, "Data", name + ".json");
            textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(textAssetPath);
        }

        // Selection mistakes should be visible instead of making the menu command appear to succeed.
        if (textAsset == null)
        {
            Debug.LogError("Create from Data could not resolve environment JSON for the selected or active scene.");
            return;
        }
        CreateEnvironmentFromData(textAsset, allowScript);
    }

    private static void CreateEnvironmentFromData(TextAsset textAsset, bool allowScript)
    {
        var assetName = textAsset.name;

        var targetPath = PathUtils.Combine(environmentPath, $"{assetName}.unity");
        var exist = AssetDatabase.AssetPathExists(targetPath);

        var scene = exist
            ? EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SceneManager.SetActiveScene(scene);

        // Save the scene with the new name (in memory, not on disk yet)
        if (!exist) scene.name = assetName;

        // Oh dear I'm loading stuff at runtime
        var environmentLibrary =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(PathUtils.Combine(editorPath, "EnvironmentLibrarySO.asset"));
        var environmentData = CreateUtils.JsonToEnvironmentData(textAsset);

        // Move null checks up here so it doesnt ruin the rest of the process
        if (environmentLibrary == null) throw new ArgumentNullException(nameof(environmentLibrary));
        if (environmentData == null) throw new ArgumentNullException(nameof(environmentData));

        // Set the skybox material if specified in the library
        if (environmentLibrary.SkyboxMaterial != null) RenderSettings.skybox = environmentLibrary.SkyboxMaterial;

        // Create the environment in the new scene
        CreateEnvironment(scene, environmentData, environmentLibrary, allowScript);

        // Save the scene to disk
        if ((exist && EditorSceneManager.SaveScene(scene)) || EditorSceneManager.SaveScene(scene, targetPath))
        {
            // Select the newly created scene in the Project window
            // EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath));
            // Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath);
        }
        else
        {
            // A failed save must stop Create All from reporting a successful environment refresh.
            var message = $"Failed to save environment scene '{targetPath}'.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }

    // Main method which constructs the environment from parsed data
    public static void CreateEnvironment(
        Scene scene,
        EnvironmentData data,
        EnvironmentLibrarySO library,
        bool allowScript)
    {
        var blacklist = new[] { "SaberBurnMarkSparklePS", "SaberBurnMarksArea", "BasicGameHUD" };
        data.Objects = data
            .Objects.Where(x => !blacklist.Any(y => x.ChromaID.Contains(y)))
            .ToList();

        var container = new CreateContainer
        {
            Data = data, Library = library, ComponentInstances = CreateContainer.CollectComponentInstances(data)
        };

        // Refuse to strip a scene when source data or generated libraries are empty after a failed refresh.
        if (data?.Objects == null || data.Objects.Count == 0)
            throw new InvalidOperationException($"Environment '{data?.Data?.ID ?? scene.name}' contains no objects.");
        // Unity libraries need explicit null checks before validating their generated mesh list.
        if (library == null || library.Meshes == null || library.Meshes.list == null || library.Meshes.list.Count == 0)
            throw new InvalidOperationException("Environment mesh library is empty; run Populate Build Data successfully first.");
        // Unity libraries need explicit null checks before validating their generated material list.
        if (library.Materials == null || library.Materials.list == null || library.Materials.list.Count == 0)
            throw new InvalidOperationException("Environment material library is empty; run Populate Build Data successfully first.");

        // Rebuild serialized-library lookups before stripping anything, including when refresh commands run back-to-back.
        library.Meshes.RebuildLookup();
        library.Materials.RebuildLookup();
        library.Sprites.RebuildLookup();
        // Stop before scene destruction if serialized entries exist but none point to usable Unity assets.
        if (!library.Meshes.Lookup.Values.Any(x => x != null))
            throw new InvalidOperationException("Environment mesh lookup contains no resolved Unity mesh assets.");
        if (!library.Materials.HasResolvedMaterials(data.Data.ID))
            throw new InvalidOperationException(
                $"Environment material lookup contains no resolved Unity material assets for '{data.Data.ID}'.");

        // first pass: strip existing object and component
        var existingObjects = StripObjects(scene, data);

        // second pass: spawn object
        container.ChromaIdObjects = SpawnObjects(container, existingObjects);

        // third pass: build component
        if (allowScript) BuildComponents(container);

        // forth pass: cleanup and remove unused
        if (allowScript) Cleanup(scene, data);

        // fifth pass
        // ReflectionProbeBakePipeline.BakeSceneReflectionProbes(scene);
    }
}
