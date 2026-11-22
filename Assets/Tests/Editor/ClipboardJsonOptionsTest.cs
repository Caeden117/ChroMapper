using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests.Editor
{
    public class ClipboardJsonOptionsTest
    {
        [Test]
        public void PartialEventBoxClipboardCommentHasLocalizedTextWithoutJsonCommentPrefix()
        {
            const long commentEntryId = 620000000000000001;
            Assert.That(
                GetLocalizedValue("Assets/Locales/Mapper_en.asset", commentEntryId),
                Is.EqualTo(
                    "CM partial eventbox, include this comment when copying back into CM to merge inner events with original event box instead of replacing all events"));
        }

        [Test]
        public void MappingOptionsContainsLocalizedEnabledClipboardJsonToggle()
        {
            const long labelEntryId = 781108813288137152;
            const long tooltipEntryId = 781108813288137153;
            var optionsTableGuid = new System.Guid("3d3fb288927a2264891ce15662e46756");
            var scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            try
            {
                var binder = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<SettingsBinder>(true))
                    .Single(candidate => candidate.BindedSetting == nameof(Settings.SyncCopiedObjectsWithSystemClipboard));
                Assert.That(binder.GetComponent<BetterToggle>(), Is.Not.Null);
                var localizer = binder.GetComponentsInChildren<MonoBehaviour>(true)
                    .SingleOrDefault(component => component.GetType().Name == "LocalizeStringEvent");
                Assert.That(localizer, Is.Not.Null);
                Assert.That(localizer.enabled, Is.True);
                var serializedLocalizer = new SerializedObject(localizer);
                Assert.That(
                    serializedLocalizer.FindProperty("m_StringReference.m_TableReference.m_TableCollectionName").stringValue,
                    Is.EqualTo($"GUID:{optionsTableGuid:N}"));
                Assert.That(
                    serializedLocalizer.FindProperty("m_StringReference.m_TableEntryReference.m_KeyId").longValue,
                    Is.EqualTo(labelEntryId));
                var tooltip = binder.GetComponent<Tooltip>();
                Assert.That(tooltip, Is.Not.Null);
                var serializedTooltip = new SerializedObject(tooltip);
                Assert.That(
                    serializedTooltip.FindProperty("LocalizedTooltip.m_TableReference.m_TableCollectionName").stringValue,
                    Is.EqualTo($"GUID:{optionsTableGuid:N}"));
                Assert.That(
                    serializedTooltip.FindProperty("LocalizedTooltip.m_TableEntryReference.m_KeyId").longValue,
                    Is.EqualTo(tooltipEntryId));
                Assert.That(GetLocalizedValue("Assets/Locales/Options_en.asset", labelEntryId),
                    Is.EqualTo("Sync copied objects with clipboard JSON"));
                Assert.That(GetLocalizedValue("Assets/Locales/Options_en.asset", tooltipEntryId),
                    Is.EqualTo("Export copied objects as JSON when leaving ChroMapper and import recognized clipboard JSON when returning."));
                Assert.That(Settings.Instance.SyncCopiedObjectsWithSystemClipboard, Is.True);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static string GetLocalizedValue(string tablePath, long entryId)
        {
            var table = AssetDatabase.LoadAssetAtPath<Object>(tablePath);
            var tableData = new SerializedObject(table).FindProperty("m_TableData");
            for (var index = 0; index < tableData.arraySize; index++)
            {
                var entry = tableData.GetArrayElementAtIndex(index);
                if (entry.FindPropertyRelative("m_Id").longValue == entryId)
                {
                    return entry.FindPropertyRelative("m_Localized").stringValue;
                }
            }

            return null;
        }
    }
}
