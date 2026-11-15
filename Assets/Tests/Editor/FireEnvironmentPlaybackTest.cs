using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    [Category("CMFirePreview")]
    public class FireEnvironmentPlaybackTest : TestBase
    {
        private static readonly int startTimeId = Shader.PropertyToID("_StartTime");
        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int pointLightColorId = Shader.PropertyToID("_PrivatePointLightColor");
        private static readonly int emissionColorId = Shader.PropertyToID("_EmissionTexColor");
        private static readonly FieldInfo fireEntriesField = typeof(FireGroupEffect)
            .GetField("fireEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo lightEntriesField = typeof(LightColorGroupEffect)
            .GetField("lightEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly float[] sampleTimes =
        {
            0f, 1.75f, 2.125f, 2.5f, 2.875f, 3.5f, 3.875f, 4.25f, 4.875f,
            5.5f, 6.125f, 6.5f, 6.875f, 7.5f, 7.875f, 8.5f, 9f
        };

        private bool previousAnimations;
        private bool previousLoadEvents;
        private LightshowMode previousLightshowMode;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private LightshowController lightshow;

        [SetUp]
        public void SaveSettings()
        {
            previousAnimations = Settings.Instance.Animations;
            previousLoadEvents = Settings.Instance.Load_Events;
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            lightshow = Object.FindAnyObjectByType<LightshowController>();
            previousLightshowMode = lightshow.Mode;
            Settings.Instance.Animations = true;
            Settings.Instance.Load_Events = true;
        }

        // Reloading releases scene-owned enhancement targets before the shared map is replaced.
        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PyroFirePlaybackMatchesPausedDirectAndReverseSeeks() =>
            VerifyPlaybackAndSeeks("PyroEnvironment", false, new[] { (12, 0f, 10), (13, 0.5f, 10) });

        [UnityTest]
        public IEnumerator MetallicaFirePlaybackMatchesPausedDirectAndReverseSeeks() =>
            VerifyPlaybackAndSeeks("MetallicaEnvironment", true,
                new[] { (27, 0f, 4), (28, 0.5f, 4), (38, 0f, 4), (39, 0.5f, 4) });

        [UnityTest]
        public IEnumerator MetallicaSharedFireSurvivesPausedTriggerEditsAcrossBoundaries()
        {
            var groups = new[] { (27, 0f, 4), (28, .5f, 4), (38, .5f, 4), (39, .5f, 4) };
            var map = CreateMap(groups);
            // The 2.5s burst is initially ignored. Removing the accepted 2s trigger must make it take over.
            map["lightColorEventBoxGroups"][0]["e"][0]["e"] = CreateColorEvents(
                (2, 1), (2.5f, 1), (4, 0), (6, 1), (8, 0));
            yield return TestUtils.ReloadMap(3, map, beatsPerMinute: 60,
                environmentName: "MetallicaEnvironment", songLengthSeconds: 60);
            lightshow.SetMode(LightshowMode.Full);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;

            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var bindings = GetBindings(descriptor, groups, true);
            var burst = (BurstFireEffect)bindings.Single(binding => binding.Group == 27 && binding.Element == 0).Fire;
            var shared = bindings.Where(binding => binding.Fire.EmissionTextureColorPropertyBlockController
                == burst.EmissionTextureColorPropertyBlockController).ToArray();
            Assert.That(shared, Has.Length.EqualTo(2), "This case must exercise a real shared Metallica light.");
            var continuousBinding = shared.Single(binding => binding.Fire is ContinuousFireEffect);
            var continuous = (ContinuousFireEffect)continuousBinding.Fire;
            Assert.That(burst.FadeOutDuration, Is.EqualTo(1), "The boundary times assume Metallica's 1s burst.");
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var boundaries = new[]
            {
                1.999f, 2f, 2.001f, 2.499f, 2.5f, 2.501f, 2.75f,
                2.999f, 3f, 3.001f, 3.5f, 4.499f, 4.5f, 4.501f
            };
            using (var clock = new DeterministicPlaybackClock(atsc))
            {
                clock.StartAtJsonTime(0);
                foreach (var time in boundaries)
                {
                    clock.AdvanceToJsonTime(time);
                    yield return null;
                    AssertSharedMetallicaFire(burst, continuous, time, 2, 2.5f, 4.5f);
                }
                clock.Pause();
                foreach (var time in boundaries.Reverse().Concat(boundaries))
                {
                    atsc.MoveToJsonTime(time);
                    AssertSharedMetallicaFire(burst, continuous, time, 2, 2.5f, 4.5f);
                }

                atsc.MoveToJsonTime(2.75f);
                Assert.That(GLSEventColorCommand.SetBrightness(FindFireTrigger(27, 2), 0), Is.Not.Null);
                // No yield, extra seek, or manager refresh: the edit itself must rebuild the paused preview.
                AssertSharedMetallicaFire(burst, continuous, 2.75f, 2.5f, 2.5f, 4.5f);
                foreach (var time in new[] { 3.499f, 3.5f, 3.501f, 2.499f, 2.5f, 3.25f })
                {
                    atsc.MoveToJsonTime(time);
                    AssertSharedMetallicaFire(burst, continuous, time, 2.5f, 2.5f, 4.5f);
                }

                DeleteFireTrigger(27, 2.5f);
                AssertSharedMetallicaFire(burst, continuous, 3.25f, null, 2.5f, 4.5f);
                PlaceUtils.Undo(); // Restore the newly accepted retrigger.
                AssertSharedMetallicaFire(burst, continuous, 3.25f, 2.5f, 2.5f, 4.5f);
                PlaceUtils.Undo(); // Restore the original trigger, which suppresses that retrigger again.
                Assert.That(FindFireTrigger(27, 2).Brightness, Is.EqualTo(1));
                AssertSharedMetallicaFire(burst, continuous, 3.25f, 2, 2.5f, 4.5f);
                PlaceUtils.Redo();
                Assert.That(FindFireTrigger(27, 2).Brightness, Is.Zero);
                AssertSharedMetallicaFire(burst, continuous, 3.25f, 2.5f, 2.5f, 4.5f);
                PlaceUtils.Redo();
                AssertSharedMetallicaFire(burst, continuous, 3.25f, null, 2.5f, 4.5f);

                DeleteFireTrigger(continuousBinding.Group, 2.5f);
                AssertSharedMetallicaFire(burst, continuous, 3.25f, null, null, 4.5f);
                PlaceUtils.Undo();
                AssertSharedMetallicaFire(burst, continuous, 3.25f, null, 2.5f, 4.5f);
                Assert.That(atsc.IsPlaying, Is.False);
                clock.Resume();
                AssertSharedMetallicaFire(burst, continuous, 3.25f, null, 2.5f, 4.5f);
                foreach (var time in new[] { 4.499f, 4.5f, 4.501f, 5.999f, 6f, 6.25f, 6.5f, 6.75f, 7f, 8.5f })
                {
                    clock.AdvanceToJsonTime(time);
                    yield return null;
                    AssertSharedMetallicaFire(burst, continuous, time, time >= 6 ? 6f : (float?)null,
                        time >= 6.5f ? 6.5f : 2.5f, time >= 6.5f ? 8.5f : 4.5f);
                }
            }
        }

        [UnityTest]
        public IEnumerator PausedFireGroupCollectionEditSupportsImmediateUndoAndRedo()
        {
            var groups = new[] { (27, 0f, 4), (28, .5f, 4), (38, 0f, 4), (39, .5f, 4) };
            yield return TestUtils.ReloadMap(3, CreateMap(groups), beatsPerMinute: 60,
                environmentName: "MetallicaEnvironment", songLengthSeconds: 60);
            lightshow.SetMode(LightshowMode.Full);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;

            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var bindings = GetBindings(context.Descriptor, groups, true);
            context.Atsc.MoveToJsonTime(2.75f);
            var before = Capture(bindings, 2.75f, "before collection edit");
            Assert.That(bindings.Any(binding => binding.Fire.BloomPrePassRenderer.enabled), Is.True);
            var actions = BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups.Select(group =>
            {
                var edited = (BaseLightColorEventBoxGroup)BeatmapFactory.Clone(group);
                edited.Boxes[0].Events[0].Brightness = 0;
                return new BeatmapGLSEventBoxModifiedAction(edited, group);
            }).ToArray();
            BeatmapActionContainer.AddAction(new ActionCollectionAction(actions), true);
            foreach (var binding in bindings)
                AssertExpectedFire(binding.Fire, 2.75f, null, 0);
            PlaceUtils.Undo();
            AssertSnapshots(before, Capture(bindings, 2.75f, "undo collection edit"), "Immediate collection undo");
            PlaceUtils.Redo();
            foreach (var binding in bindings)
                AssertExpectedFire(binding.Fire, 2.75f, null, 0);
        }

        private static BaseLightColorBase FindFireTrigger(int groupId, float relativeTime) =>
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups.Single(group => group.ID == groupId)
                .Boxes.Single().Events.Single(evt => evt.RelativeJsonTime == relativeTime);

        private static void DeleteFireTrigger(int groupId, float relativeTime)
        {
            var trigger = FindFireTrigger(groupId, relativeTime);
            Object.FindAnyObjectByType<GLSEventGridProvider>().GroupContext = trigger.EventBoxGroupData;
            BeatmapObjectContainerCollection.GetCollectionForType<GLSEventGridContainer>(ObjectType.GLSEvent)
                .DeleteObject(trigger, true, false);
            Assert.That(BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups
                .Single(group => group.ID == groupId).Boxes.Single().Events
                .Any(evt => evt.RelativeJsonTime == relativeTime), Is.False);
        }

        private static void AssertSharedMetallicaFire(BurstFireEffect burst, ContinuousFireEffect continuous,
            float seconds, float? burstStart, float? continuousStart, float continuousEnd)
        {
            var burstBloom = AssertExpectedFire(burst, seconds, burstStart,
                burstStart.GetValueOrDefault() + burst.FadeOutDuration);
            var continuousBloom = AssertExpectedFire(continuous, seconds, continuousStart, continuousEnd);
            // Continuous Update precedes the burst tween, including its final endpoint write.
            var burstWrites = burstStart.HasValue && seconds >= burstStart.Value
                && seconds <= burstStart.Value + burst.FadeOutDuration;
            var expectedPoint = burstWrites
                ? burst.PointLightColor * burstBloom : continuous.PointLightColor * continuousBloom;
            var controller = burst.EmissionTextureColorPropertyBlockController;
            Assert.That(continuous.EmissionTextureColorPropertyBlockController, Is.SameAs(controller));
            AssertColor(expectedPoint, controller.Mpb.GetColor(emissionColorId), $"Shared emission at {seconds}s");
            foreach (var renderer in controller.Renderers)
            {
                var applied = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(applied);
                AssertColor(expectedPoint, applied.GetColor(emissionColorId), $"Applied shared emission at {seconds}s");
            }
        }

        private static float AssertExpectedFire(FireEffect fire, float seconds, float? start, float end)
        {
            var triggered = start.HasValue && seconds >= start.Value;
            var visible = triggered && seconds < end;
            float flipbookAlpha = 0, bloomAlpha = 0;
            if (fire is BurstFireEffect burst)
            {
                // Initialization and completion retain the authored material endpoints.
                var progress = triggered ? Mathf.Clamp01((seconds - start.Value) / burst.FadeOutDuration) : 1f;
                flipbookAlpha = burst.FlipbookFadeOutCurve.Evaluate(progress);
                bloomAlpha = burst.BloomFadeOutCurve.Evaluate(progress);
            }
            else if (visible && fire is ContinuousFireEffect continuous)
            {
                // Beat Saber 1.45 UpdateEffect: OutQuad(fade-in) * InOutQuad(fade-out),
                // followed by the two authored sustain curves. Do not call CM's fire evaluator.
                var fadeIn = Mathf.InverseLerp(start.Value, Mathf.Min(start.Value + continuous.FadeInDuration, end), seconds);
                var fadeOut = 1 - Mathf.InverseLerp(Mathf.Max(end - continuous.FadeOutDuration, start.Value), end, seconds);
                var outQuad = 1 - (1 - fadeIn) * (1 - fadeIn);
                var inOutQuad = fadeOut < .5f ? 2 * fadeOut * fadeOut : 1 - 2 * (1 - fadeOut) * (1 - fadeOut);
                var sustainProgress = (seconds - start.Value) / continuous.SustainDuration;
                flipbookAlpha = outQuad * inOutQuad * continuous.FlipbookSustainCurve.Evaluate(sustainProgress);
                bloomAlpha = outQuad * inOutQuad * continuous.BloomSustainCurve.Evaluate(sustainProgress);
            }
            var label = $"{fire.GetType().Name} at {seconds}s, accepted trigger {start}";
            Assert.That(fire.BloomPrePassRenderer.enabled, Is.EqualTo(visible), label);
            foreach (var renderer in fire.FlipBookPropertyBlockController.Renderers)
            {
                var applied = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(applied);
                Assert.That(renderer.enabled, Is.EqualTo(visible), label);
                Assert.That(applied.GetFloat(startTimeId), Is.EqualTo(triggered ? start.Value : -1000).Within(.0001f), label);
                Assert.That(applied.GetColor(colorId).a, Is.EqualTo(flipbookAlpha).Within(.0001f), label);
            }
            foreach (var renderer in fire.BloomPropertyBlockController.Renderers)
            {
                var applied = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(applied);
                Assert.That(applied.GetColor(colorId).a,
                    Is.EqualTo(bloomAlpha * fire.BloomIntensityMultiplier).Within(.0001f), label);
            }
            AssertColor(fire.CustomLightColor.WithAlpha(visible ? bloomAlpha : 0), fire.ContributionColor, label);
            return bloomAlpha;
        }

        private IEnumerator VerifyPlaybackAndSeeks(
            string environmentName, bool useEmissionColor, (int id, float offset, int count)[] groups)
        {
            // At 60 BPM the authored JSON beats, song BPM time and effect seconds coincide.
            yield return TestUtils.ReloadMap(3, CreateMap(groups), beatsPerMinute: 60,
                environmentName: environmentName, songLengthSeconds: 60);
            lightshow.SetMode(LightshowMode.Full);
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            Assert.That(descriptor.ID, Is.EqualTo(environmentName));
            var bindings = GetBindings(descriptor, groups, useEmissionColor);
            var playback = new Dictionary<float, Dictionary<FireEffect, FireSnapshot>>();
            var litTargets = new HashSet<FireEffect>();

            using (var clock = new DeterministicPlaybackClock(atsc))
            {
                clock.StartAtJsonTime(0f);
                var time = 0f;
                foreach (var sample in sampleTimes)
                {
                    // Cross event boundaries while IsPlaying is true, through the production time callbacks.
                    while (time < sample)
                    {
                        time = Mathf.Min(time + 0.125f, sample);
                        clock.AdvanceToJsonTime(time);
                        yield return null;
                    }

                    yield return null;
                    Assert.That(atsc.IsPlaying, Is.True);
                    Assert.That(atsc.CurrentSeconds, Is.EqualTo(sample).Within(0.0001f));
                    playback[sample] = Capture(bindings, sample, $"{environmentName} playback", litTargets);
                }

                Assert.That(litTargets.Count, Is.EqualTo(bindings.Count),
                    "Every authored burst and continuous fire must visibly contribute during playback.");
                clock.Pause();

                foreach (var sample in sampleTimes)
                {
                    atsc.MoveToJsonTime(0f);
                    yield return null;
                    atsc.MoveToJsonTime(sample);
                    yield return null;
                    AssertSnapshots(playback[sample], Capture(bindings, sample, "direct seek"),
                        $"{environmentName} direct seek at {sample}s");
                }

                foreach (var sample in sampleTimes.Reverse())
                {
                    atsc.MoveToJsonTime(9f);
                    yield return null;
                    atsc.MoveToJsonTime(sample);
                    yield return null;
                    AssertSnapshots(playback[sample], Capture(bindings, sample, "reverse seek"),
                        $"{environmentName} reverse seek at {sample}s");
                }

                atsc.MoveToJsonTime(2.5f);
                clock.Resume();
                yield return null;
                AssertSnapshots(playback[2.5f], Capture(bindings, 2.5f, "resume"),
                    $"{environmentName} resume after reverse seek");
                clock.AdvanceToJsonTime(2.875f);
                yield return null;
                AssertSnapshots(playback[2.875f], Capture(bindings, 2.875f, "resumed playback"),
                    $"{environmentName} resumed playback");
            }
        }

        private static JSONObject CreateMap((int id, float offset, int count)[] groups)
        {
            var colorGroups = new JSONArray();
            foreach (var group in groups)
            {
                var events = new JSONArray();
                foreach (var (time, brightness) in new[] { (2f, 1f), (4f, 0f), (6f, 1f), (8f, 0f) })
                    events.Add(new JSONObject { ["b"] = time + group.offset, ["s"] = brightness, ["i"] = 0 });
                var boxes = new JSONArray();
                boxes.Add(new JSONObject
                {
                    ["f"] = new JSONObject { ["f"] = 1, ["p"] = 1 },
                    ["e"] = events
                });
                colorGroups.Add(new JSONObject
                {
                    ["b"] = 0,
                    ["g"] = group.id,
                    ["e"] = boxes
                });
            }

            return new JSONObject { ["version"] = "3.2.0", ["lightColorEventBoxGroups"] = colorGroups };
        }

        private static JSONArray CreateColorEvents(params (float time, float brightness)[] events)
        {
            var result = new JSONArray();
            foreach (var (time, brightness) in events)
                result.Add(new JSONObject { ["b"] = time, ["s"] = brightness, ["i"] = 0 });
            return result;
        }

        private static List<FireBinding> GetBindings(
            EnvironmentDescriptor descriptor, (int id, float offset, int count)[] groups, bool useEmissionColor)
        {
            Assert.That(fireEntriesField, Is.Not.Null);
            Assert.That(lightEntriesField, Is.Not.Null);
            var effects = descriptor.LightColorGroupEffectManager.IdToEffect;
            Assert.That(effects.Where(pair => pair.Value is FireGroupEffect).Select(pair => pair.Key),
                Is.EquivalentTo(groups.Select(group => group.id)), "The environment fire group bindings changed.");
            var bindings = new List<FireBinding>();
            foreach (var group in groups)
            {
                Assert.That(effects[group.id], Is.InstanceOf<FireGroupEffect>());
                var entries = (List<FireGroupEffectEntry>)fireEntriesField.GetValue(effects[group.id]);
                // Reflection-probe data components elsewhere in the scene can share these IDs without
                // being registered receivers. Check the same authored receiver list as the effect.
                var lights = (List<LightController>)lightEntriesField.GetValue(effects[group.id]);
                Assert.That(entries.Select(entry => entry.ElementId), Is.EquivalentTo(Enumerable.Range(0, group.count)));
                foreach (var entry in entries)
                {
                    var fire = entry.Effect;
                    Assert.That(fire, Is.Not.Null);
                    Assert.That(fire.UseEmissionColor, Is.EqualTo(useEmissionColor));
                    Assert.That(fire.FlipBookPropertyBlockController.Renderers, Is.Not.Empty);
                    Assert.That(fire.BloomPropertyBlockController.Renderers, Is.Not.Empty);
                    Assert.That(fire.BloomPrePassRenderer, Is.Not.Null);
                    var contributionLights = lights.Where(light => light.Kind == LightController.LightKind.Group
                        && light.Type == group.id && light.ID == entry.ElementId).ToArray();
                    if (!useEmissionColor)
                        Assert.That(contributionLights, Is.Not.Empty, "Pyro fire must reach its scene contribution lights.");
                    bindings.Add(new FireBinding
                    {
                        Fire = fire, Group = group.id, Element = entry.ElementId, Offset = group.offset,
                        Lights = contributionLights
                    });
                }
            }

            Assert.That(bindings.Select(binding => binding.Fire),
                Is.EquivalentTo(descriptor.GetComponentsInChildren<FireEffect>(true)));
            Assert.That(bindings.Any(binding => binding.Fire is BurstFireEffect), Is.True);
            Assert.That(bindings.Any(binding => binding.Fire is ContinuousFireEffect), Is.True);
            return bindings;
        }

        private static Dictionary<FireEffect, FireSnapshot> Capture(
            List<FireBinding> bindings, float seconds, string phase, HashSet<FireEffect> litTargets = null)
        {
            var snapshots = new Dictionary<FireEffect, FireSnapshot>();
            foreach (var binding in bindings)
            {
                var fire = binding.Fire;
                var label = $"{phase} {binding.Group}:{binding.Element} {fire.GetType().Name} at {seconds}s";
                var start = seconds >= 6f + binding.Offset ? 6f + binding.Offset : 2f + binding.Offset;
                var duration = fire is BurstFireEffect burst ? burst.FadeOutDuration : 2f;
                var visible = seconds >= start && seconds < start + duration;
                AssertExpectedFire(fire, seconds, start, start + duration);
                var flipbook = fire.FlipBookPropertyBlockController.Mpb;
                var bloom = fire.BloomPropertyBlockController.Mpb;
                var flipbookColor = flipbook.GetColor(colorId);
                var bloomColor = bloom.GetColor(colorId);
                Assert.That(fire.BloomPrePassRenderer.enabled, Is.EqualTo(visible), label);
                foreach (var renderer in fire.FlipBookPropertyBlockController.Renderers)
                {
                    Assert.That(renderer.enabled, Is.EqualTo(visible), label);
                    Assert.That(renderer.gameObject.activeInHierarchy, Is.True, label);
                    var applied = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(applied);
                    Assert.That(applied.GetFloat(startTimeId), Is.EqualTo(flipbook.GetFloat(startTimeId)), label);
                    AssertColor(flipbookColor, applied.GetColor(colorId), label);
                }
                foreach (var renderer in fire.BloomPropertyBlockController.Renderers)
                {
                    var applied = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(applied);
                    AssertColor(bloomColor, applied.GetColor(colorId), label);
                }

                if (visible && flipbookColor.a > 0.001f && bloomColor.a > 0.001f)
                    litTargets?.Add(fire);

                foreach (var light in binding.Lights)
                {
                    Assert.That(light.HasInitialized, Is.True, label);
                    AssertColor(fire.ContributionColor, light.Color, label);
                }

                var pointController = fire.UseEmissionColor
                    ? fire.EmissionTextureColorPropertyBlockController : fire.PrivatePointLightPropertyBlockController;
                Assert.That(pointController, Is.Not.Null, label);
                var shared = bindings.Where(other => other.Fire.UseEmissionColor == fire.UseEmissionColor
                    && (fire.UseEmissionColor ? other.Fire.EmissionTextureColorPropertyBlockController
                        : other.Fire.PrivatePointLightPropertyBlockController) == pointController).ToArray();
                Assert.That(shared.Length, Is.InRange(1, 2), "The shipped fixtures have at most one burst/continuous pair.");
                var writer = shared[0];
                if (shared.Length == 2)
                {
                    writer = shared.Single(other => other.Fire is ContinuousFireEffect);
                    var sharedBurst = shared.Single(other => other.Fire is BurstFireEffect);
                    var burstStart = seconds >= 6f + sharedBurst.Offset ? 6f + sharedBurst.Offset : 2f + sharedBurst.Offset;
                    if (seconds >= burstStart && seconds <= burstStart + ((BurstFireEffect)sharedBurst.Fire).FadeOutDuration)
                        writer = sharedBurst;
                }
                var writerStart = seconds >= 6f + writer.Offset ? 6f + writer.Offset : 2f + writer.Offset;
                var writerDuration = writer.Fire is BurstFireEffect writerBurst ? writerBurst.FadeOutDuration : 2f;
                var writerBloom = AssertExpectedFire(writer.Fire, seconds, writerStart, writerStart + writerDuration);
                var pointColor = pointController.Mpb.GetColor(fire.UseEmissionColor ? emissionColorId : pointLightColorId);
                AssertColor(writer.Fire.PointLightColor * writerBloom, pointColor, label);
                Assert.That(pointController.Renderers, Is.Not.Empty, label);
                foreach (var renderer in pointController.Renderers)
                {
                    var applied = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(applied);
                    AssertColor(pointColor, applied.GetColor(fire.UseEmissionColor ? emissionColorId : pointLightColorId), label);
                }
                snapshots.Add(fire, new FireSnapshot
                {
                    Visible = visible, StartTime = flipbook.GetFloat(startTimeId), Flipbook = flipbookColor,
                    Bloom = bloomColor, Contribution = fire.ContributionColor, PointLight = pointColor
                });
            }

            return snapshots;
        }

        private static void AssertSnapshots(
            Dictionary<FireEffect, FireSnapshot> expected, Dictionary<FireEffect, FireSnapshot> actual, string phase)
        {
            foreach (var pair in expected)
            {
                var sample = actual[pair.Key];
                Assert.That(sample.Visible, Is.EqualTo(pair.Value.Visible), phase);
                Assert.That(sample.StartTime, Is.EqualTo(pair.Value.StartTime).Within(0.0001f), phase);
                AssertColor(pair.Value.Flipbook, sample.Flipbook, phase);
                AssertColor(pair.Value.Bloom, sample.Bloom, phase);
                AssertColor(pair.Value.Contribution, sample.Contribution, phase);
                AssertColor(pair.Value.PointLight, sample.PointLight, phase);
            }
        }

        private static void AssertColor(Color expected, Color actual, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f), message);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f), message);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f), message);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f), message);
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorState()
        {
            uiMode.SetUIMode(UIModeType.Normal, false);
            cameraManager.SelectCamera(CameraType.Editing);
            lightshow.SetMode(previousLightshowMode);
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.Load_Events = previousLoadEvents;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        private sealed class FireBinding
        {
            public FireEffect Fire;
            public int Group;
            public int Element;
            public float Offset;
            public LightController[] Lights;
        }

        private sealed class FireSnapshot
        {
            public bool Visible;
            public float StartTime;
            public Color Flipbook;
            public Color Bloom;
            public Color Contribution;
            public Color PointLight;
        }
    }
}
