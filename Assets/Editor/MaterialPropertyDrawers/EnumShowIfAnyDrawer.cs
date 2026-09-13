using System.Linq;
using UnityEditor;
using UnityEngine;

public class EnumShowIfAnyDrawer : ShowIfAnyDrawer
{
    private readonly string[] options;

    public EnumShowIfAnyDrawer(float optionCount, params string[] keywords) :
        base(keywords.Skip((int)optionCount).ToArray()) =>
        options = keywords.Take((int)optionCount).ToArray();

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        EditorGUI.BeginChangeCheck();

        var index = (int)prop.floatValue;
        index = EditorGUI.Popup(position, label, index, options);

        prop.floatValue = index;
        SetKeywords(prop, index);
        
        EditorGUI.EndChangeCheck();
    }

    public override void Apply(MaterialProperty prop)
    {
        base.Apply(prop);
        SetKeywords(prop, (int)prop.floatValue);
    }

    private void SetKeywords(MaterialProperty prop, int index)
    {
        foreach (var target in prop.targets)
        {
            var mat = (Material)target;
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i].Replace(' ', '_').ToUpperInvariant();
                if (option is "NONE" or "OFF") continue;

                var keyword = $"{prop.name.ToUpperInvariant()}_{option}";
                var localKeyword = mat.shader.keywordSpace.FindKeyword(keyword);
                if (!localKeyword.isValid) continue;

                if (i == index)
                    mat.EnableKeyword(localKeyword);
                else
                    mat.DisableKeyword(localKeyword);
            }
        }
    }
}
