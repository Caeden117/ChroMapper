using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BetterInputField))]
[CanEditMultipleObjects]
public class InputFieldBuilder : Editor
{
    private BetterInputField inputField;
    private bool showHiddenSettings;

    private void OnEnable() => inputField = (BetterInputField)target;

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            inputField.Description.text = EditorGUILayout.TextField("Description", inputField.Description.text);

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(inputField);
                EditorUtility.SetDirty(inputField.Description);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}
