using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Environment/Environment Material", fileName = "EnvironmentMaterialSO")]
public class EnvironmentMaterialSO : ScriptableObject
{
    [SerializeField] public List<MaterialInfo> list = new();

    private readonly Dictionary<string, List<MaterialInfo>> lookup = new();

    public void OnValidate() => Initialize();
    public void OnEnable() => Initialize();

    // Refresh callers need the runtime lookup immediately, without waiting for Unity to re-enable the asset.
    public void RebuildLookup() => Initialize();

    /// <summary>Gets the number of generated material variants with a Unity material asset.</summary>
    public int ResolvedMaterialCount => list?
        .Where(source => source?.Materials != null)
        .SelectMany(source => source.Materials)
        .Count(variant => variant?.Material != null) ?? 0;

    /// <summary>Gets the number of generated material variants.</summary>
    public int MaterialVariantCount => list?
        .Where(source => source?.Materials != null)
        .SelectMany(source => source.Materials)
        .Count(variant => variant != null) ?? 0;

    /// <summary>Returns whether an environment has at least one resolved material asset.</summary>
    public bool HasResolvedMaterials(string environment) =>
        lookup.Values
            .SelectMany(sources => sources)
            .Where(source => source?.Materials != null)
            .SelectMany(source => source.Materials)
            .Any(variant => variant?.Environments?.Contains(environment) == true && variant.Material != null);

    public void Initialize()
    {
        lookup.Clear();
        if (list == null) return;

        var environmentHashes = new Dictionary<string, HashSet<string>>();
        foreach (var source in list)
        {
            if (source == null || source.Unused || string.IsNullOrEmpty(source.Hash)) continue;

            if (!lookup.TryGetValue(source.Hash, out var sources))
            {
                sources = new List<MaterialInfo>();
                lookup.Add(source.Hash, sources);
            }
            sources.Add(source);

            if (source.Materials == null) continue;
            foreach (var variant in source.Materials)
            {
                if (variant == null || variant.Unused || variant.Environments == null) continue;
                foreach (var environment in variant.Environments)
                {
                    if (string.IsNullOrEmpty(environment)) continue;
                    if (!environmentHashes.TryGetValue(environment, out var hashes))
                    {
                        hashes = new HashSet<string>();
                        environmentHashes.Add(environment, hashes);
                    }

                    if (!hashes.Add(source.Hash))
                        throw new InvalidOperationException(
                            $"Environment '{environment}' has multiple material definitions for hash '{source.Hash}'.");
                }
            }
        }
    }

    public void MarkForChange()
    {
        if (list == null) return;

        foreach (var source in list)
        {
            if (source == null) continue;
            source.Unused = true;
            if (source.Materials == null) continue;

            foreach (var variant in source.Materials)
            {
                if (variant == null) continue;
                variant.Unused = true;
                variant.Environments ??= new List<string>();
                variant.Environments.Clear();
                variant.Keywords ??= new List<string>();
                variant.Keywords.Clear();
            }
        }
    }

    public void RemoveUnused()
    {
        if (list == null) return;

        foreach (var source in list)
        {
            source?.Materials?.RemoveAll(variant =>
                variant == null || variant.Unused || variant.Environments == null || variant.Environments.Count == 0);
        }
        list.RemoveAll(source => source == null || source.Unused || source.Materials == null || source.Materials.Count == 0);
    }

    /// <summary>
    /// Adds an environment material definition. A source hash can have several variants, but only one per environment.
    /// </summary>
    public void AddEntry(EnvironmentInfoMaterial material, string environment)
    {
        if (material == null) throw new ArgumentNullException(nameof(material));
        if (string.IsNullOrEmpty(environment)) throw new ArgumentException("Environment ID is required.", nameof(environment));

        list ??= new List<MaterialInfo>();
        if (list.Any(source => source != null
            && !source.Unused
            && source.Hash == material.Hash
            && source.Materials?.Any(variant => !variant.Unused && variant.Environments?.Contains(environment) == true) == true))
            throw new InvalidOperationException(
                $"Environment '{environment}' has multiple material definitions for hash '{material.Hash}'.");

        var source = list.FirstOrDefault(candidate => candidate != null
            && candidate.Hash == material.Hash);
        if (source == null)
        {
            source = new MaterialInfo
            {
                Name = material.Name,
                Hash = material.Hash,
                Shader = material.Shader,
                Materials = new List<MaterialVariant>()
            };
            list.Add(source);
        }
        else if (source.Name != material.Name || source.Shader != material.Shader)
        {
            throw new InvalidOperationException(
                $"Material hash '{material.Hash}' has conflicting source metadata: "
                + $"'{source.Name}'/'{source.Shader}' and '{material.Name}'/'{material.Shader}'.");
        }
        else
        {
            // Revive the existing entry so RemoveUnused keeps reused materials
            // instead of discarding them and forcing full regeneration.
            source.Unused = false;
        }

        source.Materials ??= new List<MaterialVariant>();
        var variantHash = GetVariantHash(material);
        var variant = source.Materials.FirstOrDefault(candidate =>
            candidate != null && candidate.Hash == variantHash);
        if (variant == null)
        {
            source.Materials.Add(CreateVariant(material, environment, variantHash));
            return;
        }

        variant.Unused = false;
        variant.Keywords ??= new List<string>();
        if (material.Keywords != null)
            variant.Keywords.AddRange(material.Keywords.Where(keyword => !variant.Keywords.Contains(keyword)));
        variant.Environments ??= new List<string>();
        if (!variant.Environments.Contains(environment)) variant.Environments.Add(environment);
    }

