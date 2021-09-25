using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OptionsTabButton))]
public class ButtonManager : Editor
{
    private OptionsTabButton tabButton;
    private bool showHiddenSettings;

    private void OnEnable()
    {
        tabButton = (OptionsTabButton)target;
        tabButton.DiscordPopout.localScale = new Vector3(1, 1, 1);
    }

    private void OnDisable() => tabButton.DiscordPopout.localScale = new Vector3(0, 1, 1);

    public override void OnInspectorGUI()
    {
        try
        {
            var tagName = EditorGUILayout.TextField("Tab Name", tabButton.TextMeshTabName.text);
            tabButton.TextMeshTabName.text = tagName;
            tabButton.gameObject.name = tabButton.TextMeshTabName.text + " Tab";

            tabButton.Icon.sprite =
                (Sprite)EditorGUILayout.ObjectField("Icon", tabButton.Icon.sprite, typeof(Sprite), true);

            var discordPopoutSize = tabButton.DiscordPopout.sizeDelta;
            tabButton.DiscordPopout.sizeDelta =
                new Vector2(tabButton.TextMeshTabName.preferredWidth + 5, discordPopoutSize.y);

            EditorGUILayout.Space();
            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(tabButton);
                EditorUtility.SetDirty(tabButton.Icon.sprite);
                EditorUtility.SetDirty(tabButton.TextMeshTabName);
                EditorUtility.SetDirty(tabButton.DiscordPopout);
            }
        }
        catch (NullReferenceException)
        {
            EditorGUILayout.HelpBox("Error while loading custom editor, showing standard settings.", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}
