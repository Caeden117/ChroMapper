using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

namespace TestsEditMode
{
    public class GraphicsOptionsTest
    {
        private Scene scene;
        private SettingsBinder[] binders;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.OpenPreviewScene("Assets/__Scenes/04_Options.unity");
            binders = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SettingsBinder>(true)).ToArray();
        }

        [TearDown]
        public void TearDown() => EditorSceneManager.ClosePreviewScene(scene);

        [Test]
        public void ObsoleteGraphicsSettingsAreNotOffered()
        {
            Assert.That(binders.Any(binder => binder.BindedSetting == "BloomFog"), Is.False);
            Assert.That(binders.Any(binder => binder.BindedSetting == "Reflections"), Is.False);
            Assert.That(Settings.GetAllFieldInfos().ContainsKey("BloomFog"), Is.False);
            Assert.That(Settings.GetAllFieldInfos().ContainsKey("Reflections"), Is.False);
        }

        [Test]
        public void ScreenDisplacementHasLocalizedLabelAndTooltip()
        {
            var binder = binders.Single(item => item.BindedSetting == "ScreenDisplacement");
            var label = binder.GetComponentInChildren<LocalizeStringEvent>(true);
            Assert.That(ResolveEnglish(label.StringReference), Is.EqualTo("Screen Displacement"));
            Assert.That(ResolveEnglish(binder.GetComponent<Tooltip>().LocalizedTooltip),
                Is.EqualTo("Enables or disables obstacle screen displacement."));
        }

        [Test]
        public void EditModeWorldParticlesHasLocalizedControlAndDefaultsToDisabled()
        {
            var binder = binders.SingleOrDefault(item => item.BindedSetting == "EditModeWorldParticles");
            Assert.That(binder, Is.Not.Null, "The graphics panel must expose Edit Mode World Particles.");
            Assert.That(ResolveEnglish(binder.GetComponentInChildren<LocalizeStringEvent>(true).StringReference),
                Is.EqualTo("Edit Mode World Particles"));
            Assert.That(ResolveEnglish(binder.GetComponent<Tooltip>().LocalizedTooltip),
                Is.EqualTo("Shows world particles in edit modes. When disabled, particles are only visible in Playing and Preview modes."));
            Assert.That(typeof(Settings).GetField("EditModeWorldParticles").GetValue(new Settings()), Is.False);
            Assert.That(binder.transform.parent, Is.EqualTo(binders.Single(item => item.BindedSetting == "ScreenDisplacement").transform.parent));
        }

        [Test]
        public void EditModeSmokeHasLocalizedControlAndDefaultsToDisabled()
        {
            var binder = binders.SingleOrDefault(item => item.BindedSetting == "EditModeSmoke");
            Assert.That(binder, Is.Not.Null, "The graphics panel must expose Edit Mode Smoke.");
            Assert.That(ResolveEnglish(binder.GetComponentInChildren<LocalizeStringEvent>(true).StringReference),
                Is.EqualTo("Edit Mode Smoke"));
            Assert.That(ResolveEnglish(binder.GetComponent<Tooltip>().LocalizedTooltip),
                Is.EqualTo("Shows world smoke in edit modes. When disabled, smoke is only visible in Playing and Preview modes."));
            Assert.That(typeof(Settings).GetField("EditModeSmoke").GetValue(new Settings()), Is.False);
            Assert.That(binder.transform.parent, Is.EqualTo(binders.Single(item => item.BindedSetting == "EditModeWorldParticles").transform.parent));
        }

        [Test]
        public void FirstSettingSubscriberCanUnsubscribeWithoutRemovingOtherListeners()
        {
            var key = nameof(FirstSettingSubscriberCanUnsubscribeWithoutRemovingOtherListeners);
            var firstCalls = 0;
            var secondCalls = 0;
            System.Action<object> first = _ => firstCalls++;
            System.Action<object> second = _ => secondCalls++;
            Settings.ClearSettingNotifications(key);
            try
            {
                Settings.NotifyBySettingName(key, first);
                Settings.NotifyBySettingName(key, second);
                Settings.ManuallyNotifySettingUpdatedEvent(key, true);
                Assert.That(firstCalls, Is.EqualTo(1));
                Assert.That(secondCalls, Is.EqualTo(1));
                Settings.StopNotifyingBySettingName(key, first);
                Settings.ManuallyNotifySettingUpdatedEvent(key, false);
                Assert.That(firstCalls, Is.EqualTo(1), "The first subscriber must not survive unsubscription.");
                Assert.That(secondCalls, Is.EqualTo(2), "Unsubscribing the first listener must not remove later listeners.");
            }
            finally
            {
                Settings.ClearSettingNotifications(key);
            }
        }

        private static string ResolveEnglish(LocalizedString reference)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:StringTable", new[] { "Assets/Locales" }))
            {
                var table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table.LocaleIdentifier.Code != "en" || table.SharedData.TableCollectionNameGuid != reference.TableReference.TableCollectionNameGuid)
                    continue;

                return table.GetEntry(reference.TableEntryReference.KeyId)?.Value;
            }

            return null;
        }
    }
}
