using System.Collections;
using Beatmap.Animations;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // PointDefinitionBaseProviderTest pins CM's base-property evaluation to Heck's documented semantics
    // (https://heck.aeroluna.dev/animation/tracks-and-points/ "Bases", "Swizzling", "Smooth"): base properties
    // resolve against live editor state, swizzle componentwise in any order, and smooth toward their source
    // across frames, all through the production PointDefinition pipeline. Point definitions use the
    // bare-point shorthand (a bare base string is one point with an appended time of 0).
    public class PointDefinitionBaseProviderTest : TestBase
    {
        private const float Tolerance = 0.001f;

        // BaseHeadTransformFollowsThePreviewCamera: Heck's baseHeadPosition tracks the HMD; CM's preview head
        // is the player camera. The hand bases hold their fresh-session defaults (the editor has no hands).
        [UnityTest]
        public IEnumerator BaseHeadTransformFollowsThePreviewCamera()
        {
            // Head bases follow the bound manager's SelectedCameraController; an arbitrary
            // FindAnyObjectByType pick could return the unselected playing camera.
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera.transform;
            camera.position = new Vector3(6f, 7f, 8f);
            yield return null;

            var head = CreateVector3Definition("[\"baseHeadPosition\"]").Interpolate(0f);
            Assert.That(head.x, Is.EqualTo(camera.position.x).Within(Tolerance), "Head X did not follow the camera.");
            Assert.That(head.y, Is.EqualTo(camera.position.y).Within(Tolerance), "Head Y did not follow the camera.");
            Assert.That(head.z, Is.EqualTo(camera.position.z).Within(Tolerance), "Head Z did not follow the camera.");

            var leftHand = CreateVector3Definition("[\"baseLeftHandPosition\"]").Interpolate(0f);
            Assert.That(leftHand.x, Is.EqualTo(0f).Within(Tolerance), "The editor has no VR controllers, so hand bases hold their fresh-session defaults.");
        }

        // BaseSwizzlingReordersComponents covers the docs' swizzle examples: unordered/repeated components and
        // dimension changes in both directions.
        [UnityTest]
        public IEnumerator BaseSwizzlingReordersComponents()
        {
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera.transform;
            camera.position = new Vector3(1f, 2f, 3f);
            yield return null;

            var mirrored = CreateVector3Definition("[\"baseHeadPosition.zyx\"]").Interpolate(0f);
            Assert.That(mirrored.x, Is.EqualTo(camera.position.z).Within(Tolerance), "Swizzle .zyx must mirror X and Z.");
            Assert.That(mirrored.y, Is.EqualTo(camera.position.y).Within(Tolerance));
            Assert.That(mirrored.z, Is.EqualTo(camera.position.x).Within(Tolerance));

            var raised = CreateVector3Definition("[\"baseSongTime.xxx\"]").Interpolate(0f);
            var songTime = Object.FindAnyObjectByType<AudioTimeSyncController>().CurrentSeconds;
            Assert.That(raised.x, Is.EqualTo(songTime).Within(Tolerance), "baseSongTime.xxx must fill every component.");
            Assert.That(raised.y, Is.EqualTo(songTime).Within(Tolerance));
            Assert.That(raised.z, Is.EqualTo(songTime).Within(Tolerance));

            yield break;
        }

        // BaseColorsMatchTheActiveScheme pins the color bases to the live color scheme.
        [UnityTest]
        public IEnumerator BaseColorsMatchTheActiveScheme()
        {
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var scheme = context.ColorScheme;
            Assert.That(scheme, Is.Not.Null, "The mapper did not load a color scheme.");
            var original = scheme.Clone();
            try
            {
                // Map overrides mutate the active copied scheme after SetColorScheme; base providers must follow that live copy.
                scheme.LeftNoteColor = new Color(0.123f, 0.234f, 0.345f, 0.456f);
                scheme.EnvironmentLeftColor = new Color(0.567f, 0.678f, 0.789f, 0.891f);
                context.NotifyColorScheme();

                var note0 = CreateColorDefinition("[\"baseNote0Color\"]").Interpolate(0f);
                Assert.That(note0.r, Is.EqualTo(scheme.LeftNoteColor.r).Within(Tolerance));
                Assert.That(note0.g, Is.EqualTo(scheme.LeftNoteColor.g).Within(Tolerance));
                Assert.That(note0.b, Is.EqualTo(scheme.LeftNoteColor.b).Within(Tolerance));
                Assert.That(note0.a, Is.EqualTo(scheme.LeftNoteColor.a).Within(Tolerance));

                var environment = CreateColorDefinition("[\"baseEnvironmentColor0\"]").Interpolate(0f);
                Assert.That(environment.r, Is.EqualTo(scheme.EnvironmentLeftColor.r).Within(Tolerance));
            }
            finally
            {
                // Restore shared runtime state even when an assertion fails so later fixtures retain their authored palette.
                scheme.Copy(original);
                context.NotifyColorScheme();
                Object.DestroyImmediate(original);
            }

            yield break;
        }

        // BaseSongTimeTracksThePlaybackClock: baseSongTime tracks the live cursor and baseSongLength the clip.
        [UnityTest]
        public IEnumerator BaseSongTimeTracksThePlaybackClock()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            atsc.MoveToJsonTime(30f);

            var songTime = CreateFloatDefinition("[\"baseSongTime\"]").Interpolate(0f);
            Assert.That(songTime, Is.EqualTo(atsc.CurrentSeconds).Within(Tolerance),
                "baseSongTime must track the live song time in seconds.");

            var songLength = CreateFloatDefinition("[\"baseSongLength\"]").Interpolate(0f);
            Assert.That(songLength, Is.EqualTo(atsc.SongAudioSource.clip.length).Within(Tolerance),
                "baseSongLength must read the loaded clip's length.");

            var combo = CreateFloatDefinition("[\"baseCombo\"]").Interpolate(0f);
            Assert.That(combo, Is.EqualTo(0f).Within(Tolerance), "The preview simulates no gameplay, so combo holds its fresh-session value.");

            var multiplier = CreateFloatDefinition("[\"baseMultiplier\"]").Interpolate(0f);
            Assert.That(multiplier, Is.EqualTo(1f).Within(Tolerance));

            yield break;
        }

        // SmoothedBaseConvergesTowardItsSource: .s[number] starts at the fresh-session default and converges
        // toward the live source as frames advance (Heck's SmoothProvidersValues). The tick count depends on
        // the host's delta time, so the loop runs until converged within a generous frame budget.
        [UnityTest]
        public IEnumerator SmoothedBaseConvergesTowardItsSource()
        {
            var camera = Object.FindAnyObjectByType<CameraManager>().SelectedCameraController.Camera.transform;
            camera.position = new Vector3(6f, 7f, 8f);
            yield return null;

            var definition = CreateVector3Definition("[\"baseHeadPosition.s10\"]");
            var initial = definition.Interpolate(0f);
            Assert.That(initial.x, Is.LessThan(6f), "A smoothed base starts at its fresh-session default, not the live value.");

            // Each tick must move the held state toward the source (monotone convergence regardless of delta time).
            var previous = initial.x;
            for (var frame = 0; frame < 10; ++frame)
            {
                definition.Interpolate(0f);
                yield return null;
                var current = definition.Interpolate(0f).x;
                Assert.That(Mathf.Abs(current - 6f), Is.LessThanOrEqualTo(Mathf.Abs(previous - 6f)),
                    "The smoothed base moved away from its source.");
                previous = current;
            }

            for (var frame = 0; frame < 3000 && Mathf.Abs(definition.Interpolate(0f).x - camera.position.x) >= 0.05f; ++frame)
            {
                yield return null;
            }

            var final = definition.Interpolate(0f);
            Assert.That(Mathf.Abs(final.x - camera.position.x), Is.LessThan(0.05f),
                "The smoothed base did not converge toward the live camera position.");
        }

        // SmoothedBaseStartsFreshAfterTrackMapReset covers the stale-session regression: smoothed base
        // providers hold converged state in a shared cache that survives map loads, so the load-reset seam
        // (TracksManager.ResetAnimationTracks) must clear it. A point re-parsed after the reset must start
        // at the fresh-session default again instead of inheriting the previous session's held value.
        [UnityTest]
        public IEnumerator SmoothedBaseStartsFreshAfterTrackMapReset()
        {
            // A unique smoothed key keeps this assertion independent of providers warmed by other fixtures.
            var definition = CreateFloatDefinition("[\"baseMultiplier.s1_125\"]");
            for (var frame = 0; frame < 12; ++frame)
            {
                definition.Interpolate(0f);
                yield return null;
            }

            var held = definition.Interpolate(0f);
            Assert.That(held, Is.GreaterThan(0f),
                "The smoothed base must converge above its fresh-session default before the reset.");

            // Re-resolve and read in the same frame so Tick()'s once-per-frame guard keeps a genuinely
            // fresh provider at its default instead of masking the assertion with its first-frame lerp.
            Object.FindAnyObjectByType<TracksManager>().ResetAnimationTracks();
            var fresh = CreateFloatDefinition("[\"baseMultiplier.s1_125\"]").Interpolate(0f);

            Assert.That(fresh, Is.LessThan(held * 0.5f),
                "A provider resolved after the track reset must start fresh instead of inheriting the previous session's held value.");
        }

        private static PointDefinition<float> CreateFloatDefinition(string points) =>
            new(PointDataParsers.ParseFloat, BuildParams(points), null);

        private static PointDefinition<Vector3> CreateVector3Definition(string points) =>
            new(PointDataParsers.ParseVector3, BuildParams(points), null);

        private static PointDefinition<Color> CreateColorDefinition(string points) =>
            new(PointDataParsers.ParseColor, BuildParams(points), null);

        private static IPointDefinition.UntypedParams BuildParams(string points) => new()
        {
            Points = JSON.Parse(points),
            Time = 0f,
            TimeBegin = 0f,
            TimeEnd = 0f
        };
    }
}
