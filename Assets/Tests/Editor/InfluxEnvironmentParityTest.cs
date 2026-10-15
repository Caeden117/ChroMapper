using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "influx", mapped by Phøenix (BeatSaver ID: 321a2).
    // The installed Expert+ map first reproduced the y=30 upper shell at beat 6.5. This
    // fixture keeps the two source edits, one clone per blade track, their six beat-0 events,
    // and every customData section; none of its notes, walls, or 36,091 lights drive blades.
    public class InfluxEnvironmentParityTest : TestBase
    {
        private bool animationsBeforeTest;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "InfluxExpertPlusEnvironmentFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 148,
                environmentName: "KDAEnvironment",
                songLengthSeconds: 300);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        protected override void CleanupTestObjects()
        {
        }

        // Influx's V2 _localPosition paths move the shell blades well above and below the
        // player before the beat-6.5 screenshot; the other constructs are a position control.
        [UnityTest]
        public IEnumerator ShellAndHolsterTentaclesFollowV2LocalPositionAtBeatSixPointFive()
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6.5f);
            yield return null;
            yield return null;

            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var containers = geometry.LoadedContainers.Values
                .OfType<GeometryContainer>()
                .ToArray();
            Assert.That(containers.Count(container => container.EnvironmentEnhancement.Track == "TopShell"),
                Is.EqualTo(1), "The fixture did not import its upper blade shell clone.");
            Assert.That(containers.Count(container => container.EnvironmentEnhancement.Track == "BottomShell"),
                Is.EqualTo(1), "The fixture did not import its lower blade shell clone.");

            var upperShell = TargetOf(containers.First(container =>
                container.EnvironmentEnhancement.Track == "TopShell"));
            var lowerShell = TargetOf(containers.First(container =>
                container.EnvironmentEnhancement.Track == "BottomShell"));
            var upperSupport = TargetOf(containers.First(container =>
                container.EnvironmentEnhancement.Track == "TopHolsterSupport"));
            var lowerSupport = TargetOf(containers.First(container =>
                container.EnvironmentEnhancement.Track == "BottomHolsterSupport"));

            // The shell's first 78-beat path eases from 4000 to 550 by normalized
            // time 0.25: at beat 6.5, V2's 0.6-unit scale gives y=943.33.
            Assert.That(upperShell.position.y, Is.EqualTo(943.33f).Within(2f),
                $"Upper shell stayed in the central nest at y={upperShell.position.y:F2}.");
            Assert.That(lowerShell.position.y, Is.EqualTo(-943.33f).Within(2f),
                $"Lower shell stayed in the central nest at y={lowerShell.position.y:F2}.");
            // The support's first position key is at normalized time 0.48, so its
            // beat-6.5 position is still the initial 400 * 0.6 = 240 units.
            Assert.That(upperSupport.position.y, Is.EqualTo(240f).Within(1f),
                $"Upper holster support stayed at y={upperSupport.position.y:F2}.");
            Assert.That(lowerSupport.position.y, Is.EqualTo(-240f).Within(1f),
                $"Lower holster support stayed at y={lowerSupport.position.y:F2}.");
            // Both pairs already received the map's animated scale at this beat; their
            // central screen size came from being much too close to the camera.
            Assert.That(Vector3.Distance(upperShell.localScale, new Vector3(25f, 15f, 2f)),
                Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(lowerShell.localScale, new Vector3(25f, 15f, 2f)),
                Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(upperSupport.localScale, new Vector3(5f, 5f, 0.5f)),
                Is.LessThan(0.01f));
            Assert.That(Vector3.Distance(lowerSupport.localScale, new Vector3(5f, 5f, 0.5f)),
                Is.LessThan(0.01f));
        }

        // Influx's upper support is parented to TopHolster. A local path must retain
        // y=240 relative to that parent when the parent moves; a world-space write would
        // incorrectly pull the support back to absolute y=240 on the next animation frame.
        [UnityTest]
        public IEnumerator HolsterSupportLocalPositionComposesWithParentMovement()
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(6.5f);
            yield return null;
            yield return null;

            var geometry = Object.FindAnyObjectByType<GeometryGridContainer>();
            var support = TargetOf(geometry.LoadedContainers.Values
                .OfType<GeometryContainer>()
                .Single(container => container.EnvironmentEnhancement.Track == "TopHolsterSupport"));
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var parent = tracks.GetAnimationTrack("TopHolster").Track.ObjectParentTransform;
            var initialParentPosition = parent.position;
            try
            {
                parent.position += Vector3.up * 60f;
                yield return null;
                yield return null;

                Assert.That(support.localPosition.y, Is.EqualTo(240f).Within(1f));
                Assert.That(support.position.y, Is.EqualTo(300f).Within(1f),
                    "The upper blade ignored its animated parent and remained at an absolute world position.");
            }
            finally
            {
                parent.position = initialParentPosition;
            }
        }

        private static Transform TargetOf(GeometryContainer container) => container
            .GetComponents<ObjectAnimator>()
            .Single(animator => animator.LocalTarget != null
                && animator.LocalTarget.GetComponent<ChromaIDMarker>() != null)
            .LocalTarget;

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            Settings.Instance.Animations = animationsBeforeTest;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
