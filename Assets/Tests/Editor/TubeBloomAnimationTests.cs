using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // TubeBloomAnimationTests covers the docs' AnimateComponent example event (additional-events page): a
    // TubeBloomPrePassLight component animated on track-bound geometry. Heck's AnimateComponent animates
    // colorAlphaMultiplier and bloomFogIntensityMultiplier on every TubeBloomPrePassLight under the track's
    // objects; the CM preview must drive the same multipliers on the track's ParametricBloomFogLightController,
    // hold finished values, and restore the authored component values when scrubbed back before the first event.
    public class TubeBloomAnimationTests : TestBase
    {
        private const string TrackName = "tubeLights";
        private const float AuthoredAlpha = 3f;
        private const float AuthoredBloomFog = 10f;

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The shared-map [UnityTearDown] restores an empty baseline after every case, so each
        // case must reload the tube-bloom fixture it asserts against.
        [UnitySetUp]
        public IEnumerator RestoreTubeBloomFixtureForEachCase()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // AnimateComponentTubeBloomEventsDriveLightMultipliers walks the event sequence in order and requires
        // every seek to land synchronously on the as-if-played multipliers, covering the instant event, the
        // eased interpolation window, the post-finish hold, the final override, and the scrub-back restore.
        [UnityTest]
        public IEnumerator AnimateComponentTubeBloomEventsDriveLightMultipliers()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var controller = FindTrackLightController();
            Assert.That(controller, Is.Not.Null, "The track-bound geometry did not create its bloom-fog light controller.");
            Assert.That(controller.HasInitialized, Is.True,
                "The light controller never initialized; physical light assertions would be vacuous.");
            Assert.That(controller.BoxLight, Is.Not.Null,
                "The light controller has no physical box light to receive alpha changes.");
            Assert.That(controller.BoxLight.Renderer, Is.Not.Null);

            // (seek beat, expected alpha, expected bloom-fog multiplier). Beats before the first event hold the
            // authored component values; the beat-4 event is instant, so beat 4 itself already carries its
            // value. The beat-10 event interpolates [2 @ 0, 6 @ 1 easeInQuad] over 4 beats, so beat 12 eases
            // to 0.5^2 = 0.25 -> 2 + 0.25 * 4 = 3.
            var checks = new[]
            {
                (3.5f, AuthoredAlpha, AuthoredBloomFog), // before every event: authored component values hold
                (4.5f, 1f, AuthoredBloomFog),            // beat 4 instant alpha event
                (9f, 1f, AuthoredBloomFog),              // hold after the instant event
                (12f, 1f, 3f),                            // halfway through the eased bloom-fog interpolation
                (14f, 1f, 6f),                            // interpolation end
                (17f, 5f, 6f),                            // beat 16 instant alpha event overrides the held alpha
                (0f, AuthoredAlpha, AuthoredBloomFog),    // scrub back before the first event restores authored values
            };

            // CompleteMapFinalBurstPhysicalLaserSurfaceHasWhiteCore: the controller's animated
            // multiplier must also reach the cached physical AlphaMultiplier and the box
            // renderer's MPB; Initialize copies it once, so a later push without refresh leaves
            // the rendered beam at the stale default.
            var mpb = new MaterialPropertyBlock();
            var colorId = Shader.PropertyToID("_Color");
            foreach (var (beat, alpha, bloomFog) in checks)
            {
                atsc.MoveToJsonTime(beat);
                // No frame yields: the seek itself must land on the as-if-played state.
                Assert.That(
                    controller.ColorAlphaMultiplier,
                    Is.EqualTo(alpha).Within(0.0001f),
                    $"Seeking to beat {beat} left the light's color alpha multiplier at " +
                    $"{controller.ColorAlphaMultiplier} instead of {alpha}.");
                Assert.That(
                    controller.BloomFogIntensityMultiplier,
                    Is.EqualTo(bloomFog).Within(0.0001f),
                    $"Seeking to beat {beat} left the light's bloom-fog multiplier at " +
                    $"{controller.BloomFogIntensityMultiplier} instead of {bloomFog}.");
                Assert.That(
                    controller.Color.a,
                    Is.GreaterThan(0.01f),
                    $"The fixture light is unlit at beat {beat}; the alpha assertions are vacuous.");
                Assert.That(
                    controller.BoxLight.AlphaMultiplier,
                    Is.EqualTo(alpha).Within(0.0001f),
                    $"Seeking to beat {beat} left BoxLight.AlphaMultiplier at " +
                    $"{controller.BoxLight.AlphaMultiplier} instead of {alpha} " +
                    "(the pushed controller multiplier must refresh the physical box).");
                Assert.That(
                    controller.BloomFog.IntensityMultiplier,
                    Is.EqualTo(bloomFog).Within(0.0001f),
                    $"Seeking to beat {beat} left BloomFog.IntensityMultiplier at " +
                    $"{controller.BloomFog.IntensityMultiplier} instead of {bloomFog}.");
                controller.BoxLight.Renderer.GetPropertyBlock(mpb);
                var mpbAlpha = mpb.GetColor(colorId).a;
                Assert.That(
                    mpbAlpha,
                    Is.EqualTo(controller.Color.a * alpha).Within(0.001f),
                    $"Seeking to beat {beat} left the box renderer's MPB _Color.a at {mpbAlpha}, " +
                    $"expected Color.a({controller.Color.a})*{alpha} " +
                    "(the rendered alpha must carry the animated multiplier).");
            }

            yield break;
        }

        // ParametricBloomFogLightController.Refresh pushes the animated multipliers onto the
        // physical box/sprite lights only via Initialize's one-time copy; a later
        // SetColorAlphaMultiplier must re-push Box.AlphaMultiplier and Sprite.AlphaMultiplier
        // (the latter still times FakeBloomIntensityMultiplier) without disturbing the bloom
        // multiplier or accumulating stale values on repeat sets.
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void InitializedPhysicalLightsReceiveAlphaChanges(bool withBox, bool withSprite)
        {
            var root = new GameObject("InitializedPhysicalLightsReceiveAlphaChanges");
            var failures = new List<string>();
            try
            {
                root.SetActive(false);

                ParametricBoxLight box = null;
                if (withBox)
                {
                    var child = new GameObject("box");
                    child.transform.SetParent(root.transform, false);
                    box = child.AddComponent<ParametricBoxLight>();
                    box.Renderer = child.AddComponent<MeshRenderer>();
                }
                ParametricSpriteLight sprite = null;
                if (withSprite)
                {
                    var child = new GameObject("sprite");
                    child.transform.SetParent(root.transform, false);
                    sprite = child.AddComponent<ParametricSpriteLight>();
                    sprite.Renderer = child.AddComponent<MeshRenderer>();
                }
                var bloomChild = new GameObject("bloomFog");
                bloomChild.transform.SetParent(root.transform, false);
                var bloomFog = bloomChild.AddComponent<BloomFogObject>();

                var controller = root.AddComponent<ParametricBloomFogLightController>();
                controller.ColorAlphaMultiplier = 1.5f;
                controller.FakeBloomIntensityMultiplier = 2f;
                controller.BloomFogIntensityMultiplier = 10f;
                controller.DisableRenderersOnZeroAlpha = false;
                controller.BoxLight = box;
                controller.SpriteLight = sprite;
                controller.BloomFog = bloomFog;

                root.SetActive(true);
                controller.Start(); // run the real Initialize/SetColor synchronously
                controller.SetColor(new Color(0.8f, 0.4f, 0.2f, 0.4f));

                var mpb = new MaterialPropertyBlock();
                var colorId = Shader.PropertyToID("_Color");
                void Sample(string phase, float expectedBoxMul, float expectedSpriteMul)
                {
                    if (withBox)
                    {
                        if (Mathf.Abs(box.AlphaMultiplier - expectedBoxMul) > 0.0001f)
                            failures.Add($"{phase}: Box.AlphaMultiplier={box.AlphaMultiplier}, " +
                                $"expected {expectedBoxMul}.");
                        box.Renderer.GetPropertyBlock(mpb);
                        var alpha = mpb.GetColor(colorId).a;
                        var expected = controller.Color.a * expectedBoxMul;
                        if (Mathf.Abs(alpha - expected) > 0.001f)
                            failures.Add($"{phase}: box MPB _Color.a={alpha}, expected {expected}.");
                    }
                    if (withSprite)
                    {
                        if (Mathf.Abs(sprite.AlphaMultiplier - expectedSpriteMul) > 0.0001f)
                            failures.Add($"{phase}: Sprite.AlphaMultiplier={sprite.AlphaMultiplier}, " +
                                $"expected {expectedSpriteMul}.");
                        sprite.Renderer.GetPropertyBlock(mpb);
                        var alpha = mpb.GetColor(colorId).a;
                        var expected = controller.Color.a * expectedSpriteMul;
                        if (Mathf.Abs(alpha - expected) > 0.001f)
                            failures.Add($"{phase}: sprite MPB _Color.a={alpha}, expected {expected}.");
                    }
                }

                // Initial state: box gets the authored multiplier, sprite gets it times
                // FakeBloomIntensityMultiplier (1.5*2=3), so MPB alphas are 0.6/1.2.
                Sample("initial", 1.5f, 3f);

                controller.SetColorAlphaMultiplier(3f);
                controller.Refresh();
                Sample("alpha3", 3f, 6f);
                if (Mathf.Abs(bloomFog.IntensityMultiplier - 10f) > 0.0001f)
                    failures.Add($"alpha3: BloomFog.IntensityMultiplier={bloomFog.IntensityMultiplier}, " +
                        "expected 10 (an alpha change must not disturb bloom fog).");

                controller.SetBloomFogIntensityMultiplier(4f);
                controller.Refresh();
                Sample("bloom4", 3f, 6f);
                if (Mathf.Abs(bloomFog.IntensityMultiplier - 4f) > 0.0001f)
                    failures.Add($"bloom4: BloomFog.IntensityMultiplier={bloomFog.IntensityMultiplier}, " +
                        "expected 4.");

                // Returning to the original alpha must restore the original physical values with
                // no accumulation, and a redundant same-value set leaves no dirty mark.
                controller.SetColorAlphaMultiplier(1.5f);
                controller.Refresh();
                Sample("restored", 1.5f, 3f);
                controller.SetColorAlphaMultiplier(1.5f);
                if (controller.ShouldRefresh)
                    failures.Add("A no-change SetColorAlphaMultiplier after a clean refresh " +
                        "still marked the controller dirty.");

                // Disabled renderers must not lose the multiplier change either: relighting
                // after an off-state alpha push has to carry it to the physical lights, and an
                // UpdateAlways refresh must apply it the same way.
                controller.DisableRenderersOnZeroAlpha = true;
                controller.SetColor(Color.clear);
                controller.SetColorAlphaMultiplier(5f);
                controller.Refresh();
                controller.SetColor(new Color(.8f, .4f, .2f, .4f));
                Sample("relit after off-alpha change", 5f, 10f);
                if (Mathf.Abs(bloomFog.IntensityMultiplier - 4f) > 0.0001f)
                    failures.Add($"relit after off-alpha change: BloomFog.IntensityMultiplier=" +
                        $"{bloomFog.IntensityMultiplier}, expected 4.");

                controller.UpdateAlways = true;
                controller.SetColorAlphaMultiplier(2f);
                controller.Refresh();
                Sample("UpdateAlways refresh", 2f, 4f);
                if (Mathf.Abs(bloomFog.IntensityMultiplier - 4f) > 0.0001f)
                    failures.Add($"UpdateAlways refresh: BloomFog.IntensityMultiplier=" +
                        $"{bloomFog.IntensityMultiplier}, expected 4.");
            }
            finally
            {
                Object.Destroy(root);
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // The fixture mirrors the reported component shape: a track-bound geometry cube carrying the
        // ILightWithId component (which creates CM's light controller) and the TubeBloomPrePassLight component
        // the events animate. SimpleJSON's JSONArray exposes Add without implementing IEnumerable, so the
        // arrays are built explicitly.
        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["geometry"] = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = "standard"
                },
                ["components"] = new JSONObject
                {
                    ["ILightWithId"] = new JSONObject
                    {
                        ["type"] = 1,
                        ["lightID"] = 9999
                    },
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = AuthoredAlpha,
                        ["bloomFogIntensityMultiplier"] = AuthoredBloomFog
                    }
                },
                ["track"] = TrackName
            });

            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = JSON.Parse("[1]")
                    }
                }
            });
            customEvents.Add(new JSONObject
            {
                ["b"] = 10f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["duration"] = 4f,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["bloomFogIntensityMultiplier"] = JSON.Parse("[[2, 0], [6, 1, \"easeInQuad\"]]")
                    }
                }
            });
            customEvents.Add(new JSONObject
            {
                ["b"] = 16f,
                ["t"] = "AnimateComponent",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["TubeBloomPrePassLight"] = new JSONObject
                    {
                        ["colorAlphaMultiplier"] = JSON.Parse("[5]")
                    }
                }
            });

            // A lit type-1 event keeps the fixture controller's Color.a>0 through every seek so
            // the physical-alpha assertions are never vacuous.
            var basicBeatmapEvents = new JSONArray();
            basicBeatmapEvents.Add(new JSONObject
            {
                ["b"] = 0f,
                ["et"] = 1,
                ["i"] = 5,
                ["f"] = 1f,
                ["customData"] = new JSONObject
                {
                    ["color"] = JSON.Parse("[0.8, 0.4, 0.2, 0.5]")
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["basicBeatmapEvents"] = basicBeatmapEvents,
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents
                }
            };
        }

        private static ParametricBloomFogLightController FindTrackLightController() =>
            Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(container => container.EnvironmentEnhancement?.Track == TrackName)
                .Select(container => container.GetComponentInChildren<ParametricBloomFogLightController>(true))
                .FirstOrDefault(controller => controller != null);

        // Restore the canonical empty shared map so later fixtures do not inherit the tube-bloom fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
