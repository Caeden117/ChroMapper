using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

[CustomEditor(typeof(BetterInputField)), CanEditMultipleObjects]
public class InputFieldBuilder : Editor
{
    private bool showHiddenSettings = false;
    
    private BetterInputField _inputField;
    
    private void OnEnable()
    {
        _inputField = (BetterInputField) target;
    }

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            _inputField._description.text = EditorGUILayout.TextField("Description", _inputField._description.text);

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_inputField);
                EditorUtility.SetDirty(_inputField._description);
            }
        }
        catch (NullReferenceException e)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}