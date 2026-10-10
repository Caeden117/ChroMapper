using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Beat Saber's 1.44.1 BillieEnvironment light switch uses highlight/normal alpha
    // 1/0.7490196. These tests sample the absolute native endpoints and both
    // fixed-duration clocks at 1/64-beat positions.
    public class BasicEventFixedDurationParityTest : TestBase
    {
        private const float NormalAlpha = 0.7490196f;
        private const float HighlightAlpha = 1f;
        private const float Bpm = 150f;
        private const float Tolerance = 0.0005f;
        private GameObject previewLightObject;
        private BasicLightEffect configuredEffect;
        private float previousOffIntensity;
        private bool? previousChromaLite;

        protected override IEnumerator OnMapLoaded()
        {
            yield return TestUtils.ReloadMap(3, null, beatsPerMinute: Bpm, environmentName: "BillieEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreStandardMapBaseline()
        {
            yield return TestUtils.ReloadMap(
                3, new JSONObject { ["version"] = "3.2.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // Beat Saber samples OutExpo over 1.5 song seconds, with the serialized highlight alpha at
        // the event and zero at the end. A one-beat fade would fail these 1/64-beat samples.
        [Test]
        public void RedFadeMatchesGameBrightnessAtEverySixtyFourthBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(2f, LightValue.RedFade);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var fadeBeats = Bpm * 1.5f / 60f;

            for (var step = 0; step <= Mathf.CeilToInt(fadeBeats * 64f); step++)
            {
                var beatOffset = step / 64f;
                atsc.MoveToJsonTime(2f + beatOffset);
                var progress = Mathf.Clamp01(beatOffset / fadeBeats);
                var easing = progress == 1f ? 1f : 1f - Mathf.Pow(2f, -10f * progress);
                var expectedAlpha = HighlightAlpha * (1f - easing);
                Assert.That(light.Color.a, Is.EqualTo(expectedAlpha).Within(Tolerance),
                    $"Fade brightness at beat {2f + beatOffset} differs from Beat Saber's 1.5-second OutExpo tween.");
            }

            // Paused reverse scrubbing must sample the same brightness independent of approach direction.
            atsc.MoveToJsonTime(2.5f);
            Assert.That(light.Color.a,
                Is.EqualTo(HighlightAlpha * Mathf.Pow(2f, -10f * (0.5f / fadeBeats))).Within(Tolerance));
        }

        // Beat Saber samples OutCubic over 0.6 song seconds with a 1/0.7490196 highlight
        // contrast; CM's old hardcoded 1.2-to-1 flash had the wrong relative curve.
        [Test]
        public void RedFlashMatchesGameBrightnessAtEverySixtyFourthBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(6f, LightValue.RedFlash);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var flashBeats = Bpm * 0.6f / 60f;

            for (var step = 0; step <= Mathf.CeilToInt(flashBeats * 64f); step++)
            {
                var beatOffset = step / 64f;
                atsc.MoveToJsonTime(6f + beatOffset);
                var progress = Mathf.Clamp01(beatOffset / flashBeats);
                var easing = 1f - Mathf.Pow(1f - progress, 3f);
                var expectedAlpha = Mathf.LerpUnclamped(HighlightAlpha, NormalAlpha, easing);
                Assert.That(light.Color.a, Is.EqualTo(expectedAlpha).Within(Tolerance),
                    $"Flash brightness at beat {6f + beatOffset} differs from Beat Saber's 0.6-second OutCubic tween.");
            }

            atsc.MoveToJsonTime(6.5f);
            var reverseProgress = 0.5f / flashBeats;
            var reverseExpected = Mathf.LerpUnclamped(HighlightAlpha, NormalAlpha,
                1f - Mathf.Pow(1f - reverseProgress, 3f));
            Assert.That(light.Color.a, Is.EqualTo(reverseExpected).Within(Tolerance));
        }

        // The basic event callback restarts the native tween at a later event, so a fade must stop
        // contributing once a same-type light-on event arrives before its fixed-duration endpoint.
        [Test]
        public void NewLightEventInterruptsFadeAtItsExactBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(10f, LightValue.RedFade);
            PlaceLightEvent(10.5f, LightValue.RedOn);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(10.5f - (1f / 64f));
            Assert.That(light.Color.a, Is.LessThan(1f));
            atsc.MoveToJsonTime(10.5f);
            Assert.That(light.Color.a, Is.EqualTo(NormalAlpha).Within(Tolerance));
        }

        // White uses ColorManager's same alpha for normal and highlight colors in the
        // 1.44.1 light switch; a white flash changes no brightness across its 0.6-second clock.
        [Test]
        public void WhiteFlashRetainsNativeBrightnessAtSixtyFourthBeatSamples()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(12f, LightValue.WhiteFlash);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            for (var step = 0; step <= 96; step++)
            {
                atsc.MoveToJsonTime(12f + (step / 64f));
                Assert.That(light.Color.a, Is.EqualTo(1f).Within(Tolerance),
                    $"White flash changed brightness at 1/64-beat step {step}.");
            }
        }

        // A new light event replaces the active flash at its own beat, even while the
        // 0.6-second fixed-duration tween would otherwise remain in progress.
        [Test]
        public void NewLightEventInterruptsFlashAtItsExactBeat()
        {
            var light = CreatePreviewLight();
            PlaceLightEvent(14f, LightValue.RedFlash);
            PlaceLightEvent(14.5f, LightValue.RedOn);
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            atsc.MoveToJsonTime(14.5f - (1f / 64f));
            Assert.That(light.Color.a, Is.GreaterThan(NormalAlpha));
            atsc.MoveToJsonTime(14.5f);
            Assert.That(light.Color.a, Is.EqualTo(NormalAlpha).Within(Tolerance));
        }

        // Beat Saber's fade destination uses ColorWithAlpha(offIntensity * floatValue), which
        // replaces the custom color's alpha rather than multiplying it a second time.
        [Test]
        public void FadeToNonzeroOffIntensityOverridesCustomColorAlpha()
        {
            var light = CreatePreviewLight();
            configuredEffect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            previousOffIntensity = configuredEffect.OffIntensity;
            configuredEffect.OffIntensity = 0.6f;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            Settings.Instance.EmulateChromaLite = true;
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = 18f,
                Type = light.Type,
                Value = (int)LightValue.RedFade,
                FloatValue = 1f,
                CustomColor = new Color(1f, 0.2f, 0.2f, 0.25f)
            });

            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(21.75f);
            Assert.That(light.Color.a, Is.EqualTo(0.6f).Within(Tolerance));
        }

        // GeneratedGeometryUsesNativeEventAlphaAcrossSeeks: generated geometry lit by the real
        // event stream must carry the native absolute factors — normal .7490196 and highlight
        // alpha 1 (contrast 1/0.7490196) from the scene color assets (times the authored color
        // alpha and float value), boost .8, and white's 2, matching imported scene lights.
        // Kamikazi's complete-map report measured the same wrong input as MPB alpha 1 on cubes.
        [UnityTest]
        public IEnumerator GeneratedGeometryUsesNativeEventAlphaAcrossSeeks()
        {
            var previousAnimations = Settings.Instance.Animations;
            previousChromaLite = Settings.Instance.EmulateChromaLite;
            var failures = new List<string>();
            try
            {
                Settings.Instance.Animations = true;
                Settings.Instance.EmulateChromaLite = true;

                var authoredColor = "[1, 0.2, 0.1, 0.5]";
                JSONNode Event(float beat, int value, bool withColor) => new JSONObject
                {
                    ["b"] = beat,
                    ["et"] = 1,
                    ["i"] = value,
                    ["f"] = 2f,
                    ["customData"] = withColor
                        ? new JSONObject { ["color"] = JSON.Parse(authoredColor) }
                        : new JSONObject()
                };
                // SimpleJSON's JSONArray exposes Add without IEnumerable, so no collection
                // initializers.
                var events = new JSONArray();
                events.Add(Event(0f, 5, true));   // RedOn
                events.Add(Event(4f, 6, true));   // RedFlash
                events.Add(Event(8f, 7, true));   // RedFade
                events.Add(Event(12f, 0, true));  // Off
                events.Add(Event(16f, 9, false)); // WhiteOn
                events.Add(Event(21f, 5, true));  // RedOn (boost on)
                events.Add(Event(24f, 6, true));  // RedFlash (boost on)
                events.Add(Event(28f, 9, false)); // WhiteOn (boost still on)
                var environment = new JSONArray();
                environment.Add(new JSONObject
                {
                    ["geometry"] = new JSONObject
                    {
                        ["type"] = "Cube",
                        ["material"] = new JSONObject { ["shader"] = "TransparentLight" }
                    },
                    ["position"] = JSON.Parse("[0, 1, 10]"),
                    ["components"] = new JSONObject
                    {
                        ["ILightWithId"] = new JSONObject
                        {
                            ["type"] = 1,
                            ["lightID"] = 9000
                        }
                    },
                    ["track"] = "nativeGeometryAlpha"
                });
                var boostEvents = new JSONArray();
                boostEvents.Add(new JSONObject { ["b"] = 20f, ["o"] = true });
                var raw = new JSONObject
                {
                    ["version"] = "3.3.0",
                    ["basicBeatmapEvents"] = events,
                    ["colorBoostBeatmapEvents"] = boostEvents,
                    ["customData"] = new JSONObject
                    {
                        ["environment"] = environment
                    }
                };
                yield return TestUtils.ReloadMap(
                    3, raw, beatsPerMinute: 150, environmentName: "BillieEnvironment");

                var container = Object.FindObjectsByType<Beatmap.Containers.GeometryContainer>(
                        FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(c =>
                        c.EnvironmentEnhancement != null
                        && c.EnvironmentEnhancement.Track == "nativeGeometryAlpha");
                Assert.That(container, Is.Not.Null,
                    "The generated geometry enhancement did not spawn (track nativeGeometryAlpha).");
                var controller = container
                    .GetComponentsInChildren<ParametricBloomFogLightController>(true)
                    .FirstOrDefault();
                Assert.That(controller, Is.Not.Null,
                    "The generated geometry has no ParametricBloomFogLightController.");
                Assert.That(controller.BoxLight, Is.Not.Null);
                var renderer = controller.BoxLight.Renderer;
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.enabled && renderer.gameObject.activeInHierarchy, Is.True);

                // Let Initialize/SetColor run before sampling seeks.
                yield return null;
                yield return null;

                var mpb = new MaterialPropertyBlock();
                var colorId = Shader.PropertyToID("_Color");
                var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
                var flashHalf = Mathf.LerpUnclamped(1f, 0.7490196f,
                    1f - Mathf.Pow(1f - 0.5f / 1.5f, 3f));
                var fadeRemaining = Mathf.Pow(2f, -10f / 3.75f);
                var litSamples = 0;
                foreach (var (beat, expected) in new (float Beat, float Alpha)[]
                {
                    (1f, 0.7490196f),       // red on: normal factor * authored .5 * f2
                    (4f, 1f),               // flash start: highlight 1 * .5 * 2
                    (4.5f, flashHalf),      // flash mid OutCubic decay toward normal
                    (6f, 0.7490196f),       // flash settled to normal
                    (8f, 1f),               // fade start at highlight
                    (9f, fadeRemaining),    // OutExpo remaining over 1.5s = 3.75 beats
                    (12f, 0f),              // off
                    (16f, 2f),              // white on bypasses the colored multiplier
                    (21f, 0.8f),            // boost on: boosted normal alpha * .5 * 2
                    (24f, 1f),              // flash start under boost
                    (26f, 0.8f),            // flash settled to boosted normal
                    (28f, 2f),              // white on under boost
                    (1f, 0.7490196f),       // reverse seek: pre-boost normal restored
                })
                {
                    atsc.MoveToJsonTime(beat);
                    renderer.GetPropertyBlock(mpb);
                    var controllerAlpha = controller.Color.a;
                    var mpbAlpha = mpb.GetColor(colorId).a;
                    if (Mathf.Abs(controllerAlpha - expected) > 0.001f)
                        failures.Add($"beat {beat}: controller Color.a={controllerAlpha}, " +
                            $"expected {expected}.");
                    if (Mathf.Abs(mpbAlpha - expected) > 0.001f)
                        failures.Add($"beat {beat}: box renderer MPB _Color.a={mpbAlpha}, " +
                            $"expected {expected} (AlphaMultiplier 1 passes it through).");
                    if (expected > 0f && controllerAlpha > 0f)
                        litSamples++;
                }
                Assert.That(litSamples, Is.GreaterThan(0),
                    "The fixture light never received a lit event; the alpha assertions are vacuous.");
            }
            finally
            {
                Settings.Instance.Animations = previousAnimations;
                if (previousChromaLite.HasValue)
                {
                    Settings.Instance.EmulateChromaLite = previousChromaLite.Value;
                    previousChromaLite = null;
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // Register a visible probe with the environment's actual BasicLightEffect and let placement
        // and ATSC callbacks drive it, rather than testing a detached interpolation helper.
        private ParityLightController CreatePreviewLight(int type = (int)EventTypeValue.Event2)
        {
            previewLightObject = new GameObject("Fixed duration parity light");
            var light = previewLightObject.AddComponent<ParityLightController>();
            light.Type = type;
            light.ID = -1;
            var effect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            effect.Register(light);
            effect.Initialize();
            return light;
        }

        private static void PlaceLightEvent(float beat, LightValue value)
        {
            PlaceUtils.Place(new BaseEvent
            {
                JsonTime = beat,
                Type = (int)EventTypeValue.Event2,
                Value = (int)value,
                FloatValue = 1f
            });
        }

        protected override void AfterCleanup()
        {
            if (configuredEffect != null)
            {
                configuredEffect.OffIntensity = previousOffIntensity;
                configuredEffect = null;
            }

            if (previousChromaLite.HasValue)
            {
                Settings.Instance.EmulateChromaLite = previousChromaLite.Value;
                previousChromaLite = null;
            }

            if (previewLightObject == null)
            {
                return;
            }

            var light = previewLightObject.GetComponent<ParityLightController>();
            var effect = Object.FindAnyObjectByType<BeatmapRuntimeContext>()
                .Descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(light.Type);
            effect.Unregister(light);
            Object.DestroyImmediate(previewLightObject);
            previewLightObject = null;
        }

        public sealed class ParityLightController : LightController
        {
            protected override bool Initialize() => true;

            public override void SetColor(Color color) => Color = color;
        }
    }
}
