using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "CENSORED!!", mapped by Saltyfish (BeatSaver ID: 4b3da).
    // The fixture retains the authored TV panel, material keywords, and track animations.
    public class CensoredPanelRenderingTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "CensoredFullMapFixture.json");


        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;

            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment",
                songLengthSeconds: 200);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        [SetUp]
        public void EnablePreviewAnimations()
        {
            Settings.Instance.Animations = true;
        }

        protected override void CleanupTestObjects()
        {
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        private static (int CenterWhite, int OuterWhite, int SaturatedPanel,
            float CenterLum, float EdgeLum) MeasureFrame(Texture2D t)
        {
            var centerWhite = 0;
            var outerWhite = 0;
            var saturatedPanel = 0;
            float centerLum = 0f;
            float edgeLum = 0f;
            var centerCount = 0;
            var edgeCount = 0;
            // Panel viewport x=0.27..0.75, y=0.22..0.72 mapped to pixel space (approximate -
            // the panel vp rect came from the a_* group aggregate, not a fitted quad).
            var pxMin = Mathf.RoundToInt(0.27f * t.width);
            var pxMax = Mathf.RoundToInt(0.75f * t.width);
            var pyMin = Mathf.RoundToInt(0.22f * t.height);
            var pyMax = Mathf.RoundToInt(0.72f * t.height);
            for (var y = 0; y < t.height; y++)
            {
                for (var x = 0; x < t.width; x++)
                {
                    var p = t.GetPixel(x, y);
                    var isCenter = x >= t.width / 4 && x < t.width * 3 / 4
                        && y >= t.height / 4 && y < t.height * 3 / 4;
                    var lum = 0.2126f * p.r + 0.7152f * p.g + 0.0722f * p.b;
                    if (p.r >= 0.9f && p.g >= 0.9f && p.b >= 0.9f)
                    {
                        if (isCenter) centerWhite++; else outerWhite++;
                    }
                    if (x >= pxMin && x < pxMax && y >= pyMin && y < pyMax)
                    {
                        var max = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                        var min = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                        if (max > 0.4f && max - min > 0.25f) saturatedPanel++;
                    }
                    var isEdge = x < t.width / 8 || x >= t.width * 7 / 8
                        || y < t.height / 8 || y >= t.height * 7 / 8;
                    if (isCenter) { centerLum += lum; centerCount++; }
                    else if (isEdge) { edgeLum += lum; edgeCount++; }
                }
            }
            return (centerWhite, outerWhite, saturatedPanel,
                centerCount == 0 ? 0f : centerLum / centerCount,
                edgeCount == 0 ? 0f : edgeLum / edgeCount);
        }

        // The freshly re-downloaded map fixture carries the published
        // file's explicit shaderKeywords:[] on every named Standard material — the precondition is
        // asserted so this variant re-runs the beat-124 visual assertions on original authored data
        // (Chroma upgrades Standard with an explicitly empty shaderKeywords array to the unlit
        // Glowing material).
        [UnityTest]
        public IEnumerator PublishedMapTvPanelHasAuthoredUnlitColors()
        {
            var raw = JSON.Parse(File.ReadAllText(FixturePath));
            Assert.That(raw["customData"]["materials"]["whitebg"]["shaderKeywords"].IsArray, Is.True);
            Assert.That(raw["customData"]["materials"]["whitebg"]["shaderKeywords"].Count, Is.Zero);
            yield return TestUtils.ReloadMap(3, raw, beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();

            // Round-trip through the real save path in memory: serialization must keep each
            // material's explicit shaderKeywords:[] so a save+reload cannot silently revert
            // these surfaces to lit Standard (the installed map was once corrupted this way).
            var output = Beatmap.V3.V3Difficulty.GetOutputJson(BeatSaberSongContainer.Instance.Map);
            foreach (var name in raw["customData"]["materials"].AsObject.Keys)
            {
                var saved = output["customData"]["materials"][name]["shaderKeywords"];
                Assert.That(saved.IsArray, Is.True,
                    $"material '{name}' lost its shaderKeywords key on save.");
                Assert.That(saved.AsArray.Count, Is.Zero,
                    $"material '{name}' shaderKeywords is not empty after save.");
            }

            // Serialize to text and re-parse so the second pass reads exactly what a real save
            // writes — the original JSONNode would keep referencing the live map's parsed data.
            var savedText = output.ToString();

            yield return TestUtils.ReloadMap(3, JSON.Parse(savedText), beatsPerMinute: 195,
                environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();
        }

        // Lifecycle discriminator for the round-trip centering drift above: reload the SAME
        // original file a second time while still in Playing mode. A fresh parse each load
        // separates data differences produced by serialization from reload-lifecycle state.
        [UnityTest]
        public IEnumerator FreshOriginalReloadWhilePlayingKeepsTvAligned()
        {
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();

            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 195, environmentName: "FitBeatEnvironment", songLengthSeconds: 200);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
            yield return Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround();
        }

        // The panel must keep its white surround, saturated colors, and centered red text after seeks.
        [UnityTest]
        public IEnumerator Beat124TvPanelHasVisibleColoursCenteredTextAndWhiteSurround()
        {
            var map = BeatSaberSongContainer.Instance.Map;
            Assert.That(map, Is.Not.Null, "The panel fixture must be loaded.");
            Assert.That(map.EnvironmentEnhancements.Where(e => e.Geometry != null), Is.Not.Empty,
                "The fixture must contain the authored panel geometry.");

            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            var failures = new List<string>();
            var rt = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var frame = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var probe = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var previousTarget = camera.targetTexture;
            try
            {
                foreach (var (beatLabel, beat) in new[]
                {
                    ("115.234", 115.234f),
                    ("123.75", 123.75f), ("124", 124f), ("124.25", 124.25f),
                    ("125", 125f), ("125->124", 124f),
                })
                {
                    atsc.MoveToJsonTime(beat);
                    yield return null;
                    yield return null;

                    var geometries = Object.FindObjectsByType<GeometryContainer>(
                            FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(c => c != null && c.EnvironmentEnhancement != null)
                        .ToList();
                    List<Renderer> LiveCubes(System.Predicate<GeometryContainer> pred) =>
                        geometries.Where(c => pred(c))
                            .SelectMany(c => c.GetComponentsInChildren<Renderer>(true))
                            .Where(r => r != null && r.name == "Cube"
                                && r.enabled && r.gameObject.activeInHierarchy)
                            .ToList();

                    var panelCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track != null
                        && c.EnvironmentEnhancement.Track.StartsWith("a_"));
                    var textCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track != null
                        && c.EnvironmentEnhancement.Track.StartsWith("redcen_"));
                    var backingCubes = LiveCubes(c =>
                        c.EnvironmentEnhancement.Track == "theaaaa");


                    if (beatLabel == "115.234" || beatLabel == "124" || beatLabel == "125->124")
                    {
                        if (panelCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live a_* colour grid cubes");
                        if (textCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live redcen_* text cubes");
                        if (backingCubes.Count == 0)
                            failures.Add($"beat {beatLabel}: no live theaaaa backing cube");
                        if (panelCubes.Count > 0 && textCubes.Count > 0 && backingCubes.Count > 0)
                        {
                            Bounds Combine(List<Renderer> rs)
                            {
                                var b = rs[0].bounds;
                                foreach (var r in rs) b.Encapsulate(r.bounds);
                                return b;
                            }
                            var panelX = camera.WorldToViewportPoint(Combine(panelCubes).center).x;
                            var textX = camera.WorldToViewportPoint(Combine(textCubes).center).x;
                            var backX = camera.WorldToViewportPoint(Combine(backingCubes).center).x;
                            if (Mathf.Abs(textX - panelX) > 0.08f)
                            {
                                failures.Add(
                                    $"beat {beatLabel}: red text vpX={textX:F3} is not centred on " +
                                    $"the colour panel vpX={panelX:F3} (|dx|={Mathf.Abs(textX - panelX):F3} > 0.08)");
                            }
                            if (Mathf.Abs(textX - backX) > 0.08f)
                            {
                                failures.Add(
                                    $"beat {beatLabel}: red text vpX={textX:F3} is not centred on " +
                                    $"the black backing vpX={backX:F3} (|dx|={Mathf.Abs(textX - backX):F3} > 0.08)");
                            }
                        }

                        camera.targetTexture = rt;
                        camera.Render();
                        ReadRenderTexture(rt, frame);
                        camera.targetTexture = previousTarget;
                        var metrics = MeasureFrame(frame);
                        if (metrics.OuterWhite <= 50000)
                        {
                            failures.Add(
                                $"beat {beatLabel}: whitebg surround should read near-white " +
                                $"(outer near-white={metrics.OuterWhite}, expected >50000)");
                        }
                        if (metrics.SaturatedPanel <= 10000)
                        {
                            failures.Add(
                                $"beat {beatLabel}: colour bars should be saturated " +
                                $"(saturatedPanel={metrics.SaturatedPanel}, expected >10000)");
                        }


                        // Contribution evidence: hiding the text renderers must remove solid red
                        // pixels and hiding the black backing must reveal bright content; a
                        // washed-out glow would not produce these deltas on the lit panel.
                        var basePx = frame.GetPixels32();
                        var toggled = new List<Renderer>();
                        camera.targetTexture = rt;
                        try
                        {
                            foreach (var r in textCubes) { r.enabled = false; toggled.Add(r); }
                            camera.Render();
                            ReadRenderTexture(rt, probe);
                        }
                        finally
                        {
                            foreach (var r in toggled) r.enabled = true;
                            camera.targetTexture = previousTarget;
                        }
                        var probePx = probe.GetPixels32();
                        var redTextPixels = 0;
                        for (var i = 0; i < basePx.Length; i++)
                        {
                            if (basePx[i].r >= 180 && basePx[i].g <= 70 && basePx[i].b <= 70
                                && basePx[i].r - probePx[i].r >= 30) redTextPixels++;
                        }
                        if (redTextPixels <= 200)
                        {
                            failures.Add($"beat {beatLabel}: hiding redcen_* removed only " +
                                $"{redTextPixels} red pixels (expected >200; text is not " +
                                "rendering as visible red)");
                        }

                        toggled.Clear();
                        camera.targetTexture = rt;
                        try
                        {
                            foreach (var r in backingCubes) { r.enabled = false; toggled.Add(r); }
                            camera.Render();
                            ReadRenderTexture(rt, probe);
                        }
                        finally
                        {
                            foreach (var r in toggled) r.enabled = true;
                            camera.targetTexture = previousTarget;
                        }
                        probePx = probe.GetPixels32();
                        var revealedPixels = 0;
                        for (var i = 0; i < basePx.Length; i++)
                        {
                            var b = basePx[i];
                            var n = probePx[i];
                            if (Mathf.Max(b.r, Mathf.Max(b.g, b.b)) <= 20
                                && Mathf.Max(n.r, Mathf.Max(n.g, n.b)) >= 80) revealedPixels++;
                        }
                        if (revealedPixels <= 100)
                        {
                            failures.Add($"beat {beatLabel}: hiding theaaaa revealed only " +
                                $"{revealedPixels} bright pixels (expected >100; the black " +
                                "backing is not occluding bright content)");
                        }
                    }
                    else
                    {
                    }
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(rt);
                Object.Destroy(frame);
                Object.Destroy(probe);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
