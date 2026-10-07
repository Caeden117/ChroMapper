using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Regens the Teko font atlas with a larger SDF padding so the edge gradient is
/// encoded over more texels. Greatly improves AA performance and clarity of font at distance.
/// </summary>
public static class RegenerateTekoFontAtlas
{
    private const string FontAssetPath = "Assets/_Graphics/Materials/Font/Teko.asset";
    private const string SourceFontPath = "Assets/_Graphics/Materials/Font/Source/Teko-Regular.ttf";
    private const int Padding = 12; // was 7; finer edge-gradient encoding - 9 tested visibly worse at distance
    private const int AtlasWidth = 2048; // was 1024x512; same charset does not fit at padding 12
    private const int AtlasHeight = 1024;
    private const string CharacterSequence = "32 - 126, 160 - 255, 8192 - 8303, 8364, 8482, 9633";

    [MenuItem("Tools/Regenerate Teko Font Atlas")]
    public static void Run()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"[RegenTeko] Font asset not found at {FontAssetPath}");
            return;
        }

        var sourceFontFullPath = Path.GetFullPath(SourceFontPath);
        if (!File.Exists(sourceFontFullPath))
        {
            Debug.LogError($"[RegenTeko] Source font not found at {sourceFontFullPath}");
            return;
        }

        // Static font assets refuse TryAddCharacters, so temporarily switch to Dynamic and point
        // LoadFontFace() at the source TTF on disk via m_SourceFontFilePath.
        var serialized = new SerializedObject(fontAsset);
        serialized.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Dynamic;
        serialized.FindProperty("m_SourceFontFilePath").stringValue = sourceFontFullPath;
        serialized.FindProperty("m_AtlasPadding").intValue = Padding;
        serialized.FindProperty("m_AtlasWidth").intValue = AtlasWidth;
        serialized.FindProperty("m_AtlasHeight").intValue = AtlasHeight;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        fontAsset.ClearFontAssetData(false);

        var unicodes = ParseSequence(CharacterSequence);
        if (!fontAsset.TryAddCharacters(unicodes.ToArray(), out var missing, true))
            Debug.LogWarning($"[RegenTeko] {missing.Length} characters did not fit or resolve");

        // Any overflow atlas textures created while Dynamic must be embedded as sub-assets.
        for (var i = 1; i < fontAsset.atlasTextures.Length; i++)
            if (!AssetDatabase.IsSubAsset(fontAsset.atlasTextures[i]))
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAsset);

        serialized = new SerializedObject(fontAsset);
        serialized.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Static;
        serialized.FindProperty("m_SourceFontFilePath").stringValue = string.Empty;

        // Make future manual regen reproduce this atlas
        var settings = serialized.FindProperty("m_CreationSettings");
        settings.FindPropertyRelative("padding").intValue = Padding;
        settings.FindPropertyRelative("atlasWidth").intValue = AtlasWidth;
        settings.FindPropertyRelative("atlasHeight").intValue = AtlasHeight;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // The standalone Teko .mat files and the font asset's embedded material store _GradientScale,
        // _TextureWidth/Height, and the TMP ScaleRatios - all derived from padding/atlas size, so they
        // must be re-synced here or the SDF scale math goes stale against the regenerated atlas.
        var gradientScale = Padding + 1;
        var materials = new List<Material>();
        if (fontAsset.material != null) materials.Add(fontAsset.material);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Graphics/Materials/Font" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!Path.GetFileName(path).StartsWith("Teko")) continue;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) materials.Add(mat);
        }

        foreach (var mat in materials)
        {
            mat.SetFloat("_GradientScale", gradientScale);
            mat.SetFloat("_TextureWidth", fontAsset.atlasTextures[0].width);
            mat.SetFloat("_TextureHeight", fontAsset.atlasTextures[0].height);
            ShaderUtilities.UpdateShaderRatios(mat);
            EditorUtility.SetDirty(mat);
        }

        EditorUtility.SetDirty(fontAsset);
        foreach (var atlas in fontAsset.atlasTextures) EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();

        Debug.Log($"[RegenTeko] glyphs={fontAsset.glyphTable.Count} characters={fontAsset.characterTable.Count} atlases={fontAsset.atlasTextures.Length} size={fontAsset.atlasTextures[0].width}x{fontAsset.atlasTextures[0].height}");
    }

    private static List<uint> ParseSequence(string sequence)
    {
        var result = new List<uint>();
        foreach (var segment in sequence.Split(','))
        {
            var parts = segment.Split('-');
            if (parts.Length == 2 && uint.TryParse(parts[0].Trim(), out var start)
                && uint.TryParse(parts[1].Trim(), out var end))
            {
                for (var u = start; u <= end; u++) result.Add(u);
            }
            else if (uint.TryParse(segment.Trim(), out var single))
            {
                result.Add(single);
            }
        }

        return result;
    }
}
