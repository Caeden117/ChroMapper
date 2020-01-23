using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ButtonHelper))]
public class ButtonManager : Editor
{

    private bool showHiddenSettings;
    
    public override void OnInspectorGUI()
    {
        ButtonHelper button = (ButtonHelper) target;

        string tagName = EditorGUILayout.TextField("Tab Name", button.textMeshTabName.text);
        button.textMeshTabName.text = tagName;
        button.gameObject.name = button.textMeshTabName.text + " Tab";
        
        button.icon.sprite = (Sprite) EditorGUILayout.ObjectField("Icon", button.icon.sprite, typeof(Sprite), true);

        Vector2 discordPopoutSize = button.discordPopout.sizeDelta;
        button.discordPopout.sizeDelta = new Vector2(EditorGUILayout.FloatField("Popout Width", discordPopoutSize.x), discordPopoutSize.y);

        EditorGUILayout.Separator();
        showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
        if(showHiddenSettings) base.OnInspectorGUI();
    }
}