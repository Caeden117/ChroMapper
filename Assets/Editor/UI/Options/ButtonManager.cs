using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OptionsTabButton))]
public class ButtonManager : Editor
{
    private bool showHiddenSettings = false;
    private OptionsTabButton _tabButton;

    private void OnEnable()
    {
        _tabButton = (OptionsTabButton) target;
        _tabButton.discordPopout.localScale = new Vector3(1,1,1);
    }

    private void OnDisable()
    {
        _tabButton.discordPopout.localScale = new Vector3(0,1,1);
    }

    public override void OnInspectorGUI()
    {
        try
        {
            string tagName = EditorGUILayout.TextField("Tab Name", _tabButton.textMeshTabName.text);
            _tabButton.textMeshTabName.text = tagName;
            _tabButton.gameObject.name = _tabButton.textMeshTabName.text + " Tab";
        
            _tabButton.icon.sprite = (Sprite) EditorGUILayout.ObjectField("Icon", _tabButton.icon.sprite, typeof(Sprite), true);

            Vector2 discordPopoutSize = _tabButton.discordPopout.sizeDelta;
            _tabButton.discordPopout.sizeDelta = new Vector2(_tabButton.textMeshTabName.preferredWidth + 5, discordPopoutSize.y);

            EditorGUILayout.Space();
            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(_tabButton);
                EditorUtility.SetDirty(_tabButton.icon.sprite);
                EditorUtility.SetDirty(_tabButton.textMeshTabName);
                EditorUtility.SetDirty(_tabButton.discordPopout);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}