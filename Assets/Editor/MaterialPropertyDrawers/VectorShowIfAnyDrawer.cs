using UnityEditor;
using UnityEngine;

public class VectorShowIfAnyDrawer : ShowIfAnyDrawer
{
    private readonly int dimensions;

    public VectorShowIfAnyDrawer(float dims) => dimensions = (int)dims;
    public VectorShowIfAnyDrawer(float dims, params string[] keywords) : base(keywords) => dimensions = (int)dims;

    public VectorShowIfAnyDrawer(float dims, float required, params string[] keywords) : base(required, keywords) =>
        dimensions = (int)dims;

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (!IsVisible(prop)) return;

        var val = prop.vectorValue;

        EditorGUI.BeginChangeCheck();
        switch (dimensions)
        {
            case 2:
                {
                    var v = EditorGUI.Vector2Field(position, label, val);
                    if (EditorGUI.EndChangeCheck()) prop.vectorValue = new Vector4(v.x, v.y, val.z, val.w);
                    break;
                }
            case 3:
                {
                    var v = EditorGUI.Vector3Field(position, label, val);
                    if (EditorGUI.EndChangeCheck()) prop.vectorValue = new Vector4(v.x, v.y, v.z, val.w);
                    break;
                }
            default:
                editor.DefaultShaderProperty(position, prop, label);
                break;
        }
    }
}
