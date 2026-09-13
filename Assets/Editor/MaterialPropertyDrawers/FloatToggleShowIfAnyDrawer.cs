using UnityEditor;
using UnityEngine;

public class FloatToggleShowIfAnyDrawer : ShowIfAnyDrawer
{
    public FloatToggleShowIfAnyDrawer(params string[] keywords) : base(keywords)
    {
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        bool value = prop.floatValue != 0f;
        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        value = EditorGUI.Toggle(position, label, value);
        EditorGUI.showMixedValue = false;
        if (EditorGUI.EndChangeCheck()) prop.floatValue = value ? 1f : 0f;
    }
}