    /// <summary>Gets a material by its source environment ID and original JSON hash.</summary>
    public Material GetSafe(string environment, string hash) =>
        TryGetMaterial(environment, hash, out var material) ? material : null;

    /// <summary>Tries to get a material by its source environment ID and original JSON hash.</summary>
    public bool TryGetMaterial(string environment, string hash, out Material material)
    {
        material = null;
        if (string.IsNullOrEmpty(environment)
            || string.IsNullOrEmpty(hash)
            || hash.Equals("null", StringComparison.OrdinalIgnoreCase)
            || !lookup.TryGetValue(hash, out var sources))
            return false;

        var variants = sources
            .Where(source => source.Materials != null)
            .SelectMany(source => source.Materials)
            .Where(variant => variant != null && !variant.Unused && variant.Environments?.Contains(environment) == true)
            .ToList();
        if (variants.Count > 1)
            throw new InvalidOperationException(
                $"Environment '{environment}' has multiple material definitions for hash '{hash}'.");
        if (variants.Count == 0) return false;

        material = variants[0].Material;
        return true;
    }

    public void Sort()
    {
        if (list == null) return;

        foreach (var source in list)
        {
            if (source?.Materials == null) continue;
            source.Materials = source.Materials
                .Where(variant => variant != null)
                .OrderBy(variant => variant.Hash, StringComparer.Ordinal)
                .ToList();
        }
        list = list
            .Where(source => source != null)
            .OrderBy(source => source.Name, StringComparer.Ordinal)
            .ThenBy(source => source.Hash, StringComparer.Ordinal)
            .ThenBy(source => source.Shader, StringComparer.Ordinal)
            .ToList();
    }

    private static MaterialVariant CreateVariant(
        EnvironmentInfoMaterial material,
        string environment,
        string variantHash)
    {
        var shaderProps = material.ShaderProps ?? new Dictionary<string, dynamic>();
        return new MaterialVariant
        {
            Hash = variantHash,
            Keywords = material.Keywords?.ToList() ?? new List<string>(),
            FloatProps = shaderProps
                .Where(entry => IsNumeric(entry.Value))
                .Select(entry => new MaterialVariant.ShaderProps<float>
                {
                    Key = entry.Key, Value = Convert.ToSingle(entry.Value, CultureInfo.InvariantCulture)
                })
                .ToList(),
            VectorProps = shaderProps
                .Where(entry => entry.Value is JArray)
                .Select(entry => new MaterialVariant.ShaderProps<Vector4>
                {
                    Key = entry.Key, Value = GetVector4(((JArray)entry.Value).ToObject<float[]>())
                })
                .ToList(),
            TextureProps = shaderProps
                .Where(entry => entry.Value is string value && !IsMissingTexture(value))
                .Select(entry => new MaterialVariant.ShaderProps<string> { Key = entry.Key, Value = entry.Value })
                .ToList(),
            Environments = new List<string> { environment }
        };
    }

    private static string GetVariantHash(EnvironmentInfoMaterial material)
    {
        var shaderProperties = new JObject(
            (material.ShaderProps ?? new Dictionary<string, dynamic>())
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new JProperty(entry.Key, Canonicalize((object)entry.Value))));
        var variant = new JObject
        {
            ["name"] = ToToken(material.Name),
            ["shaderProperties"] = shaderProperties
        };

        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(variant.ToString(Formatting.None)));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static JToken Canonicalize(object value)
    {
        if (value == null) return JValue.CreateNull();
        return Canonicalize(value as JToken ?? JToken.FromObject(value));
    }

    private static JToken Canonicalize(JToken token) => token switch
    {
        JObject obj => new JObject(obj.Properties()
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => new JProperty(property.Name, Canonicalize(property.Value)))),
        JArray array => new JArray(array.Select(Canonicalize)),
        _ => token.DeepClone()
    };

    private static JToken ToToken(string value) => value == null ? JValue.CreateNull() : new JValue(value);

    private static bool IsNumeric(object value) =>
        value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static bool IsMissingTexture(string value) =>
        value.Equals("null", StringComparison.OrdinalIgnoreCase)
        || value.Equals("none", StringComparison.OrdinalIgnoreCase);

    private static Vector4 GetVector4(float[] value) => new(value[0], value[1], value[2], value[3]);
}

[Serializable]
public class MaterialInfo
{
    public string Name;
    public string Hash;
    public string Shader;
    public List<MaterialVariant> Materials;

    [HideInInspector]
    public bool Unused;

    [HideInInspector] public bool Ignored;
}

[Serializable]
public class MaterialVariant
{
    public Material Material;
    public string Hash;
    public List<string> Keywords;
    public List<ShaderProps<float>> FloatProps;
    public List<ShaderProps<Vector4>> VectorProps;
    public List<ShaderProps<string>> TextureProps;
    public List<string> Environments;

    [HideInInspector]
    public bool Unused;

    [Serializable]
    public class ShaderProps<T>
    {
        public string Key;
        public T Value;
    }
}
