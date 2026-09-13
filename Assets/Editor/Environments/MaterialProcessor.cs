using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class MaterialProcessor
{
    private static readonly Dictionary<string, string> propRemap = new()
    {
        { "_DiffuseTexture", "_DiffuseTex" },
        { "_DiffuseTexture_ST", "_DiffuseTex_ST" },
        { "_NormalTexture", "_NormalTex" },
        { "_NormalTexture_ST", "_NormalTex_ST" },
        { "_BlendSrcFactor", "_BlendModeSrc" },
        { "_BlendDstFactor", "_BlendModeDst" },
        { "_BlendSrcFactorA", "_BlendModeSrcA" },
        { "_BlendDstFactorA", "_BlendModeDstA" },
        { "_WhiteBoostMultiplier", "_BloomWhiteMultiplier" },
        { "_AngleDisappear", "_EnableEmissionAngleDisappear" },
        { "_ThresholdAngle", "_EmissionThresholdAngle" },
        { "_Cull", "_CullMode" },
        { "_EmissionTexWhiteboostMultiplier", "_EmissionTexWhiteBoostMultiplier" },
        { "_WhiteboostRemapStart", "_WhiteBoostRemapStart" },
        { "_Rotate_UV", "_RotateUV" },
        { "_RimCameraDistanceOffset", "_RimDistanceOffset" },
        { "_RimCameraDistanceScale", "_RimDistanceScale" },
        { "_DissolveGradientWidth", "_DissolveScale" },
        { "_InvertDissolve", "_DissolveReverse" },
        { "_FinalAlphaOverride", "_OverrideFinalAlpha" },
        { "_CustomZWrite", "_ZWrite" },
        { "_CloseToCameraOffset", "_CloseCameraDisappearDistance" },
        { "_CloseToCameraFactor", "_CloseCameraDisappearWidth" },
        { "_UseMipmapBias", "_EnableMipmapBias" },
        { "_FakeMirrorTransparencyEnabled", "_EnableFakeMirrorTransparency" },
        { "_FakeMirrorTransparencyMultiplier", "_FakeMirrorTransparency" },
        { "_UseColor_Gradient", "_UseColorGradient" },
        { "_WorldspacePanning", "_EnableWorldSpacePanning" },
        { "_MainTexWorldspacePanning", "_EnableMainTexWorldSpacePanning" }
    };

    private static readonly Dictionary<string, string> keywordRemap = new()
    {
        { "ENABLE_FOG", "FOG" },
        { "ENABLE_NOISE_DITHERING", "NOISE_DITHERING" },
        { "ENABLE_WORLD_NOISE", "WORLD_NOISE" },
        { "ENABLE_DIRT", "DIRT" },
        { "ENABLE_HEIGHT_FOG", "HEIGHT_FOG" },
        { "ENABLE_ANGLE_DISAPPEAR", "ANGLE_DISAPPEAR" },
        { "ENABLE_WORLD_SPACE_FADE", "WORLD_SPACE_FADE" },
        { "ENABLE_Y_AXIS_BILLBOARD", "Y_AXIS_BILLBOARD" },
        { "ENABLE_CUTOUT", "CUTOUT" },
        { "ENABLE_CLIPPING", "CLIPPING" },
        { "ENABLE_MAIN_EFFECT_WHITE_BOOST", "MAIN_EFFECT_WHITE_BOOST" },
        { "ENABLE_EMISSION_ANGLE_DISAPPEAR", "EMISSION_ANGLE_DISAPPEAR" },
        { "ENABLE_RIM_DIM", "RIM_DIM" }
    };

    /// <summary>Applies a generated material variant's serialized properties and keywords.</summary>
    public static void HandleProp(EnvironmentLibrarySO library, MaterialVariant matInfo)
    {
        var material = matInfo.Material;
        if (material == null) return;

        if (matInfo.FloatProps != null)
            foreach (var prop in matInfo.FloatProps)
            {
                if (TryGetPropertyName(material, prop.Key, out var name)) material.SetFloat(name, prop.Value);
            }

        if (matInfo.VectorProps != null)
            foreach (var prop in matInfo.VectorProps)
                if (TryGetPropertyName(material, prop.Key, out var name))
                    material.SetVector(name, prop.Value);

        if (matInfo.TextureProps != null)
            foreach (var prop in matInfo.TextureProps)
            {
                if (string.IsNullOrWhiteSpace(prop.Value)
                    || prop.Value.Equals("null", StringComparison.OrdinalIgnoreCase)
                    || prop.Value.Equals("none", StringComparison.OrdinalIgnoreCase)
                    || !TryGetPropertyName(material, prop.Key, out var name)
                    || !library.Textures.Lookup.TryGetValue(prop.Value.ToLowerInvariant(), out var texture))
                    continue;

                material.SetTexture(name, texture);
            }

        SynchronizeKeywords(material, matInfo.Keywords);
    }

    private static void SynchronizeKeywords(Material material, IEnumerable<string> keywords)
    {
        if (material == null || material.shader == null) return;

        // The source keyword list is authoritative. Overridable keywords are
        // runtime routes such as bloom fog, depth, post bloom, and stereo.
        var keywordSpace = material.shader.keywordSpace;
        foreach (var keyword in keywordSpace.keywords)
        {
            if (!keyword.isOverridable) material.DisableKeyword(keyword);
        }

        if (keywords == null) return;
        foreach (var sourceKeyword in keywords)
        {
            var keyword = keywordSpace.FindKeyword(sourceKeyword);
            if (!keyword.isValid && keywordRemap.TryGetValue(sourceKeyword, out var remappedKeyword))
                keyword = keywordSpace.FindKeyword(remappedKeyword);
            if (keyword.isValid && !keyword.isOverridable) material.EnableKeyword(keyword);
        }
    }

    private static bool TryGetPropertyName(
        Material material,
        string sourceName,
        out string propertyName)
    {
        var remappedName = propRemap.GetValueOrDefault(sourceName, sourceName);
        if (HasProperty(material, remappedName))
        {
            propertyName = remappedName;
            return true;
        }

        if (HasProperty(material, sourceName))
        {
            propertyName = sourceName;
            return true;
        }

        propertyName = null;
        return false;
    }

    private static bool HasProperty(Material material, string propertyName)
    {
        if (material.HasProperty(propertyName)) return true;
        return propertyName.EndsWith("_ST", StringComparison.Ordinal) && material.HasProperty(propertyName[..^3]);
    }
}
