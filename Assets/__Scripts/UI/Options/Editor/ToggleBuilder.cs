using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

[CustomEditor(typeof(BetterToggle)), CanEditMultipleObjects]
public class ToggleBuilder : Editor
{
    private bool showHiddenSettings = false;
    
    private BetterToggle _toggle;
    
    private void OnEnable()
    {
        _toggle = (BetterToggle) target;
    }

    public override void OnInspectorGUI() //Why is this broken on BUILD
    {
        try
        {
            _toggle.description.text = EditorGUILayout.TextField("Toggle Description", _toggle.description.text);
            
            _toggle.OnColor = EditorGUILayout.ColorField("Toggle On Color", _toggle.OnColor);
            _toggle.OffColor = EditorGUILayout.ColorField("Toggle Off Color", _toggle.OffColor);
            
            //toggle.background.color = toggle.isOn ? toggle.OnColor : toggle.OffColor;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onValueChanged"), false);
            serializedObject.ApplyModifiedProperties();
            
            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_toggle);
                EditorUtility.SetDirty(_toggle.description);
            }
        }
        catch (NullReferenceException e)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}