using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BetterToggle))]
[CanEditMultipleObjects]
public class ToggleBuilder : Editor
{
    private BetterToggle toggle;
    private bool showHiddenSettings;

    private void OnEnable() => toggle = (BetterToggle)target;

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            toggle.Description.text = EditorGUILayout.TextField("Toggle Description", toggle.Description.text);

            toggle.Color = EditorGUILayout.ColorField("Toggle On Color", toggle.Color);
            toggle.OffColor = EditorGUILayout.ColorField("Toggle Off Color", toggle.OffColor);

            //toggle.background.color = toggle.isOn ? toggle.OnColor : toggle.OffColor;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onValueChanged"), false);
            serializedObject.ApplyModifiedProperties();

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(toggle);
                EditorUtility.SetDirty(toggle.Description);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}
