using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(SettingsBinder), true), CanEditMultipleObjects]
public class SettingsBinderBuilder : Editor
{
    private SettingsBinder settingsBinder;

    private void OnEnable()
    {
        settingsBinder = target as SettingsBinder;
    }

    public override void OnInspectorGUI()
    {
        try
        {
            settingsBinder.BindedSettingSearchType = (SettingsBinder.SettingsType)EditorGUILayout.EnumPopup("Binded Settings Search Type", settingsBinder.BindedSettingSearchType);
            Dictionary<string, Type> fieldInfos = Settings.GetAllFieldInfos();
            List<string> potentialOptions = fieldInfos.Keys.ToList();

            if (settingsBinder.BindedSettingSearchType != SettingsBinder.SettingsType.ALL)
            {
                potentialOptions = potentialOptions.Where(x => fieldInfos[x].Name.ToUpperInvariant().Contains(settingsBinder.BindedSettingSearchType.ToString())).ToList();
            }

            potentialOptions.Insert(0, "None");
            potentialOptions = potentialOptions.OrderBy(x => x).ToList();

            if (potentialOptions.IndexOf(settingsBinder.BindedSetting) == -1)
            {
                settingsBinder.BindedSetting = "None";
            }

            settingsBinder.BindedSetting = potentialOptions[
                EditorGUILayout.Popup(
                    "Binded Setting",
                    potentialOptions.IndexOf(settingsBinder.BindedSetting),
                    potentialOptions.ToArray(),
                    GUILayout.MinHeight(0), GUILayout.MinWidth(0),
                    GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true))];

            settingsBinder.PopupEditorWarning = EditorGUILayout.Toggle("Show Editor Restart Warning", settingsBinder.PopupEditorWarning);

            if (settingsBinder.BindedSetting != "None")
            {
                EditorGUILayout.TextField("Binded Setting Type", fieldInfos[settingsBinder.BindedSetting]?.Name ?? "None");
            }
            else
            {
                EditorGUILayout.TextField("Binded Setting Type", "None");
            }

            base.OnInspectorGUI();

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
