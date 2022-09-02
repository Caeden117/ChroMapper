using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



[CustomEditor(typeof(PrefabEditorData))]
public class PrefabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var data = target as PrefabEditorData;
        if (GUILayout.Button("Apply"))
        {
            data.Apply();
        }
    }
}
