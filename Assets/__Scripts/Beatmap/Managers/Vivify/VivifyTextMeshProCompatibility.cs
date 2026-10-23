using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Prepares bundled TMP materials for the current text vertex layout before prefab activation.</summary>
internal static class VivifyTextMeshProCompatibility
{
    private static readonly Dictionary<string, Shader> shaders = new();

    internal static void Prepare(TMP_Text text)
    {
        if (text.font == null) return;
        // Older bundles serialize "material", while Unity 6 reads "m_Material" (UUM-114451).
        if (text.font.material == null && text.fontSharedMaterial != null)
            text.font.material = text.fontSharedMaterial;
        PrepareMaterial(text.font.material);
        PrepareMaterial(text.fontSharedMaterial);
    }

    internal static void PrepareMaterial(Material material)
    {
        if (material == null || material.shader == null) return;
        var name = material.shader.name;
        var resource = name switch
        {
            "TextMeshPro/Distance Field" => "DistanceField",
            "TextMeshPro/Mobile/Distance Field" => "MobileDistanceField",
            _ => null
        };
        if (resource == null) return;
        if (!shaders.TryGetValue(name, out var shader) || shader == null)
        {
            // Explicit asset references avoid Shader.Find selecting the bundle's same-named legacy shader.
            var reference = Resources.Load<Material>("VivifyTextMeshPro/" + resource);
            if (reference == null) throw new InvalidOperationException("Vivify TMP shader reference is missing: " + resource);
            shaders[name] = shader = reference.shader;
        }
        if (material.shader == shader) return;
        var keywords = material.shaderKeywords;
        var queue = material.rawRenderQueue;
        // Keep the authored atlas, bevel/outline/glow values, material identity and event bindings.
        material.shader = shader;
        material.shaderKeywords = keywords;
        material.renderQueue = queue;
    }
}
