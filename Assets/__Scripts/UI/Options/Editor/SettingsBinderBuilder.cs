using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(SettingsBinder), true), CanEditMultipleObjects]
public class SettingsBinderBuilder : Editor
{
    private bool showHiddenSettings = false;

    private SettingsBinder settingsBinder;

    private void OnEnable()
    {
        settingsBinder = target as SettingsBinder;
    }

    public override void OnInspectorGUI()
    {
        try
        {
            settingsBinder.BindedSettingType = (SettingsBinder.SettingsType)EditorGUILayout.EnumPopup("Binded Settings Type", settingsBinder.BindedSettingType);
            Dictionary<string, Type> fieldInfos = Settings.GetAllFieldInfos();
            List<string> potentialOptions = fieldInfos.Keys.ToList();

            if (settingsBinder.BindedSettingType != SettingsBinder.SettingsType.ALL)
            {
                potentialOptions = potentialOptions.Where(x => fieldInfos[x].Name.ToUpperInvariant().Contains(settingsBinder.BindedSettingType.ToString())).ToList();
            }

            potentialOptions.Insert(0, "None");
            settingsBinder.BindedSetting = potentialOptions[
                EditorGUILayout.Popup(
                    "Binded Setting",
                    potentialOptions.IndexOf(settingsBinder.BindedSetting),
                    potentialOptions.ToArray(),
                    GUILayout.MinHeight(0), GUILayout.MinWidth(0),
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))];

            showHiddenSettings = EditorGUILayout.Toggle("Show Hidden Settings", showHiddenSettings);
            if (showHiddenSettings) base.OnInspectorGUI();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settingsBinder);
            }
        }
        catch (Exception e)
        {
            EditorGUILayout.HelpBox($"Erorr while loading custom editor:\n{e}", MessageType.Error);
            base.OnInspectorGUI();
        }
    }
}
