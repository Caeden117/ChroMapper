// --- instance.id ------------------------------------------------------------
// Thanks to TheZombieKiller and Peter77 for creating this
// https://forum.unity.com/threads/editor-skinning-thread.711059/#post-4785434
// Tested on Unity 2019.3.0b1 - 95% Dark mode Conversion
// Example Screenshot - https://i.imgur.com/9q5VPQk.png
// (Note - Once I ran this, I had to hit play and then it took effect)
// ----------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditorInternal;
 
public static class DarkTheme
{
    [MenuItem("Theme/Init Dark")]
    static void Init()
    {
        foreach (var sheet in Resources.FindObjectsOfTypeAll<StyleSheet>())
        {
            if (ContainsInsensitive(sheet.name, "Dark"))
                continue;
 
            if (!ContainsInsensitive(sheet.name, "Light"))
            {
                InvertColors(sheet);
                continue;
            }
 
            var dark = null as StyleSheet;
            var path = ReplaceInsensitive(AssetDatabase.GetAssetPath(sheet), "Light", "Dark");
            var name = ReplaceInsensitive(sheet.name, "Light", "Dark");
 
            if (path == "Library/unity editor resources")
                dark = EditorGUIUtility.Load(name) as StyleSheet;
            else
                dark = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
 
            if (!dark)
                InvertColors(sheet);
            else
            {
                string oldName = sheet.name;
                EditorUtility.CopySerialized(dark, sheet);
                sheet.name = oldName;
            }
        }
 
        EditorUtility.RequestScriptReload();
        InternalEditorUtility.RepaintAllViews();
    }
 
    static void InvertColors(StyleSheet sheet)
    {
        var serialized = new SerializedObject(sheet); serialized.Update();
        var colors     = serialized.FindProperty("colors");
     
        for (int i = 0; i < colors.arraySize; i++)
        {
            var property = colors.GetArrayElementAtIndex(i);
            Color.RGBToHSV(property.colorValue, out var h, out var s, out var v);
            property.colorValue = Color.HSVToRGB(h, s, 1 - v);
        }
 
        serialized.ApplyModifiedProperties();
    }
 
    static string ReplaceInsensitive(string str, string oldValue, string newValue)
    {
        return Regex.Replace(str, Regex.Escape(oldValue), newValue.Replace("$", "$$"), RegexOptions.IgnoreCase);
    }
 
    static bool ContainsInsensitive(string str, string find)
    {
        return str.IndexOf(find, StringComparison.OrdinalIgnoreCase) != -1;
    }
}