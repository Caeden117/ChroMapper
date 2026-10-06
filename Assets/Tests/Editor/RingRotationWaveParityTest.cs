using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "As The World Caves In", mapped by Mawntee & Fatalution (BeatSaver ID: 210e3).
    // RingRotationWaveParityTest pins the reported beat-165.3 ring rotation divergence on "As The World Caves
    // In": in game the active beat-150 ring rotation wave (_step 225, _prop 15, _rotation 0, _speed 0.25)
    // leaves the last ring segment roughly 200-210 degrees from the first on each side, while ChroMapper
    // showed about 235. The game's algorithm (decompiled TrackLaneRingsRotationEffect/TrackLaneRing plus
    // Chroma's RingRotationChromafier.TriggerRotation) is:
    //   - each type-8 event seeds a wave whose base angle is the FIRST RING'S CURRENT DESTINATION plus the
    //     event rotation with the direction sign applied, carrying the event's step/prop/flexy speed;
    //   - at 50 Hz fixed ticks each wave advances its cursor by _prop and assigns
    //     ring[i].destination = base + i * step, newest wave first (Chroma iterates newest-to-oldest);
    //   - after assignments every ring lerps rot = Lerp(rot, destination, 0.02 * speed).
    // The simulation below implements exactly that recurrence from the map's verbatim ring events, and the
    // test requires ChroMapper's preview to land on the same per-ring rotations at the reported beat.
    public class RingRotationWaveParityTest : TestBase
    {
        private const float Bpm = 124f;
        private const float FixedDeltaTime = 0.02f;
        private const int RingCount = 10;
        private const float SampleBeat = 165.3f;

        private static readonly Regex RingChromaId =
            new(@"\[\d+\]PairLaserTrackLaneRing\(Clone\)$", RegexOptions.Compiled);

        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "WorldCavesInEnvironmentEssence.json");

        protected override IEnumerator OnMapLoaded()
        {
            Assert.That(
                PersistentUI.Instance.EnableTransitions,
                Is.False,
                "A preceding fixture re-enabled loading transitions for the shared test mapper.");
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 124,
                environmentName: "TimbalandEnvironment",
                songLengthSeconds: 260);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        protected override void CleanupTestObjects()
        {
        }

        // RingSegmentRotationsMatchGameWaveSimulationAtBeat165 drives the preview continuously through the
        // beat-150 wave (the one active at the reported moment) and asserts each ring's rotation matches the
        // game's wave-plus-lerp simulation, including the per-ring spread the user measured.
        [UnityTest]
        public IEnumerator RingSegmentRotationsMatchGameWaveSimulationAtBeat165()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();

            // Reconstruct the pre-wave state with a seek, then run continuous per-frame time steps across the
            // beat-150 event exactly like real playback time flow.
            atsc.MoveToJsonTime(140f);
            yield return null;
            for (var beat = 140.25f; beat <= 165.3f; beat += 0.25f)
            {
                atsc.MoveToJsonTime(beat);
                yield return null;
            }

            // The wave assigns rings by the manager's Rings array order, not by world position, so sample the
            // rotations through that same ordering.
            var manager = Object.FindAnyObjectByType<TrackLaneRingsManager>();
            Assert.That(manager.Rings, Has.Count.EqualTo(RingCount), "The Timbaland environment must expose ten ring segments.");
            var actual = manager.Rings.Select(ring => ring.CachedTransform.localEulerAngles.z).ToList();
            Debug.Log("[RingParity] actual:   " + string.Join(", ", actual.Select(a => a.ToString("F1"))));

            var expected = SimulateGameRingRotations(SampleBeat);
            Debug.Log("[RingParity] expected: " + string.Join(", ", expected.Select(a => a.ToString("F1"))));

            // The wave's absolute base chains from every earlier wave's accumulated destinations, and the
            // rendered eulers wrap mod 360, so the comparable quantity is each ring's rotation RELATIVE to the
            // first ring: the game's per-ring spread (step 225 partially lerped) is what the user measured.
            for (var i = 1; i < RingCount; i++)
            {
                var delta = Mathf.Abs(Mathf.DeltaAngle(actual[i] - actual[0], expected[i] - expected[0]));
                Assert.That(
                    delta,
                    Is.LessThan(8f),
                    $"Ring {i} sits {Mathf.Repeat(actual[i] - actual[0], 360f):F1} degrees from ring 0 at beat " +
                    $"{SampleBeat} but the game's wave simulation puts it {expected[i] - expected[0]:F1} degrees " +
                    "from ring 0; the ring rotation propagation diverges from the game.");
            }

            // The reported symptom: each segment carries a higher rotation angle than the previous, and the
            // user measured that per-segment step as roughly 200-210 degrees in game. The simulation's per-ring
            // spread (the 225-degree step partially lerped after 15 beats) must land in that measured range for
            // the reference model itself to be right. The raw difference is the visual rotation delta; the
            // eulers only wrap per ring, not per step.
            var expectedPerRing = expected[1] - expected[0];
            Debug.Log($"[RingParity] simulated per-ring spread: {expectedPerRing:F1} degrees");
            Assert.That(
                expectedPerRing,
                Is.InRange(195f, 215f),
                "The game simulation's per-ring rotation spread left the user's measured 200-210 degree range; " +
                "the reference simulation itself is wrong.");
        }

        // SimulateGameRingRotations replays the game's ring rotation pipeline on a 50 Hz fixed-tick grid from
        // song start through the requested beat: each type-8 event seeds a wave (base = first ring's current
        // destination + signed rotation), waves assign ring[i].destination = base + i * step newest-first as
        // their cursors cross rings, and every ring lerps toward its destination each tick.
        private static float[] SimulateGameRingRotations(float endBeat)
        {
            var events = RingRotationEvents();
            var eventIndex = 0;

            var destinations = new float[RingCount];
            var speeds = new float[RingCount];
            var rotations = new float[RingCount];
            var waves = new List<Wave>();

            // The environment's serialized startup wave assigns the same shape as an authored event at scene start.
            var (startupRotation, startupStep, startupProp, startupSpeed) = EnvironmentStartupWave();
            waves.Add(new Wave { Angle = startupRotation, Step = startupStep, Prop = startupProp, Speed = startupSpeed });

            var seconds = 0f;
            var endSeconds = endBeat * 60f / Bpm;
            while (seconds < endSeconds)
            {
                seconds += FixedDeltaTime;
                var beat = seconds * Bpm / 60f;

                while (eventIndex < events.Count && events[eventIndex].Beat <= beat)
                {
                    var authored = events[eventIndex++];
                    // Chroma's TriggerRotation: base = first ring destination + rotation * (rotRight ? -1 : 1),
                    // where rotRight is direction == 1; this map's waves all use direction 0.
                    var sign = authored.Direction == 1 ? -1f : 1f;
                    waves.Add(new Wave
                    {
                        Angle = destinations[0] + (authored.Rotation * sign),
                        Step = authored.Step,
                        Prop = authored.Prop,
                        Speed = authored.Speed,
                        Progress = 0f
                    });
                }

                // Chroma iterates active effects newest-to-oldest, so newer assignments win.
                for (var w = waves.Count - 1; w >= 0; w--)
                {
                    var wave = waves[w];
                    var ring = (long)wave.Progress;
                    wave.Progress += wave.Prop;
                    while (ring < wave.Progress && ring < RingCount)
                    {
                        destinations[(int)ring] = wave.Angle + (ring * wave.Step);
                        speeds[(int)ring] = wave.Speed;
                        ring++;
                    }
                }

                waves.RemoveAll(wave => wave.Progress >= RingCount);

                for (var i = 0; i < RingCount; i++)
                    rotations[i] = Mathf.Lerp(rotations[i], destinations[i], FixedDeltaTime * speeds[i]);
            }

            return rotations;
        }

        private sealed class Wave
        {
            internal float Angle;
            internal float Step;
            internal float Prop;
            internal float Speed;
            internal float Progress;
        }

        private static (float Rotation, float Step, float Prop, float Speed) EnvironmentStartupWave()
        {
            // The Timbaland environment serializes exactly one ring rotation spawner ("PairLaserTrackLaneRings").
            var effect = Object.FindObjectsByType<TrackLaneRingsRotationEffect>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .First();
            return (effect.Rotation, effect.Step, effect.PropagationSpeed, effect.FlexySpeed);
        }

        private static List<(float Beat, float Step, float Prop, float Rotation, int Direction, float Speed)>
            RingRotationEvents()
        {
            var ringEvents = new List<(float Beat, float Step, float Prop, float Rotation, int Direction, float Speed)>();
            foreach (var ev in BeatSaberSongContainer.Instance.Map.Events)
            {
                if (ev.Type != 8) continue;
                ringEvents.Add((
                    ev.JsonTime,
                    ev.CustomStep ?? 0f,
                    ev.CustomProp ?? 1f,
                    ev.CustomRingRotation ?? 0f,
                    ev.CustomDirection ?? -1,
                    ev.CustomSpeed ?? ev.CustomPreciseSpeed ?? 0.5f));
            }

            return ringEvents.OrderBy(e => e.Beat).ToList();
        }

        private static List<Transform> RingMarkers() => Object
            .FindObjectsByType<ChromaIDMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(marker => RingChromaId.IsMatch(marker.ChromaID))
            .Select(marker => marker.transform)
            .ToList();

        // Restore the canonical empty shared map so later fixtures do not inherit this map's state.
        [UnityTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
