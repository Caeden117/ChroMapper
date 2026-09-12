using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class EnvironmentBuildPopulate
{
    private const string editorPath = "Assets/Editor/Environments";
    private const string environmentPath = "Assets/__Scenes/Environments";

    [MenuItem("Environment/Populate Build Data", false, 800)]
    private static void PopulateBuildData()
    {
        // AssetDatabase always reports forward-slash paths, including on Windows.
        var envDataPaths = AssetDatabase
            .GetAllAssetPaths()
            .Where(x => x.StartsWith(PathUtils.Combine(environmentPath, "Data")) && x.EndsWith(".json"))
            .ToList();

        // Abort before marking entries unused so a path regression cannot silently empty the generated libraries.
        if (envDataPaths.Count == 0)
        {
            const string message =
                "Populate Build Data found no environment JSON assets; generated libraries were not changed.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        // Unity asset loading requires normalized project-relative paths on every host platform.
        var library =
            AssetDatabase.LoadAssetAtPath<EnvironmentLibrarySO>(
                PathUtils.Combine(editorPath, "EnvironmentLibrarySO.asset"));

        // Fail explicitly instead of producing a partial refresh when the library asset cannot be resolved.
        if (library == null)
        {
            const string message = "Populate Build Data could not load EnvironmentLibrarySO.asset.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        // Validate every source before mutating generated assets so one unreadable file cannot leave a partial refresh.
        var environmentData = new List<EnvironmentData>(envDataPaths.Count);
        foreach (var dataPath in envDataPaths)
        {
            var dataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(dataPath);
            if (dataAsset == null)
            {
                var message = $"Populate Build Data could not load '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            var data = CreateUtils.JsonToEnvironmentData(dataAsset);
            if (data?.Data == null)
            {
                var message = $"Populate Build Data could not deserialize '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            if ((data.Data.UniqueTextures ?? Array.Empty<EnvironmentInfoTexture>())
                .Any(texture => texture == null || string.IsNullOrWhiteSpace(texture.Hash)))
            {
                var message = $"Populate Build Data found an invalid texture entry in '{dataPath}'.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            environmentData.Add(data);
        }

        library.Meshes.MarkForChange();
        library.Materials.MarkForChange();
        library.Textures.MarkForChange();
        library.Sprites.MarkForChange();
        foreach (var s in library.Shaders) s.keywords.Clear();

        foreach (var data in environmentData)
        {
            Debug.Log($"Populating data from {data.Data.ID}");

            foreach (var m in data.Data.UniqueMeshes) library.Meshes.AddEntry(m, data.Data.ID);
            foreach (var m in data.Data.UniqueMaterials)
            {
                if (IsInternalErrorMaterial(m.Name, m.Shader)) continue;

                library.Materials.AddEntry(m, data.Data.ID);
                if (library.Shaders.All(s => s.name != m.Shader))
                    library.Shaders.Add(new ShaderEntry { name = m.Shader });
                var keywords = library.Shaders.Find(x => x.name == m.Shader).keywords;
                keywords.AddRange(m.Keywords.Where(x => !keywords.Contains(x)));
            }

            foreach (var t in data.Data.UniqueTextures ?? Array.Empty<EnvironmentInfoTexture>())
                library.Textures.AddEntry(t.Hash, t.Name, data.Data.ID);

            foreach (var o in data.Objects.Where(x => x.Components.SpriteRenderer != null))
            {
                var t = o.Components.SpriteRenderer;
                foreach (var r in t)
                {
                    if (string.IsNullOrEmpty(r.Texture))
                    {
                        Debug.LogWarning($"Could not get sprite in {o.ChromaID}");
                        continue;
                    }

                    library.Sprites.AddEntry(r.Texture, data.Data.ID);
                }
            }

            foreach (var layerName in data.Objects.Select(x => x.Layer))
                library.LayerMaskLookup.TryAdd(layerName, LayerMask.GetMask("Default"));
        }

        library.Meshes.RemoveUnused();
        library.Materials.RemoveUnused();
        library.Textures.RemoveUnused();
        library.Sprites.RemoveUnused();

        library.Meshes.Sort();
        library.Materials.Sort();
        library.Textures.Sort();
        library.Sprites.Sort();
        // Rebuild runtime lookups now so Create All from Data can run correctly in the same Unity session.
        library.Meshes.RebuildLookup();
        library.Materials.RebuildLookup();
        library.Textures.RebuildLookup();
        library.Sprites.RebuildLookup();
        // Report unresolved references explicitly; null entries are metadata-only and cannot render.
        var resolvedMeshCount = library.Meshes.Lookup.Values.Count(x => x != null);
        if (resolvedMeshCount == 0)
        {
            const string message = "Populate Build Data produced no usable mesh references.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        var resolvedTextureCount = library.Textures.list.Count(x => x?.Texture != null);
        var unresolvedTextureCount = library.Textures.list.Count - resolvedTextureCount;
        if (unresolvedTextureCount > 0)
            Debug.LogWarning(
                $"Populate Build Data found {unresolvedTextureCount}/{library.Textures.list.Count} texture entries without a mapped Unity texture. Material texture properties using these hashes will retain their current values.");

        foreach (var s in library.Shaders)
            s.keywords.Sort((a, b) => string.Compare(a.Replace("_", ""), b.Replace("_", ""), StringComparison.Ordinal));
        library.Shaders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        library.layerMaskRemap =
            library
                .LayerMaskLookup
                .Select(x => new LayerMaskEntry { name = x.Key, layerMask = x.Value })
                .OrderBy(x => x.name)
                .ToList();

        var usedMaterialName = new Dictionary<string, int>();
        var collidingMaterialHashes = library
            .Materials.list
            .Where(source => source?.Materials != null)
            .SelectMany(source => source.Materials.Where(variant => variant != null).Select(_ => source.Hash))
            .GroupBy(hash => hash)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet();
        var keptUnmappedShaderCount = 0;
        foreach (var source in library.Materials.list.Where(source => source?.Materials != null))
        {
            foreach (var variant in source.Materials.Where(variant => variant != null))
            {
                if (variant.Material == null)
                {
                    var shader = Shader.Find("ChroMapper/Missing");
                    if (TryGetShader(library.Shaders, source.Shader, out var existingShader)) shader = existingShader;

                    var mat = new Material(shader) { enableInstancing = true };

                    var suffix = variant.Hash?.Substring(0, Math.Min(12, variant.Hash.Length)) ?? "unknown";
                    var baseName = collidingMaterialHashes.Contains(source.Hash)
                        ? $"{source.Name}_{suffix}"
                        : source.Name;
                    var name = usedMaterialName.TryGetValue(baseName, out var n) && n > 0
                        ? baseName + n
                        : baseName;
                    var environments = variant.Environments ?? new List<string>();
                    if (environments.Count == 0)
                        throw new InvalidOperationException($"Material '{source.Hash}' has no associated environment.");
                    if (environments.Count > 1)
                    {
                        var targetPath = PathUtils.Combine(Constants.MaterialsPath, $"{name}.mat");
                        if (!AssetDatabase.AssetPathExists(targetPath))
                            AssetDatabase.CreateAsset(mat, targetPath);
                        else
                            mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                    }
                    else
                    {
                        var parentPath = Constants.MaterialsPath;
                        var env = environments[0].Replace("Environment", "");
                        var folderPath = PathUtils.Combine(parentPath, env);
                        if (!AssetDatabase.AssetPathExists(folderPath)) AssetDatabase.CreateFolder(parentPath, env);

                        var targetPath = PathUtils.Combine(folderPath, $"{name}.mat");
                        if (!AssetDatabase.AssetPathExists(targetPath))
                            AssetDatabase.CreateAsset(mat, targetPath);
                        else
                            mat = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
                    }

                    usedMaterialName.TryAdd(baseName, 0);
                    usedMaterialName[baseName]++;
                    variant.Material = mat;
                }

                if (TryGetShader(library.Shaders, source.Shader, out var mappedShader))
                {
                    if (variant.Material.shader != mappedShader) variant.Material.shader = mappedShader;
                }
                else
                {
                    keptUnmappedShaderCount++;
                }

                MaterialProcessor.HandleProp(library, variant);
            }
        }

        // Report unresolvable shader mappings once instead of warning per material.
        if (keptUnmappedShaderCount > 0)
            Debug.LogWarning(
                $"Populate Build Data kept the existing shader on {keptUnmappedShaderCount} material(s) because no mapped shader resolved for their environment data. Open EnvironmentLibrarySO in the Inspector and assign a ChroMapper shader to each entry.");

        foreach (var obj in library
            .Materials.list
            .Where(source => source?.Materials != null)
            .SelectMany(source => source.Materials)
            .Where(variant => variant?.Material != null)
            .Select(variant => (Object)variant.Material)
            .Append(library)
            .Append(library.Materials)
            .Append(library.Meshes)
            .Append(library.Textures)
            .Append(library.Sprites))
            EditorUtility.SetDirty(obj);
        library.Materials.RebuildLookup();
        var resolvedMaterialCount = library.Materials.ResolvedMaterialCount;
        Debug.Log(
            $"Populated environment libraries: {resolvedMeshCount}/{library.Meshes.list.Count} meshes and "
            + $"{resolvedMaterialCount}/{library.Materials.MaterialVariantCount} materials and "
            + $"{resolvedTextureCount}/{library.Textures.list.Count} textures resolved.");
        if (resolvedMaterialCount == 0)
        {
            const string message = "Populate Build Data produced no usable material references.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        AssetDatabase.SaveAssets();
    }

    private static bool IsInternalErrorMaterial(string materialName, string shaderName) =>
        string.Equals(shaderName, "Hidden/InternalErrorShader", StringComparison.Ordinal)
        || materialName?.StartsWith("Hidden/InternalErrorShader", StringComparison.Ordinal) == true;

    private static bool TryGetShader(List<ShaderEntry> list, string shaderName, out Shader shader)
    {
        shader = null;
        var entry = list.FirstOrDefault(x => x.name == shaderName);
        if (entry?.shader == null) return false;

        shader = entry.shader;
        return true;
    }
}
