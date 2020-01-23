using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BetterToggle))]
public class ToggleBuilder : Editor
{
    private bool showHiddenSettings;
    public override void OnInspectorGUI()
    {
        BetterToggle toggle = (BetterToggle) target;

        toggle.description.text = EditorGUILayout.TextField("Toggle Description", toggle.description.text);

        toggle.OnColor = EditorGUILayout.ColorField("Toggle On Color", toggle.OnColor);
        toggle.OffColor = EditorGUILayout.ColorField("Toggle Off Color", toggle.OffColor);

        toggle.background.color = toggle.IsOn ? toggle.OnColor : toggle.OffColor;

        showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
        if(showHiddenSettings) base.OnInspectorGUI();
    }
}