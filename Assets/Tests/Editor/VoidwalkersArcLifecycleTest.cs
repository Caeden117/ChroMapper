using System.Collections;
using System.IO;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class VoidwalkersArcLifecycleTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects() { }

        [UnityTest]
        public IEnumerator PreviewTransitionsAndPoolReuseKeepArcPositionsFinite(
            [Values(false, true)] bool animations,
            [Values(UIModeType.Preview, UIModeType.Playing)] UIModeType previewMode)
        {
            yield return LoadFixture(animations);
            var mode = Object.FindAnyObjectByType<UIMode>();
            var arcs = Object.FindAnyObjectByType<ArcGridContainer>();
            var cameras = Object.FindAnyObjectByType<CameraManager>();

            mode.SetUIMode(previewMode, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;
            AssertArcPositions(arcs, clock.CurrentJsonTime);

            foreach (var beat in new[] { 82.8f, 86.8f, 90.3f, 188.8f, 190.8f, 82.8f })
            {
                mode.SetUIMode(UIModeType.Normal, false);
                clock.MoveToJsonTime(beat);
                mode.SetUIMode(previewMode, false);
                cameras.SelectCamera(CameraType.Playing);
                yield return null;
                yield return null;
                AssertArcPositions(arcs, beat);

                arcs.RefreshPool(true);
                yield return null;
                yield return null;
                AssertArcPositions(arcs, beat);
            }
        }

        [UnityTest]
        public IEnumerator MovementIsInitializedBeforeTheFirstTimeUpdate()
        {
            yield return LoadFixture(true);
            var root = new GameObject("Fresh Voidwalkers movement provider");
            try
            {
                var provider = root.AddComponent<VariableNJSProvider>();
                provider.Atsc = clock;
                provider.Initialize();

                Assert.That(provider.NoteJumpSpeed, Is.EqualTo(18f));
                Assert.That(provider.HalfJumpDurationInBeats, Is.EqualTo(1.5f).Within(0.001f));
                Assert.That(provider.HalfJumpDistance, Is.EqualTo(18f * 1.5f * 60f / 142f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private IEnumerator LoadFixture(bool animations)
        {
            Settings.Instance.Animations = animations;
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "VoidwalkersArcLifecycleFixture.json");
            // Restore the saved cursor during loading so a test seek cannot initialize movement before preview.
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(fixturePath)),
                editorState: new JSONObject
                {
                    ["components"] = new JSONObject
                    {
                        ["currentJsonTime"] = new JSONObject { ["value"] = 83f }
                    }
                },
                beatsPerMinute: 142f,
                environmentName: "Monstercat2Environment",
                songLengthSeconds: 400,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 18f,
                    NoteStartBeatOffset = -0.5f
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
        }

        private static void AssertArcPositions(ArcGridContainer arcs, float beat)
        {
            Assert.That(arcs.LoadedContainers.Count, Is.GreaterThan(0), $"No arcs loaded at beat {beat}.");
            foreach (ArcContainer arc in arcs.LoadedContainers.Values)
            {
                var context = $"Arc {arc.ArcData.JsonTime}-{arc.ArcData.TailJsonTime} at beat {beat}";
                Assert.That(arc.ArcData.HalfJumpDuration, Is.GreaterThan(0f), context);
                Assert.That(arc.ArcData.HalfJumpDistance, Is.GreaterThan(0f), context);
                AssertFinite(arc.transform.position, context);
                AssertFinite(arc.transform.parent.position, context);
                Assert.That(arc.SplineRenderer.positionCount, Is.EqualTo(ArcContainer.NumSamples + 1), context);
                for (var i = 0; i < arc.SplineRenderer.positionCount; i++)
                    AssertFinite(arc.SplineRenderer.GetPosition(i), context + $" sample {i}");
            }
        }

        private static void AssertFinite(Vector3 position, string context)
        {
            Assert.That(float.IsNaN(position.x) || float.IsInfinity(position.x), Is.False, context + " X");
            Assert.That(float.IsNaN(position.y) || float.IsInfinity(position.y), Is.False, context + " Y");
            Assert.That(float.IsNaN(position.z) || float.IsInfinity(position.z), Is.False, context + " Z");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            var mode = Object.FindAnyObjectByType<UIMode>();
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            var cameras = Object.FindAnyObjectByType<CameraManager>();
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            if (clock != null)
                clock.enabled = previousClockEnabled;

            Settings.Instance.Animations = previousAnimations;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
