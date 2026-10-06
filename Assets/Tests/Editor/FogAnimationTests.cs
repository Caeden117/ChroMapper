using System.Collections;
using System.IO;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // FogAnimationTests reproduces the reported "Spells" light show (song "BillieEnvironment Spells" by
    // acloudyskye, mapped by Joetastic & Swifter; fixture chunks reused verbatim from
    // SpellsLaserWallFixture.json). The map drives BloomFogEnvironment attenuation through
    // AnimateComponent events on the "fog" track: three instant single-point events (beats 207, 413,
    // 415), an 8-beat easeInOutCubic interpolation from 5e-07 to 5e-05 (beat 463), and a final instant
    // event (beat 527). In game, Heck's AnimateComponent mutates the live fog params from these
    // events; the CM preview must land every seek on the same as-if-played attenuation, hold finished
    // values, and restore the authored enhancement attenuation (6e-06, from the map's [0]Environment
    // BloomFogEnvironment component) when scrubbed back before the first event.
    public class FogAnimationTests : TestBase
    {
        private const float AuthoredAttenuation = 6e-06f;

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "SpellsLaserWallFixture.json");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            // 150 BPM matches the real map; the fog events reach beat 527 (210.8 s), so the shared clip
            // must extend past them or AudioTimeSyncController clamps the seek.
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 240);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // AnimateComponentFogEventsDriveBloomFogPreviewSeeks walks the reported event sequence in order
        // and requires every seek to land synchronously on the as-if-played attenuation in both the
        // descriptor (data level) and the shader-facing global (render level), covering the eased
        // interpolation window, the post-finish hold, the final override, the scrub-back restore, and a
        // full map reload that must not carry the animated value into the fresh session.
        [UnityTest]
        public IEnumerator AnimateComponentFogEventsDriveBloomFogPreviewSeeks()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            Assert.That(context, Is.Not.Null, "The mapper did not expose its beatmap runtime context.");
            Assert.That(context.Descriptor, Is.Not.Null, "The mapper did not load an environment descriptor.");

            // (seek beat, expected attenuation). The beat-463 event interpolates [5e-07 @ 0, 5e-05 @ 1
            // easeInOutCubic] over 8 beats, so beat 465 eases to 4*0.25^3 = 0.0625 and beat 467 to 0.5.
            var checks = new[]
            {
                (100f, AuthoredAttenuation), // before every fog event: authored enhancement value holds
                (207.5f, 1e-06f),             // beat 207 instant event
                (414f, 1e-04f),               // beat 413 instant event
                (416f, 5e-07f),               // beat 415 instant event
                (465f, 3.59375e-06f),         // quarter through the eased 463..471 interpolation
                (467f, 2.525e-05f),           // halfway through the eased interpolation
                (471f, 5e-05f),               // interpolation end
                (490f, 5e-05f),               // hold after the animation finished
                (528f, 1e-04f),               // beat 527 instant event overrides the held value
                (0f, AuthoredAttenuation),    // scrub back before the first event restores authored value
            };

            foreach (var (beat, expected) in checks)
            {
                atsc.MoveToJsonTime(beat);
                // No frame yields: the seek itself must land on the as-if-played state.
                Assert.That(
                    context.Descriptor.BloomFogParams.Attenuation,
                    Is.EqualTo(expected).Within(1e-09f),
                    $"Seeking to beat {beat} left the descriptor attenuation at " +
                    $"{context.Descriptor.BloomFogParams.Attenuation} instead of {expected}.");
                Assert.That(
                    Shader.GetGlobalFloat("_CustomFogAttenuation"),
                    Is.EqualTo(expected).Within(1e-09f),
                    $"Seeking to beat {beat} left the shader fog attenuation at " +
                    $"{Shader.GetGlobalFloat("_CustomFogAttenuation")} instead of {expected}.");
            }

            // The events animate attenuation only; the authored [0]Environment height-fog values must
            // survive every animation push above.
            atsc.MoveToJsonTime(528f);
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogHeightFogStartY"),
                Is.EqualTo(1500f).Within(0.0001f),
                "The fog animation clobbered the authored height-fog start Y.");
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogHeightFogHeight"),
                Is.EqualTo(-600f).Within(0.0001f),
                "The fog animation clobbered the authored height-fog height.");

            // Reloading the same map must rebuild fog animation state instead of carrying the previous
            // session's animated attenuation (the WorldCavesInEnvironmentTest class of stale named-track
            // state), so the fresh session restores the authored value before any event fires.
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 150,
                environmentName: "BillieEnvironment",
                songLengthSeconds: 240);

            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            atsc.MoveToJsonTime(100f);
            Assert.That(
                context.Descriptor.BloomFogParams.Attenuation,
                Is.EqualTo(AuthoredAttenuation).Within(1e-09f),
                "The reloaded map carried the previous session's animated attenuation before any event.");
            Assert.That(
                Shader.GetGlobalFloat("_CustomFogAttenuation"),
                Is.EqualTo(AuthoredAttenuation).Within(1e-09f),
                "The reloaded map carried the previous session's animated shader attenuation.");
        }

        // Restore the canonical empty shared map so later fixtures do not inherit the fog fixture.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
