using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "DELETE IT ALL", mapped by Mawntee (BeatSaver ID: 4cf62).
    // DELETE IT ALL's beat-363 cut parents the environment after setting its child tracks'
    // world positions. Heck then lets the later parent scale carry those stationary children.
    public class DeleteItAllMapParityTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "DeleteItAllBeat363Fixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        // The original 6.7 MB map reproduced all four positions below before reduction to five
        // environment entries and six custom events. The misplaced neon glass creates CM's white
        // square; the two pillars account for the shallow walls behind it.
        [UnityTest]
        public IEnumerator ParentScaleCarriesStationaryNeonAndWallTracksAtBeat363()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            yield return SeekTo(362.55f);

            var markers = Object.FindObjectsByType<ChromaIDMarker>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var root = markers.Single(marker =>
                marker.ChromaID == "InterscopeEnvironment.[0]Environment").transform;
            var names = new[]
            {
                ".[46]NeonTop",
                ".[47]NeonTop (1)",
                "RearPillar_A_L",
                "RearPillar_A_R"
            };
            var authoredPositions = new[]
            {
                new Vector3(0f, 8.5f, 11.500024f),
                new Vector3(0f, 8.5f, 23f),
                new Vector3(-21f, 0f, -0.75f),
                new Vector3(21f, 0f, -0.75f)
            };
            var targets = names.Select(name => markers.Single(marker =>
                marker.ChromaID.EndsWith(name))).ToArray();
            var failures = new List<string>();
            var rootLocalPositions = new Vector3[targets.Length];
            for (var i = 0; i < targets.Length; i++)
            {
                var actual = targets[i].transform.position;
                if (Vector3.Distance(actual, authoredPositions[i]) > 0.1f)
                    failures.Add($"{names[i]} at beat 362.55: expected {authoredPositions[i]}, got {actual}");
                rootLocalPositions[i] = root.InverseTransformPoint(actual);
            }

            foreach (var beat in new[] { 363.03f, 362.55f, 363.03f })
            {
                yield return SeekTo(beat);
                for (var i = 0; i < targets.Length; i++)
                {
                    var expected = root.TransformPoint(rootLocalPositions[i]);
                    var actual = targets[i].transform.position;
                    if (Vector3.Distance(actual, expected) > 0.1f)
                        failures.Add($"{names[i]} at beat {beat}: expected {expected.ToString("F2")}, " +
                            $"got {actual.ToString("F2")}");
                }
            }

            // At the cut the nearest active NeonTop glass must be below and behind the camera;
            // leaving its old world position in place produces the large white prism in CM.
            var neonGlass = targets[0].GetComponentsInChildren<Renderer>(true)
                .Single(renderer => renderer.name == "BoxLight"
                    && renderer.sharedMaterial != null
                    && renderer.sharedMaterial.name == "EnvLightGlass");
            var camera = cameraManager.CameraControllers[1].Camera;
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (GeometryUtility.TestPlanesAABB(planes, neonGlass.bounds))
                failures.Add("The nearest NeonTop glass still intersects the playing camera frustum.");

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        // A failed assertion must still leave the editor camera and preview mode safe for reload.
        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
                cameraManager.SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = animationsBeforeTest;
            yield break;
        }

        // The large environment scene owns AudioSource references that require a fresh scene on teardown.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
