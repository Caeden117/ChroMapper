using UnityEditor;
using UnityEngine;

public class ToggleShowIfAnyDrawer : ShowIfAnyDrawer
{
    private readonly string keyword;

    public ToggleShowIfAnyDrawer(string keyword, params string[] keywords) :
        base(keywords) =>
        this.keyword = keyword.ToUpper();

    public ToggleShowIfAnyDrawer(string keyword, float required, params string[] keywords) :
        base(required, keywords) =>
        this.keyword = keyword.ToUpper();

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        var value = prop.floatValue != 0.0f;

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;

        value = EditorGUI.Toggle(position, label, value);

        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck()) prop.floatValue = value ? 1.0f : 0.0f;

        SetKeywords(prop, value);
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        SetKeywords(prop, prop.floatValue != 0.0f);
    }

    private void SetKeywords(MaterialProperty prop, bool active)
    {
        foreach (var target in prop.targets)
        {
            var mat = (Material)target;
            var localKeyword = mat.shader.keywordSpace.FindKeyword(keyword);
            if (!localKeyword.isValid) continue;

            if (active)
                mat.EnableKeyword(localKeyword);
            else
                mat.DisableKeyword(localKeyword);
        }
    }
}
