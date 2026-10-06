using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dynamic CJK fallback: its single 2048x1024 atlas filled up (runtime-added
/// glyphs persist because ClearDynamicDataOnBuild is off), and with multi-atlas disabled every later
/// TryAddCharacter fails and song-select names and unbaked glyphs render as nothing or the missing-
/// glyph box. Enabling multi-atlas lets TMP allocate atlas[1]+ on demand.
/// Run with: Unity -batchmode -projectPath -executeMethod EnableCjkMultiAtlas.Run -quit
/// or via the Tools menu in an open editor.
/// </summary>
public static class EnableCjkMultiAtlas
{
    private const string FontAssetPath = "Assets/_Graphics/Materials/Font/NotoSansCJKjp.asset";

    [MenuItem("Tools/Enable CJK Multi-Atlas")]
    public static void Run()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Debug.LogError($"[EnableCjkMultiAtlas] Font asset not found at {FontAssetPath}");
            return;
        }

        var serialized = new SerializedObject(fontAsset);
        var prop = serialized.FindProperty("m_IsMultiAtlasTexturesEnabled");
        if (prop == null)
        {
            Debug.LogError("[EnableCjkMultiAtlas] m_IsMultiAtlasTexturesEnabled not found");
            return;
        }

        prop.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
    }
}
