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
    // MaterialTrackAnimationTest covers Heck material color animation: a customData.materials entry can carry
    // a "track" so AnimateTrack "color" animates every geometry using that material (Give In To You by
    // JRE_McNuggies & Nugget crashes map load with an NRE in GeometryAppearanceSO.SetGeometryAppearance
    // because Geometry.prefab lost its AnimationTarget child when the prefab was recreated, leaving
    // GeometryContainer.MaterialAnimator null for any tracked material).
    public class MaterialTrackAnimationTest : TestBase
    {
        private const string TrackName = "matTrack";
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The shared-map [UnityTearDown] restores an empty baseline after every case, so without this
        // the tests added after TrackedMaterialAnimatesGeometryColor would run against no geometry.
        [UnitySetUp]
        public IEnumerator RestoreMaterialFixtureForEachCase()
        {
            yield return TestUtils.ReloadMap(3, CreateDifficulty());
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // TrackedMaterialAnimatesGeometryColor requires the spawn to survive SetGeometryAppearance (the NRE),
        // a dedicated material animator on the container, and the track's animated color to reach the
        // container's material property block after a seek.
        [UnityTest]
        public IEnumerator TrackedMaterialAnimatesGeometryColor()
        {
            var container = FindGeometryContainer();
            Assert.That(container, Is.Not.Null, "The geometry enhancement did not spawn a container.");
            Assert.That(
                container.MaterialAnimator,
                Is.Not.Null,
                "The container has no material animator; tracked materials throw NRE during SetGeometryAppearance.");

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            // The color event interpolates black -> red over beats 4..8.
            atsc.MoveToJsonTime(9f);
            // One frame so the track push reaches the aggregator's LateUpdate application into the MPB.
            yield return null;

            var color = container.MpbController.Mpb.GetColor(ColorId);
            Assert.That(
                color.r,
                Is.EqualTo(1f).Within(0.01f),
                $"The track's animated color never reached the geometry material (got {color}); in game the " +
                "material's track colors the object red by beat 8.");
            Assert.That(color.g, Is.LessThan(0.05f), $"Expected animated red, got {color}.");
        }

        // Chroma keys its Standard->Glowing upgrade on an explicitly empty shaderKeywords array,
        // but BaseMaterial collapses presence to a List and V2/V3Material.ToJson only writes
        // Count>0 — saving or cloning must not silently drop the authored empty array.
        [TestCase(2, false)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        public void MaterialKeywordPresenceSurvivesSaveAndClone(int version, bool explicitEmpty)
        {
            var previousVersion = Settings.Instance.MapVersion;
            try
            {
                Settings.Instance.MapVersion = version;
                var shaderKey = version == 2 ? "_shader" : "shader";
                var keywordKey = version == 2 ? "_shaderKeywords" : "shaderKeywords";
                var node = new JSONObject { [shaderKey] = "Standard" };
                if (explicitEmpty) node[keywordKey] = new JSONArray();

                var material = new Beatmap.Base.Customs.BaseMaterial(node);
                var serialized = material.ToJson();
                Assert.That(serialized.HasKey(keywordKey), Is.EqualTo(explicitEmpty),
                    $"v{version} explicitEmpty={explicitEmpty}: ToJson keyword key presence " +
                    "changed the authored data.");
                if (explicitEmpty)
                {
                    Assert.That(serialized[keywordKey].IsArray, Is.True);
                    Assert.That(serialized[keywordKey].AsArray.Count, Is.Zero);
                }

                var cloneJson = material.Clone().ToJson();
                Assert.That(cloneJson.HasKey(keywordKey), Is.EqualTo(explicitEmpty),
                    $"v{version} explicitEmpty={explicitEmpty}: Clone+ToJson keyword key " +
                    "presence changed the authored data.");
                if (explicitEmpty)
                {
                    Assert.That(cloneJson[keywordKey].IsArray, Is.True);
                    Assert.That(cloneJson[keywordKey].AsArray.Count, Is.Zero);
                }
            }
            finally
            {
                Settings.Instance.MapVersion = previousVersion;
            }
        }

        // Native Chroma converts a named Standard material with explicit shaderKeywords:[] into
        // the Glowing shader with the color's alpha forced to 0; the same rule applies to inline
        // material objects on geometry. These guard both shapes against the lit-Standard fallback.
        [UnityTest]
        public IEnumerator EmptyStandardKeywordsNamedMaterialUsesZeroAlpha() =>
            EmptyStandardKeywordsUseZeroAlpha(named: true);

        [UnityTest]
        public IEnumerator EmptyStandardKeywordsInlineMaterialUsesZeroAlpha() =>
            EmptyStandardKeywordsUseZeroAlpha(named: false);

        // Presence is not enough: a non-empty authored keyword list must round-trip
        // verbatim through ToJson and Clone for both map versions.
        [TestCase(2)]
        [TestCase(3)]
        public void NonemptyMaterialKeywordsSurviveSaveAndClone(int version)
        {
            var previousVersion = Settings.Instance.MapVersion;
            try
            {
                Settings.Instance.MapVersion = version;
                var shaderKey = version == 2 ? "_shader" : "shader";
                var keywordKey = version == 2 ? "_shaderKeywords" : "shaderKeywords";
                var node = new JSONObject
                {
                    [shaderKey] = "Standard",
                    [keywordKey] = JSON.Parse("[\"DIFFUSE\",\"FOG\"]")
                };

                var material = new Beatmap.Base.Customs.BaseMaterial(node);
                var serialized = material.ToJson()[keywordKey].AsArray;
                Assert.That(serialized.Children.Select(k => (string)k),
                    Is.EqualTo(new[] { "DIFFUSE", "FOG" }),
                    $"v{version}: ToJson changed the authored keyword list.");

                var cloneJson = material.Clone().ToJson()[keywordKey].AsArray;
                Assert.That(cloneJson.Children.Select(k => (string)k),
                    Is.EqualTo(new[] { "DIFFUSE", "FOG" }),
                    $"v{version}: Clone+ToJson changed the authored keyword list.");
            }
            finally
            {
                Settings.Instance.MapVersion = previousVersion;
            }
        }

        // A non-empty authored keyword list on a named Standard material must stay on the
        // lit Standard path (ChroMapper/Lit) with the authored override honored and alpha
        // untouched — the {Count:0} gate is strictly the explicit-empty fallback.
        [UnityTest]
        public IEnumerator StandardMaterialWithKeywordsStaysLit() =>
            NamedMaterialKeywordGuard("Standard", "[\"DIFFUSE\"]",
                expectGlowing: false);

        // An authored Glowing material with shaderKeywords omitted must keep alpha 1 —
        // the zero-alpha rule belongs only to the Standard/BTSPillar empty-array fallback.
        [UnityTest]
        public IEnumerator AuthoredGlowingMaterialKeepsAlpha() =>
            NamedMaterialKeywordGuard("Glowing", null, expectGlowing: true);

        private static IEnumerator NamedMaterialKeywordGuard(
            string shader, string keywordJson, bool expectGlowing)
        {
            var previousAnimations = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            var failures = new List<string>();
            try
            {
                var raw = CreateDifficulty();
                var mat = raw["customData"]["materials"]["glowmat"];
                mat["shader"] = shader;
                if (keywordJson != null) mat["shaderKeywords"] = JSON.Parse(keywordJson);

                yield return TestUtils.ReloadMap(3, raw);
                var container = FindGeometryContainer();
                if (container == null)
                {
                    failures.Add("The geometry enhancement did not spawn a container.");
                }
                else
                {
                    var renderer = container.MpbController.Renderers.FirstOrDefault();
                    if (renderer == null)
                    {
                        failures.Add("The geometry container has no shape renderer.");
                    }
                    else
                    {
                        var resolved = renderer.sharedMaterial;
                        var expectedShader = expectGlowing ? "ChroMapper/Glowing" : "ChroMapper/Lit";
                        if (resolved.shader.name != expectedShader)
                            failures.Add($"shader={resolved.shader.name}, expected {expectedShader}.");
                        if (!expectGlowing)
                        {
                            if (!resolved.IsKeywordEnabled("DIFFUSE"))
                                failures.Add("authored DIFFUSE keyword not enabled on resolved material.");
                            if (resolved.IsKeywordEnabled("SPECULAR"))
                                failures.Add("SPECULAR enabled though not authored.");
                            if (resolved.IsKeywordEnabled("FOG"))
                                failures.Add("FOG enabled though not authored.");
                        }

                        var mpb = new MaterialPropertyBlock();
                        yield return null; // let the appearance push reach the renderer's MPB
                        renderer.GetPropertyBlock(mpb);
                        var spawnColor = mpb.GetColor(ColorId);
                        if (Mathf.Abs(spawnColor.a - 1f) > 0.01f)
                            failures.Add($"spawn MPB _Color.a={spawnColor.a}, expected 1.");

                        // The track's color event interpolates black -> red over beats 4..8.
                        var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
                        atsc.MoveToJsonTime(9f);
                        yield return null;
                        yield return null;
                        renderer.GetPropertyBlock(mpb);
                        var animated = mpb.GetColor(ColorId);
                        if (Mathf.Abs(animated.r - 1f) > 0.01f || animated.g > 0.05f)
                            failures.Add($"beat 9 animated color={animated}, expected red.");
                        if (Mathf.Abs(animated.a - 1f) > 0.01f)
                            failures.Add($"beat 9 MPB _Color.a={animated.a}, expected 1.");
                    }
                }
            }
            finally
            {
                Settings.Instance.Animations = previousAnimations;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static IEnumerator EmptyStandardKeywordsUseZeroAlpha(bool named)
        {
            var previousAnimations = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            var failures = new List<string>();
            try
            {
                var raw = CreateDifficulty();
                var mat = raw["customData"]["materials"]["glowmat"];
                mat["shader"] = "Standard";
                mat["shaderKeywords"] = new JSONArray();
                if (!named)
                {
                    raw["customData"]["environment"][0]["geometry"]["material"] =
                        mat.Clone();
                }

                yield return TestUtils.ReloadMap(3, raw);
                var container = FindGeometryContainer();
                if (container == null)
                {
                    failures.Add("The geometry enhancement did not spawn a container.");
                }
                else
                {
                    var renderer = container.MpbController.Renderers.FirstOrDefault();
                    if (renderer == null)
                    {
                        failures.Add("The geometry container has no shape renderer.");
                    }
                    else
                    {
                        if (renderer.sharedMaterial.shader.name != "ChroMapper/Glowing")
                            failures.Add("shader=" + renderer.sharedMaterial.shader.name +
                                ", expected ChroMapper/Glowing for explicit shaderKeywords:[]");

                        var mpb = new MaterialPropertyBlock();
                        yield return null; // let the appearance push reach the renderer's MPB
                        renderer.GetPropertyBlock(mpb);
                        var spawnColor = mpb.GetColor(ColorId);
                        if (Mathf.Abs(spawnColor.a) > 0.01f)
                            failures.Add($"spawn MPB _Color.a={spawnColor.a}, expected 0 " +
                                "(Glowing conversion zeroes the authored alpha).");

                        // The track's color event interpolates black -> red over beats 4..8.
                        var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
                        atsc.MoveToJsonTime(9f);
                        yield return null;
                        yield return null;
                        renderer.GetPropertyBlock(mpb);
                        var animated = mpb.GetColor(ColorId);
                        if (Mathf.Abs(animated.r - 1f) > 0.01f || animated.g > 0.05f)
                            failures.Add($"beat 9 animated color={animated}, expected red.");
                        if (Mathf.Abs(animated.a) > 0.01f)
                            failures.Add($"beat 9 MPB _Color.a={animated.a}, expected 0.");

                        atsc.MoveToJsonTime(2f);
                        yield return null;
                        yield return null;
                        renderer.GetPropertyBlock(mpb);
                        var reverse = mpb.GetColor(ColorId);
                        if (Mathf.Abs(reverse.a) > 0.01f)
                            failures.Add($"beat 2 reverse MPB _Color.a={reverse.a}, expected 0.");
                    }
                }
            }
            finally
            {
                Settings.Instance.Animations = previousAnimations;
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // The fixture mirrors Give In To You's shape: a materials entry whose "track" field binds a named
        // track, and a geometry cube referencing that material by name.
        private static JSONNode CreateDifficulty()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["geometry"] = new JSONObject
                {
                    ["type"] = "Cube",
                    ["material"] = "glowmat"
                }
            });

            var customEvents = new JSONArray();
            customEvents.Add(new JSONObject
            {
                ["b"] = 4f,
                ["t"] = "AnimateTrack",
                ["d"] = new JSONObject
                {
                    ["track"] = TrackName,
                    ["duration"] = 4f,
                    ["color"] = JSON.Parse("[[0, 0, 0, 1, 0], [1, 0, 0, 1, 1]]")
                }
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["customData"] = new JSONObject
                {
                    ["environment"] = environment,
                    ["customEvents"] = customEvents,
                    ["materials"] = new JSONObject
                    {
                        ["glowmat"] = new JSONObject
                        {
                            ["color"] = JSON.Parse("[0.2, 0.2, 0.2, 1]"),
                            ["shader"] = "OpaqueLight",
                            ["track"] = TrackName
                        }
                    }
                }
            };
        }

        private static GeometryContainer FindGeometryContainer() =>
            Object.FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(container => container.EnvironmentEnhancement != null
                    && container.EnvironmentEnhancement.Geometry != null);

        // Restore the canonical empty shared map so later fixtures do not inherit the material fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
