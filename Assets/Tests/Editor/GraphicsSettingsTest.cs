using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace Tests.Editor
{
    public class GraphicsSettingsTest
    {
        private bool previousAnimations;
        private bool previousEditModeWorldParticles;
        private bool previousEditModeSmoke;
        private GameObject clone;
        private Material particleMaterial;
        private bool previousInstantEscapeMenuTransitions;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return TestUtils.LoadMap(3);
            previousAnimations = Settings.Instance.Animations;
            previousInstantEscapeMenuTransitions = Settings.Instance.InstantEscapeMenuTransitions;
            var field = typeof(Settings).GetField("EditModeWorldParticles");
            previousEditModeWorldParticles = field != null && (bool)field.GetValue(Settings.Instance);
            if (field != null)
                Settings.ApplyOptionByName("EditModeWorldParticles", false);

            var smokeField = typeof(Settings).GetField("EditModeSmoke");
            previousEditModeSmoke = smokeField != null && (bool)smokeField.GetValue(Settings.Instance);
            if (smokeField != null)
                Settings.ApplyOptionByName("EditModeSmoke", false);

            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var options = Object.FindAnyObjectByType<OptionsController>();
            if (options != null)
            {
                Settings.Instance.InstantEscapeMenuTransitions = true;
                options.Close();
                yield return new WaitUntil(() => !SceneManager.GetSceneByName("04_Options").isLoaded);
            }

            var pauseManager = Object.FindAnyObjectByType<PauseManager>();
            if (pauseManager != null && PauseManager.IsPaused)
                pauseManager.TogglePause();

            Settings.Instance.InstantEscapeMenuTransitions = previousInstantEscapeMenuTransitions;
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);

            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            if (typeof(Settings).GetField("EditModeWorldParticles") != null)
                Settings.ApplyOptionByName("EditModeWorldParticles", previousEditModeWorldParticles);

            if (typeof(Settings).GetField("EditModeSmoke") != null)
                Settings.ApplyOptionByName("EditModeSmoke", previousEditModeSmoke);

            Settings.ManuallyNotifySettingUpdatedEvent("BloomFog", true);
            if (clone != null)
                Object.Destroy(clone);
            if (particleMaterial != null)
                Object.Destroy(particleMaterial);

            yield return null;
        }

        [UnityTest]
        public IEnumerator WorldParticlesFollowEveryViewModeAndLiveSettingChanges()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var renderers = descriptor.GetComponentsInChildren<ParticleSystemRenderer>(true)
                .Where(renderer => renderer.name == "DustPS").ToArray();
            Assert.That(renderers, Is.Not.Empty, "The environment must provide real world particle renderers.");
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            foreach (var mode in new[] { UIModeType.Normal, UIModeType.HideUI, UIModeType.HideGrids,
                         UIModeType.Preview, UIModeType.Playing })
            {
                uiMode.SetUIMode(mode, false);
                foreach (var renderer in renderers)
                    Assert.That(renderer.forceRenderingOff, Is.EqualTo(mode is not (UIModeType.Preview or UIModeType.Playing)),
                        $"World particles have incorrect visibility in {mode} with the setting disabled.");

                Settings.ApplyOptionByName("EditModeWorldParticles", true);
                foreach (var renderer in renderers)
                    Assert.That(renderer.forceRenderingOff, Is.False,
                        $"Enabling edit-mode particles must show them immediately in {mode}.");

                Settings.ApplyOptionByName("EditModeWorldParticles", false);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator ParticleVisibilitySurvivesCloningReactivationAndEnvironmentRefresh()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var source = context.Descriptor.GetComponentsInChildren<ParticleSystemRenderer>(true)
                .Single(renderer => renderer.name == "DustPS");
            Assert.That(source, Is.Not.Null);
            Assert.That(source.forceRenderingOff, Is.True, "Particles must start hidden in edit mode.");
            clone = Object.Instantiate(source.gameObject, context.Descriptor.transform);
            var renderer = clone.GetComponent<ParticleSystemRenderer>();
            clone.SetActive(false);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Preview, false);
            clone.SetActive(true);
            Assert.That(renderer.forceRenderingOff, Is.False, "A reactivated clone must use the current mode.");

            Settings.ApplyOptionByName("EditModeWorldParticles", true);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            context.Descriptor.Initialize(context);
            Assert.That(renderer.forceRenderingOff, Is.False);
            renderer.enabled = false;
            Settings.ApplyOptionByName("EditModeWorldParticles", false);
            Settings.ApplyOptionByName("EditModeWorldParticles", true);
            Assert.That(renderer.enabled, Is.False, "Visibility must not override a renderer disabled by map content.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SmokeAndDustSettingsAreIndependentInEveryViewMode()
        {
            var renderers = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor
                .GetComponentsInChildren<ParticleSystemRenderer>(true);
            var smoke = renderers.Single(renderer => renderer.name == "BigSmokePS");
            var dust = renderers.Single(renderer => renderer.name == "DustPS");
            var uiMode = Object.FindAnyObjectByType<UIMode>();
            Settings.ApplyOptionByName("EditModeWorldParticles", true);
            Assert.That(smoke.forceRenderingOff, Is.True, "Enabling world particles must not enable edit-mode smoke.");

            foreach (var mode in new[] { UIModeType.Normal, UIModeType.HideUI, UIModeType.HideGrids,
                         UIModeType.Preview, UIModeType.Playing })
            foreach (var showSmoke in new[] { false, true })
            foreach (var showDust in new[] { false, true })
            {
                Settings.ApplyOptionByName("EditModeSmoke", showSmoke);
                Settings.ApplyOptionByName("EditModeWorldParticles", showDust);
                uiMode.SetUIMode(mode, false);
                var editMode = mode is not (UIModeType.Playing or UIModeType.Preview);
                Assert.That(smoke.forceRenderingOff, Is.EqualTo(editMode && !showSmoke), $"Incorrect smoke visibility in {mode}.");
                Assert.That(dust.forceRenderingOff, Is.EqualTo(editMode && !showDust), $"Incorrect dust visibility in {mode}.");
            }

            uiMode.SetUIMode(UIModeType.Normal, false);
            Settings.ApplyOptionByName("EditModeSmoke", false);
            clone = Object.Instantiate(smoke.gameObject, smoke.transform.parent);
            clone.name = "Renamed environment effect";
            clone.SetActive(false);
            Settings.ApplyOptionByName("EditModeSmoke", true);
            clone.SetActive(true);
            var clonedRenderer = clone.GetComponent<ParticleSystemRenderer>();
            Assert.That(clonedRenderer.forceRenderingOff, Is.False);
            Settings.ApplyOptionByName("EditModeSmoke", false);
            Assert.That(clonedRenderer.forceRenderingOff, Is.True);
            yield return null;
        }

        [TestCase("Assets/_Graphics/Materials/Environments/BigSmokeParticle (Instance).mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/BigSmokeParticle (Instance)_4164ddaea465.mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/Gaga/GagaSmoke (Instance).mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/LinkinPark/BigSmokeParticle.mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/Pyro/PyroSmoke (Instance).mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/Queen/QueenSmoke (Instance).mat")]
        public void SmokeMaterialsUseSmokeSettingInsteadOfWorldParticles(string materialPath)
        {
            Settings.ApplyOptionByName("EditModeWorldParticles", true);
            clone = new GameObject("Unnamed particle effect", typeof(ParticleSystem));
            var renderer = clone.GetComponent<ParticleSystemRenderer>();
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(source, Is.Not.Null);
            particleMaterial = new Material(source) { name = "Regenerated material" };
            particleMaterial.SetOverrideTag("ChroMapperWorldEffect", "");
            renderer.sharedMaterial = particleMaterial;
            WorldParticleVisibility.Initialize(clone.transform);
            Assert.That(renderer.forceRenderingOff, Is.True, "Smoke must use its shader and fog mode, without custom tags or its original material name.");
            Settings.ApplyOptionByName("EditModeSmoke", true);
            Assert.That(renderer.forceRenderingOff, Is.False);
            Settings.ApplyOptionByName("EditModeWorldParticles", false);
            Assert.That(renderer.forceRenderingOff, Is.False, "The particles setting must not affect smoke.");
            Settings.ApplyOptionByName("EditModeSmoke", false);
            Assert.That(renderer.forceRenderingOff, Is.True);
        }

        [TestCase("Assets/_Graphics/Materials/Environments/ParticleStars (Instance).mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/SimpleParticle (Instance).mat")]
        [TestCase("Assets/_Graphics/Materials/Environments/Billie/Rain.mat")]
        public void DustAndRainMaterialsUseWorldParticlesSetting(string materialPath)
        {
            Settings.ApplyOptionByName("EditModeSmoke", true);
            clone = new GameObject("Unnamed world particles", typeof(ParticleSystem));
            var renderer = clone.GetComponent<ParticleSystemRenderer>();
            var source = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(source, Is.Not.Null);
            particleMaterial = new Material(source) { name = "Regenerated material" };
            renderer.sharedMaterial = particleMaterial;
            WorldParticleVisibility.Initialize(clone.transform);
            Assert.That(renderer.forceRenderingOff, Is.True, "Smoke visibility must not enable dust or rain.");
            Settings.ApplyOptionByName("EditModeWorldParticles", true);
            Assert.That(renderer.forceRenderingOff, Is.False);
            Settings.ApplyOptionByName("EditModeSmoke", false);
            Assert.That(renderer.forceRenderingOff, Is.False, "Smoke visibility must not disable dust or rain.");
            Settings.ApplyOptionByName("EditModeWorldParticles", false);
            Assert.That(renderer.forceRenderingOff, Is.True);
        }

        [UnityTest]
        public IEnumerator OptionsTogglesUpdateBackgroundWorldEffectsImmediatelyAfterMapReload()
        {
            yield return TestUtils.ReloadMap(3, new SimpleJSON.JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var renderers = context.Descriptor.GetComponentsInChildren<ParticleSystemRenderer>(true);
            var smoke = renderers.Single(renderer => renderer.name == "BigSmokePS");
            var dust = renderers.Single(renderer => renderer.name == "DustPS");
            var cameraManager = Object.FindAnyObjectByType<CameraManager>();
            var cameraTransform = cameraManager.SelectedCameraController.transform;
            var position = cameraTransform.position;
            var rotation = cameraTransform.rotation;
            Object.FindAnyObjectByType<PauseManager>().TogglePause();
            OptionsController.ShowOptions();
            yield return null;
            Assert.That(OptionsController.IsActive, Is.True);
            Assert.That(UIMode.SelectedMode, Is.EqualTo(UIModeType.Normal));
            var optionsScene = SceneManager.GetSceneByName("04_Options");
            var binders = optionsScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<SettingsBinder>(true)).ToArray();
            var smokeToggle = binders.Single(binder => binder.BindedSetting == "EditModeSmoke").GetComponent<BetterToggle>();
            var dustToggle = binders.Single(binder => binder.BindedSetting == "EditModeWorldParticles").GetComponent<BetterToggle>();
            smokeToggle.gameObject.SetActive(true);
            dustToggle.gameObject.SetActive(true);
            foreach (var show in new[] { true, false, true, false })
            {
                Assert.DoesNotThrow(() => smokeToggle.SetUiOn(show));
                Assert.That(smoke.forceRenderingOff, Is.EqualTo(!show), "Smoke must update before leaving options or moving the camera.");
                Assert.DoesNotThrow(() => dustToggle.SetUiOn(show));
                Assert.That(dust.forceRenderingOff, Is.EqualTo(!show), "Dust must update before leaving options or moving the camera.");
                Assert.That(OptionsController.IsActive, Is.True);
                Assert.That(cameraTransform.position, Is.EqualTo(position));
                Assert.That(cameraTransform.rotation, Is.EqualTo(rotation));
            }
        }

        [UnityTest]
        public IEnumerator LegacyBloomFogNotificationCannotDisableFog()
        {
            var controller = Object.FindAnyObjectByType<BloomfogRenderingController>();
            Assert.That(controller.CanRenderReflections, Is.True);
            Settings.ManuallyNotifySettingUpdatedEvent("BloomFog", false);
            Assert.That(controller.CanRenderReflections, Is.True, "Fog bloom must remain enabled independently of the removed setting.");
            yield return null;
        }
    }
}
