using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ShowIfAnyDrawer : MaterialPropertyDrawer
{
    private readonly string[] requiredKeywords;
    private readonly string[] anyKeywords;

    public ShowIfAnyDrawer()
    {
        requiredKeywords = Array.Empty<string>();
        anyKeywords = Array.Empty<string>();
    }

    public ShowIfAnyDrawer(params string[] keywords)
    {
        requiredKeywords = Array.Empty<string>();
        anyKeywords = keywords;
    }

    public ShowIfAnyDrawer(float required, params string[] keywords)
    {
        requiredKeywords = keywords.Take((int)required).ToArray();
        anyKeywords = keywords.Skip((int)required).ToArray();
    }

    protected bool IsVisible(MaterialProperty prop)
    {
        if (requiredKeywords.Length == 0 && anyKeywords.Length == 0) return true;

        foreach (var obj in prop.targets)
        {
            var mat = obj as Material;
            if (mat == null) return false;
            return (requiredKeywords.Length == 0 || requiredKeywords.All(ConditionalKeyword))
                && (anyKeywords.Length == 0 || anyKeywords.Any(ConditionalKeyword));

            bool ConditionalKeyword(string keyword)
            {
                if (keyword.StartsWith('0'))
                {
                    var revKeyword = keyword[1..];
                    var count = mat.shader.GetPropertyCount();
                    for (var i = 0; i < count; i++)
                    {
                        var attributes = mat.shader.GetPropertyAttributes(i);
                        foreach (var attribute in attributes)
                        {
                            var p = attribute.IndexOf("(", StringComparison.Ordinal);
                            if (p == -1) continue;
                            var n = attribute[..p];
                            var o = attribute[(p + 1)..^1].Split(',').Select(x => x.Trim().ToUpper()).ToArray();
                            var propName = mat.shader.GetPropertyName(i).ToUpper();
                            switch (n)
                            {
                                case "KeywordEnum":
                                    if (o.Select(x => $"{propName}_{x}").Contains(revKeyword))
                                        return !mat.shaderKeywords.Contains(revKeyword);
                                    break;
                                case "Toggle":
                                    if (o[0] == revKeyword) return !mat.shaderKeywords.Contains(revKeyword);
                                    break;
                                case "EnumShowIfAny":
                                    var c = int.Parse(o.First());
                                    if (o.Skip(1).Take(c).Select(x => $"{propName}_{x.Replace(' ', '_')}")
                                        .Contains(revKeyword))
                                        return !mat.shaderKeywords.Contains(revKeyword);
                                    break;
                                case "ToggleShowIfAny":
                                    if (o[0] == revKeyword) return !mat.shaderKeywords.Contains(revKeyword);
                                    break;
                            }
                        }
                    }

                    return false;
                }

                return mat.IsKeywordEnabled(keyword);
            }
        }

        return false;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (IsVisible(prop)) editor.DefaultShaderProperty(position, prop, label);
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        return prop.propertyType switch
        {
            UnityEngine.Rendering.ShaderPropertyType.Vector => IsVisible(prop)
                ? EditorGUIUtility.wideMode
                    ? base.GetPropertyHeight(prop, label, editor)
                    : (EditorGUIUtility.singleLineHeight * 2f) + 2f
                : -2f,
            UnityEngine.Rendering.ShaderPropertyType.Texture => IsVisible(prop) ? EditorGUIUtility.singleLineHeight * 4f : -2f,
            _ => IsVisible(prop) ? base.GetPropertyHeight(prop, label, editor) : -2f
        };
    }
}
