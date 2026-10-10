using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Beatmap.Animations;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // Fixture source: "As The World Caves In", mapped by Mawntee & Fatalution (BeatSaver ID: 210e3).
    // WorldCavesIn* converts the reported "As The World Caves In" map (CustomLevels 210e3, TimbalandEnvironment,
    // V2, Chroma + Noodle) into fixture-backed parity tests. The fixture keeps the map's verbatim environment
    // enhancements, custom events, and the noodle notes around each reported moment. Expected values below encode
    // the game's math, not ChroMapper's: V2 AnimateTrack positions scale by BeatmapConstant.LaneSize (Chroma/NE
    // multiply every authored vector by the same 0.6 note-line distance), AssignTrackParent physically parents
    // matched objects under the animated parent track (so enhanced objects ride their parent track's motion on top
    // of their vanilla positions), and AssignPlayerToTrack moves the player with the "note" track.
    public class WorldCavesInEnvironmentTest : TestBase
    {
        private const float LaneScale = BeatmapConstant.LaneSize;
        private const float PlayerStartZ = 1337f * LaneScale;

        private static readonly Regex RingChromaId =
            new(@"\[\d+\]PairLaserTrackLaneRing\(Clone\)$", RegexOptions.Compiled);

        private bool? animationsBeforeTest;
        private float playerCameraOffsetZBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;

        // The Timbaland fixture load spawns tens of thousands of environment objects, so the class loads it
        // once here instead of per test: each case seeks back through the deterministic scrub machinery, which
        // lands on the same as-if-played state a fresh load produces (TrackScrubParityTest pins that
        // equivalence). CaptureCurrentMapAsSharedBaseline points per-test ResetSharedMapState at this fixture
        // map rather than the canonical empty map.
        protected override IEnumerator OnMapLoaded()
        {
            yield return LoadReportedMap();
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The shared fixture's map objects must survive between tests; the default cleanup would delete every
        // custom event and leave later tests with nothing to seek through.
        protected override void CleanupTestObjects()
        {
        }

        // PlayingModeKeepsNoodleNotesAndEnvironmentConstructsTogetherAtSongStart reproduces the reported mode split
        // where Playing mode showed only the notes: in game the beat-0 AnimateTrack events move the player (note
        // track) AND every construct/laser track to z 1337, so all of them render together around the player while
        // the scaled BigSmokePS stays near the origin, out of sight.
        [UnityTest]
        public IEnumerator PlayingModeKeepsNoodleNotesAndEnvironmentConstructsTogetherAtSongStart()
        {
            // In game the glow lines do NOT ride to the player at song start: the map composes GlowLineLParent's
            // beat-0 z 1337 under TrackConstructionParent's beat-0 z -1337 (AssignTrackParent parents
            // GlowLineLParent under TrackConstructionParent), so the glow line stays at its vanilla position while
            // the player, triangle lasers, and rings move to z 802.2.
            var glowLineMarker = SingleMarker("GlowLineL");
            var glowLineVanillaZ = glowLineMarker.transform.position.z;
            EnterPlayingMode();
            yield return SeekTo(6f);

            var camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerStartZ).Within(2f),
                $"The playing camera did not ride the player track to z {PlayerStartZ} at song start " +
                $"(actual {camera.position.z}); AssignPlayerToTrack(\"note\") must move the player with the track.");

            var noteContainers = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values.ToList();
            Assert.That(noteContainers, Is.Not.Empty, "No noodle note containers existed at beat 6.");
            // Track and noodle animation offsets land on each note's rendered AnimationTarget child, so the
            // visual position is the animator's LocalTarget rather than the logical container anchor.
            var renderedNotes = noteContainers
                .Where(note => note.Animator.LocalTarget != null)
                .Select(note => note.Animator.LocalTarget.position)
                .ToList();
            var noteSample = string.Join(", ", renderedNotes.Take(5).Select(position => position.ToString("F2")));
            Assert.That(
                renderedNotes.Count(position => Mathf.Abs(position.z - camera.position.z) < 120f),
                Is.GreaterThan(0),
                "No noodle note rode the player track near the playing camera; notes and constructs must render " +
                $"together in Playing mode like in game. Camera z {camera.position.z:F2}; rendered note positions: {noteSample}.");

            AssertConstructNearPlayer("Light (4)", "a triangle laser");
            // The glow line must stay at its vanilla position at song start: GlowLineLParent's +1337 cancels
            // TrackConstructionParent's -1337, so riding both levels composes to zero movement exactly like the
            // game's nested ParentObjects.
            Assert.That(
                Mathf.Abs(glowLineMarker.transform.position.z - glowLineVanillaZ),
                Is.LessThan(2f),
                $"The left glow line left its vanilla position at song start (z {glowLineMarker.transform.position.z} " +
                $"vs vanilla {glowLineVanillaZ}); GlowLineLParent's +1337 must compose with TrackConstructionParent's " +
                "-1337 so the glow line stays put, as in game.");
            AssertConstructNearPlayer("Light (4)", "a triangle laser");
            foreach (var ring in RingMarkers())
            {
                Assert.That(
                    Mathf.Abs(ring.transform.position.z - camera.position.z),
                    Is.LessThan(150f),
                    $"Ring '{ring.ChromaID}' stayed at its vanilla position instead of riding RingsParent to the " +
                    $"player (ring z {ring.transform.position.z}, camera z {camera.position.z}).");
            }

            var smoke = SingleMarker("BigSmokePS");
            Assert.That(
                Vector3.Distance(smoke.transform.position, camera.position),
                Is.GreaterThan(700f),
                "The big smoke was visible from the beat-6 player position; the player starts at z 802.2 while the " +
                "smoke stays near the origin until the beat-102 player movement.");
            Assert.That(
                Vector3.Distance(smoke.transform.lossyScale, Vector3.one * 3f),
                Is.LessThan(0.01f),
                "The BigSmokePS environment enhancement did not apply its authored 3x scale.");

            var hiddenColumns = SingleMarker("BackColumns");
            Assert.That(
                hiddenColumns.transform.position.x,
                Is.GreaterThan(400000f),
                $"BackColumns did not ride its beat-0 hide track to x ~418181 (actual " +
                $"{hiddenColumns.transform.position.x}); only the later 1337-track constructs may stay near the player.");
        }

        // PlayerAndConstructsSeparateAroundBeat104 reproduces the reported ~50 second movement: the player track
        // starts easing from z 1337 toward 0 at beat 102, the glow lines and triangle lasers race forward in front
        // of the player, the rings stay around the player until beat 105, and the beat-102 ring rotation event keeps
        // every ring spinning through the movement.
        [UnityTest]
        public IEnumerator PlayerAndConstructsSeparateAroundBeat104()
        {
            EnterPlayingMode();

            // Capture every construct's authored beat-0 position before the beat-102 animations begin; enhanced
            // objects ride their parent track on top of these vanilla offsets, so only deltas are game-parity.
            yield return SeekTo(101f);
            var camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerStartZ).Within(2f),
                "The player was not at the beat-0 track position right before the beat-102 movement began.");
            var glowLine = SingleEnhancedTarget("GlowLineL", "the left glow line");
            var triLight = SingleMarker("Light (4)");
            var ring = RingMarkers().First();
            var glowBase = glowLine.position.z;
            var triLightBase = triLight.transform.position.z;
            var ringBase = ring.transform.position.z;

            yield return SeekTo(104f);
            camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerZ(104f)).Within(2f),
                "The player did not start moving from z 802.2 at the beat-102 AnimateTrack.");
            Assert.That(
                camera.position.z,
                Is.LessThan(PlayerStartZ - 0.05f),
                "The player had not moved at beat 104 even though the beat-102 track animation should have started.");
            Assert.That(
                Mathf.Abs(ring.transform.position.z - camera.position.z),
                Is.LessThan(150f),
                $"The rings were not around the player at beat 104 (ring z {ring.transform.position.z}, player z " +
                $"{camera.position.z}); RingsParent only starts moving them away at beat 105.");

            yield return SeekTo(130f);
            camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerZ(130f)).Within(5f),
                "The player track did not follow the authored easeInOutCubic descent toward z 0.");
            Assert.That(
                glowLine.position.z - glowBase,
                Is.EqualTo(GlowLineZ(130f) - PlayerStartZ).Within(5f),
                "The glow lines did not move forward with the beat-102 easeInCubic track.");
            Assert.That(
                triLight.transform.position.z - camera.position.z,
                Is.GreaterThan(300f),
                "The triangle lasers did not pull ahead of the backwards-moving player around beat 104-130; the " +
                "glow lines stay near the origin in game because TrackConstructionParent's -1337 composes against " +
                "GlowLineLParent's beat-102 motion.");
            Assert.That(
                triLight.transform.position.z - triLightBase,
                Is.EqualTo(TriLightZ(130f) - PlayerStartZ).Within(5f),
                "The triangle lasers did not pull ahead to their beat-132 target position.");
            Assert.That(
                ring.transform.position.z - ringBase,
                Is.EqualTo(RingTrackZ(130f) - PlayerStartZ).Within(5f),
                $"The ring did not race ahead on RingsParent between beats 105 and 130 (moved " +
                $"{ring.transform.position.z - ringBase}).");

            var rotationAt130 = ring.transform.localEulerAngles.z;
            yield return SeekTo(131f);
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(rotationAt130, ring.transform.localEulerAngles.z)),
                Is.GreaterThan(0.5f),
                "The ring segments did not keep spinning while moving with the player; the beat-102 ring rotation " +
                "event must keep rotating every ring through the movement.");
        }

        // PlayerReachesTheSmokeAndAllRingsFaceThePlayerByBeat165 reproduces the reported 1:18-1:25 sequence: the
        // player finishes its beat-102..166.6 approach to z 0 (so the origin-area smoke becomes visible around beat
        // 161), and by beat 165 the RingsParent approach animation has every one of the ten ring segments in front
        // of the player instead of behind it.
        [UnityTest]
        public IEnumerator PlayerReachesTheSmokeAndAllRingsFaceThePlayerByBeat165()
        {
            EnterPlayingMode();

            yield return SeekTo(161f);
            var camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerZ(161f)).Within(5f),
                "The player had not moved back toward the origin-area smoke by beat 161.");
            Assert.That(
                camera.position.z,
                Is.LessThan(50f),
                $"The player was still at z {camera.position.z} at beat 161; the smoke only becomes visible once " +
                "the player reaches the origin area.");

            yield return SeekTo(165f);
            camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerZ(165f)).Within(5f),
                "The player did not stay on its authored approach curve at beat 165.");
            var rings = RingMarkers();
            Assert.That(
                rings,
                Has.Count.EqualTo(10),
                "The Timbaland environment must expose exactly ten PairLaserTrackLaneRing segments for the map's " +
                "Ring1..Ring10 enhancement tracks.");
            var expectedTrackZ = RingTrackZ(165f);
            foreach (var ring in rings)
            {
                // The map's zoom events (type 9) keep drifting every ring's native local z by an amount that varies
                // per segment and beat, exactly like the game's TrackLaneRing zoom, so a world-z delta cannot pin
                // the track motion. Instead verify the composed structure directly: each ring is parented under
                // its own Ring1..Ring10 track's object parent (Noodle's ParentObject parents every child-track
                // object under the animated parent), that track reaches the authored RingsParent approach value,
                // and the ring's zoomed local offset composes on top of it.
                Assert.That(
                    ring.transform.parent,
                    Is.Not.Null,
                    $"Ring '{ring.ChromaID}' was not parented under its Ring track's object parent; Noodle's " +
                    "ParentObject parents every child-track object under the animated parent.");
                Assert.That(
                    ring.transform.parent.position.z,
                    Is.EqualTo(expectedTrackZ).Within(2f),
                    $"Ring '{ring.ChromaID}' track did not reach the RingsParent approach value z " +
                    $"{expectedTrackZ} at beat 165 (actual {ring.transform.parent.position.z}).");
                Assert.That(
                    ring.transform.position.z,
                    Is.GreaterThan(camera.position.z),
                    $"Ring '{ring.ChromaID}' was still behind the player at beat 165 (ring z " +
                    $"{ring.transform.position.z}, player z {camera.position.z}).");
            }

            var smoke = SingleMarker("BigSmokePS");
            Assert.That(
                Vector3.Distance(smoke.transform.position, camera.position),
                Is.LessThan(150f),
                "The smoke was not within view distance of the beat-165 player even though the player has moved " +
                "to the origin area where the smoke lives.");
        }

        // RingSegmentPositionsMatchGameZoomSimulationAtBeat162 reproduces the reported beat-162 divergence:
        // the map's beat-158 type-9 event carries only _speed, so its step falls back to the spawner's
        // alternating default. Beat Saber numbers sameTypeIndex from 1 (the beat-158 event is the EIGHTH
        // type-9 event: even -> _maxPositionStep 3), while ChroMapper numbered it from 0 (index 7 -> min
        // 1.5), so the preview lerped every ring toward half the game's spacing. The simulation below
        // replays the game's TrackLaneRing zoom recurrence verbatim: each type-9 event SetPositions
        // ring[i].destination = i * step at the fixed frame after its callback, then every 50 Hz tick
        // runs posZ = Lerp(posZ, positionOffset.z + destination, 0.02 * moveSpeed) and LateUpdate
        // interpolates the two fixed states.
        [UnityTest]
        public IEnumerator RingSegmentPositionsMatchGameZoomSimulationAtBeat162()
        {
            var manager = Object.FindAnyObjectByType<TrackLaneRingsManager>();
            Assert.That(
                manager.Rings,
                Has.Count.EqualTo(10),
                "The Timbaland environment must expose ten ring segments for the map's Ring1..Ring10 tracks.");
            var positionEffect = Descriptor()
                .BasicEventEffectManager.GetEffect<TrackLaneRingsPositionEffect>(9);
            Assert.That(
                positionEffect,
                Is.Not.Null,
                "The Timbaland environment must register a TrackLaneRingsPositionEffect for Event 9.");

            // Capture each ring's local z before the first zoom event (beat 4) so the simulation starts
            // from the preview's real init state rather than assuming the scene's serialized offsets.
            yield return SeekTo(2f);
            var startZ = manager.Rings
                .Select(ring => ring.CachedTransform.localPosition.z)
                .ToArray();
            var offsets = manager.Rings
                .Select(ring => ring.PositionOffset.z)
                .ToArray();

            yield return SeekTo(162f);
            var spawner = positionEffect.Visual;
            var expected = SimulateGameRingZoom(
                2f,
                162f,
                startZ,
                offsets,
                spawner.MinPositionStep,
                spawner.MaxPositionStep,
                spawner.MoveSpeed);
            var actual = manager.Rings
                .Select(ring => ring.CachedTransform.localPosition.z)
                .ToArray();
            Debug.Log("[RingZoom] actual:   " + string.Join(", ", actual.Select(z => z.ToString("F2"))));
            Debug.Log("[RingZoom] expected: " + string.Join(", ", expected.Select(z => z.ToString("F2"))));
            for (var i = 0; i < manager.Rings.Count; i++)
            {
                Assert.That(
                    actual[i],
                    Is.EqualTo(expected[i]).Within(1.5f),
                    $"Ring {i} sits at local z {actual[i]:F2} at beat 162 but the game's zoom recurrence puts " +
                    $"it at {expected[i]:F2}; the beat-158 speed-only event must take the even-index " +
                    "(maxPositionStep) branch like the game's 1-based sameTypeIndex.");
            }
        }

        // KaleidoscopeDuplicatesCloneAndSwirlAtBeat266 reproduces the reported 2:10 fractal: the map duplicates the
        // environment constructs 160 times, hides most clones on a 696969 track at beat 0, then swirls every clone
        // around the player with per-clone easeOutBack kaleidoscope tracks starting at beat 266.
        [UnityTest]
        public IEnumerator KaleidoscopeDuplicatesCloneAndSwirlAtBeat266()
        {
            AssertCloneCount("PlayersPlace(Clone)", 24, "PlayersPlace");
            AssertCloneCount("GlowLineL(Clone)", 16, "GlowLineL");
            AssertCloneCount("GlowLineR(Clone)", 16, "GlowLineR");
            AssertCloneCount("BackColumns(Clone)", 16, "BackColumns");
            AssertCloneCount("MainStructure(Clone)", 16, "MainStructure");
            AssertCloneCount("TopStructure(Clone)", 16, "TopStructure");
            AssertCloneCount("Buildings(Clone)", 24, "Buildings");
            AssertCloneCount("TrackMirror(Clone)", 16, "TrackMirror");
            AssertCloneCount("TrackConstruction(Clone)", 16, "TrackConstruction");

            // Every Ring1..Ring10 enhancement must capture exactly one ring clone; the map's ring regexes target
            // the in-game ring indices, so ChroMapper's Timbaland ChromaIDs must resolve them the same way.
            for (var ringIndex = 1; ringIndex <= 10; ringIndex++)
            {
                Assert.That(
                    FindEnhancedTargets($"Ring{ringIndex}"),
                    Has.Count.EqualTo(1),
                    $"The Ring{ringIndex} environment enhancement did not capture exactly one ring clone.");
            }

            EnterPlayingMode();
            yield return SeekTo(267f);
            var playerPlaceClone = FindEnhancedTargets("PlayersPlace0").Single();
            var buildingsClone = FindEnhancedTargets("Buildings0").Single();
            var playerPlaceBase = playerPlaceClone.position;
            // The clones ride rigidly, so their authored offset from the animated parent track pivot is the
            // local transform captured before the swirl; the composed expectations below need it because the
            // game's ParentObject rotation also swings the translated pivot around the child.
            var playerPlaceCloneLocal = playerPlaceClone.localPosition;
            var playerPlaceCloneLocalRotation = playerPlaceClone.localRotation;
            var buildingsBase = buildingsClone.position;
            var buildingsCloneLocal = buildingsClone.localPosition;

            // The kaleidoscope peaks at each track's authored point: PlayersPlaceParent0 (event 266.08, duration
            // 71.75) reaches position (-7, -35.5, 63) at its 0.769 point and rotation (0, -21, -97) at 0.75.
            // Noodle's ParentObject composes the animated rotation above the animated translation
            // (ParentObject.Update writes localRotation and the rotation-pre-multiplied localPosition), so a
            // clone at local offset L renders at rotation * (position * LaneScale + L); comparing against the
            // translation alone would miss the swing of the translated pivot.
            yield return SeekTo(266.08f + 0.769f * 71.75f);
            Assert.That(
                Vector3.Distance(
                    playerPlaceClone.position - playerPlaceBase,
                    PlayersPlaceParent0World(266.08f + 0.769f * 71.75f, playerPlaceCloneLocal)
                        - PlayersPlaceParent0World(267f, playerPlaceCloneLocal)),
                Is.LessThan(3f),
                $"The PlayersPlace0 clone did not reach its kaleidoscope position (moved " +
                $"{playerPlaceClone.position - playerPlaceBase}); duplicated constructs must swirl with their " +
                "per-clone parent tracks.");
            Assert.That(
                Vector3.Distance(
                    buildingsClone.position - buildingsBase,
                    BuildingsParent0World(266.08f + 0.769f * 71.75f, buildingsCloneLocal)
                        - BuildingsParent0World(267f, buildingsCloneLocal)),
                Is.LessThan(3f),
                $"The Buildings0 clone did not reach its kaleidoscope position (moved " +
                $"{buildingsClone.position - buildingsBase}); every duplicated track family must animate.");

            yield return SeekTo(266.08f + 0.75f * 71.75f);
            Assert.That(
                Quaternion.Angle(
                    playerPlaceClone.rotation,
                    PlayersPlaceParent0Rotation(266.08f + 0.75f * 71.75f) * playerPlaceCloneLocalRotation),
                Is.LessThan(3f),
                $"The PlayersPlace0 clone did not reach its kaleidoscope rotation (actual {playerPlaceClone.rotation}); " +
                "duplicated constructs must rotate with their parent tracks, not only translate.");
        }

        // RunwayDuplicatesInFrontOfThePlayerAtMapStart reproduces the reported intro: the map duplicates the
        // Timbaland runway constructs and rides the TrackConstructionParent1 family to the beat-0 player position
        // (z 1337), so the duplicated TrackMirror/TrackConstruction runway sits right in front of the playing
        // camera in the same spot the vanilla environment puts it, with the rings spinning in over it. CM showed
        // the rings but stranded the runway clones at the beat-0 hide position (x 696969) instead.
        [UnityTest]
        public IEnumerator RunwayDuplicatesInFrontOfThePlayerAtMapStart()
        {
            // This case specifically pins the reported load -> Playing mode path with no seek in between, so it
            // keeps its own fresh load; the re-captured baseline keeps the shared fixture coherent for the
            // remaining tests.
            yield return LoadReportedMap();
            TestUtils.CaptureCurrentMapAsSharedBaseline();

            // The map duplicates the runway constructs 16 times for the kaleidoscope; the "1" family is the one
            // that assembles the world in front of the player at song start.
            AssertCloneCount("TrackMirror(Clone)", 16, "TrackMirror");
            AssertCloneCount("TrackConstruction(Clone)", 16, "TrackConstruction");

            EnterPlayingMode();
            // The reported workflow is load -> Playing mode with no seek: the beat-0 events on the runway
            // track have already fired before the first Playing frame, so the runway must already sit in
            // front of the camera. Asserting only after a later seek masked this ordering regression when
            // the user reported it (the beat-0 696969 hide must lose to the later beat-0 instant show).
            yield return null;
            yield return null;
            var camera = PlayingCamera();
            Assert.That(
                camera.position.z,
                Is.EqualTo(PlayerStartZ).Within(2f),
                "The playing camera did not ride the player track to the song-start position on the first " +
                "Playing-mode frame.");

            // The runway duplicate family rides TrackConstructionParent1 (beat-0 z 1337). Its _position
            // property must resolve the two same-beat events in file order: the 696969 hide first, the
            // instant 1337 show second — Heck's Property.Init makes the last applied event the base.
            var runwayTrack = FindTrackAnimator("TrackConstructionParent1");
            var runwayPosition = (AnimateProperty<Vector3>)runwayTrack.AnimatedProperties["_position"];
            Assert.That(
                runwayPosition.GetLerpedValue(0f).z,
                Is.EqualTo(1337f).Within(0.01f),
                $"TrackConstructionParent1's beat-0 instant AnimateTrack lost to the 696969 hide at the same " +
                $"beat (evaluated z {runwayPosition.GetLerpedValue(0f).z}); same-time events must resolve in " +
                "file order, not whatever List.Sort leaves behind.");

            var runwayTrackConstruction = SingleEnhancedTarget("TrackConstruction1", "duplicated TrackConstruction runway");
            var runwayTrackMirror = SingleEnhancedTarget("TrackMirror1", "duplicated TrackMirror runway");
            foreach (var clone in new[] { runwayTrackConstruction, runwayTrackMirror })
            {
                Assert.That(
                    Mathf.Abs(clone.position.z - camera.position.z),
                    Is.LessThan(150f),
                    $"The duplicated runway construct '{clone.name}' stayed at its beat-0 hide position " +
                    $"on the first Playing-mode frame (z {clone.position.z:F2}, camera z " +
                    $"{camera.position.z:F2}); in game the duplicated runway duplicates in front of the player " +
                    "at song start with the rings spinning in over it.");
            }

            // The same state must still hold after an intro seek; a regression that only breaks the no-seek
            // path (or only the seeked path) is still a parity failure.
            yield return SeekTo(6f);
            camera = PlayingCamera();
            Assert.That(
                Mathf.Abs(runwayTrackConstruction.position.z - camera.position.z),
                Is.LessThan(150f),
                "The duplicated runway was not in front of the player after seeking to beat 6 even though the " +
                "beat-0 events leave it riding TrackConstructionParent1 through the whole intro.");

            // The runway stays in front of the player camera through the whole intro, so a later intro beat must
            // still find it around the player until the beat-102 movement begins.
            yield return SeekTo(20f);
            camera = PlayingCamera();
            Assert.That(
                Mathf.Abs(runwayTrackConstruction.position.z - camera.position.z),
                Is.LessThan(150f),
                "The duplicated runway left the player before the beat-102 movement; it must stay in front of the " +
                "player camera the whole intro like the vanilla Timbaland runway.");
        }

        // RunwayAndTrapezoidLightsFollowChromaLightIdsAroundBeat170 reproduces the reported 1:22 light bug: the
        // map's type-4 events author Chroma lightID [1,2] for the two runway edge lights (GlowLineL/GlowLineR,
        // _lights[5] indexes 15 and 9) and [3,4,5,6] for the four trapezoid logo lights at the back of the
        // runway (Light (4),(6),(5),(7), indexes 11,10,8,14). CM's baked remap used a different index space
        // than Chroma's LightIDTableManager, so the runway pair never lit together and the back lights were
        // hit/missed by the wrong keys.
        [UnityTest]
        public IEnumerator RunwayAndTrapezoidLightsFollowChromaLightIdsAroundBeat170()
        {
            EnterPlayingMode();

            // RunwayAndTrapezoidLightsFollowChromaLightIdsAroundBeat170 must resolve the fixture's original
            // lights by their exported Chroma IDs: a scene-wide name lookup can also find lights left by other
            // fixtures, as the full-suite failure showed while the isolated class remained green.
            var runwayLights = new[]
            {
                OriginalSlotFiveLight(23, "GlowLineL"),
                OriginalSlotFiveLight(24, "GlowLineR")
            };
            var trapezoidLights = new[]
            {
                OriginalSlotFiveLight(25, "Light (4)"),
                OriginalSlotFiveLight(27, "Light (5)"),
                OriginalSlotFiveLight(26, "Light (6)"),
                OriginalSlotFiveLight(29, "Light (7)")
            };

            // Beat 120 is the clean discriminator: [1,2] has been on since 101.938 while the [3,4,5,6] off
            // at beat 112 keeps all four trapezoids dark until 166, so in game the runway pair is lit and
            // every trapezoid light is off.
            yield return SeekTo(120f);
            foreach (var controller in runwayLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.GreaterThan(0.5f),
                    $"Runway light '{controller.name}' was not lit at beat 120 even though the map's type-4 " +
                    "lightID [1,2] on-events drive it; Chroma's lightID table maps keys 1,2 to GlowLineL/R.");
            }
            foreach (var controller in trapezoidLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.LessThan(0.5f),
                    $"Trapezoid light '{controller.name}' was lit at beat 120 even though the map's beat-112 " +
                    "type-4 off event targets lightID [3,4,5,6]; the off must reach all four back-of-runway " +
                    "lights.");
            }

            // At beat 169.9 both groups are on, and the authored _color streams pin the mapping more strongly
            // than lit/off: the [1,2] events carry alpha 0.633 at 169.875 while the [3,4,5,6] events carry
            // alpha 0.427. Timbaland's normal ColorSO multiplies both by .7490196 before dispatch.
            yield return SeekTo(169.9f);
            foreach (var controller in runwayLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.EqualTo(0.633f * 0.7490196f).Within(0.005f),
                    $"Runway light '{controller.name}' showed alpha {controller.Color.a} at beat 169.9 instead " +
                    "of the [1,2] stream's native alpha 0.633*0.7490196; Chroma maps keys 1,2 to GlowLineL/R.");
            }
            foreach (var controller in trapezoidLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.EqualTo(0.427f * 0.7490196f).Within(0.005f),
                    $"Trapezoid light '{controller.name}' showed alpha {controller.Color.a} at beat 169.9 " +
                    "instead of the [3,4,5,6] stream's native alpha 0.427*0.7490196; the back lights must " +
                    "all receive keys 3-6.");
            }

            // At beat 170 the map turns [3,4,5,6] off while [1,2] stays on (last on at 170.25), so at 170.3
            // in game the runway pair is lit and all four trapezoid lights are off.
            yield return SeekTo(170.3f);
            foreach (var controller in runwayLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.GreaterThan(0.5f),
                    $"Runway light '{controller.name}' dropped out at beat 170.3 even though the map keeps " +
                    "lightID [1,2] on past beat 170.25.");
            }
            foreach (var controller in trapezoidLights)
            {
                Assert.That(
                    controller.Color.a,
                    Is.LessThan(0.5f),
                    $"Trapezoid light '{controller.name}' stayed lit at beat 170.3 even though the map's " +
                    "beat-170 type-4 off event targets lightID [3,4,5,6]; the off must reach all four " +
                    "back-of-runway lights.");
            }
        }

        // KaleidoscopeClonesRideHeckEaseOutBackAtBeat287 reproduces the reported 265-281.5 rotation desync:
        // Chroma resolves point-data easings through Heck's own table, whose Back family is a sine-overshoot
        // variant (easeOutBack peaks near 1.375) rather than the easings.net curve CM used (peak near 1.088).
        // Mid-ease the swirl clones therefore sit tens of degrees apart even though both sides evaluate the
        // same authored point data.
        [UnityTest]
        public IEnumerator KaleidoscopeClonesRideHeckEaseOutBackAtBeat287()
        {
            EnterPlayingMode();
            yield return SeekTo(267f);
            var tcClone = FindEnhancedTargets("TrackConstruction0").FirstOrDefault();
            var glClone = FindEnhancedTargets("GlowLineL0").FirstOrDefault();
            Assert.That(tcClone, Is.Not.Null, "The TrackConstruction0 clone did not spawn.");
            Assert.That(glClone, Is.Not.Null, "The GlowLineL0 clone did not spawn.");
            var tcLocal = tcClone.localPosition;
            var tcLocalRot = tcClone.localRotation;
            var glLocal = glClone.localPosition;
            var glLocalRot = glClone.localRotation;

            // Beat 287.3 lands mid-overshoot on TrackConstructionParent0's easeOutBack segment (event 266.055,
            // duration 69, segment ending at point time 0.769): Heck eases to ~1.36 there while the classic
            // curve only reaches ~1.03, so a wrong easing table reads ~35 degrees off on the -108 target.
            const float beat = 287.3f;
            yield return SeekTo(beat);

            var expectedRot = TcParent0Rot(beat) * tcLocalRot;
            var expectedPos = TcParent0Rot(beat) * (TcParent0Pos(beat) + tcLocal);
            Assert.That(
                Quaternion.Angle(tcClone.rotation, expectedRot),
                Is.LessThan(2f),
                $"TrackConstruction0 clone rotation {tcClone.rotation.eulerAngles} at beat {beat} does not " +
                $"match the game's Heck-eased swirl (expected {expectedRot.eulerAngles}); Chroma evaluates " +
                "easeOutBack with its sine-overshoot variant, not the easings.net curve.");
            Assert.That(
                Vector3.Distance(tcClone.position, expectedPos),
                Is.LessThan(0.5f),
                $"TrackConstruction0 clone position {tcClone.position} at beat {beat} does not match the " +
                $"game's Heck-eased swirl (expected {expectedPos}).");

            var expectedGlRot = TcParent0Rot(beat) * glLocalRot;
            var expectedGlPos = TcParent0Rot(beat) * (TcParent0Pos(beat) + glLocal);
            Assert.That(
                Quaternion.Angle(glClone.rotation, expectedGlRot),
                Is.LessThan(2f),
                $"GlowLineL0 clone rotation {glClone.rotation.eulerAngles} at beat {beat} does not match " +
                $"the game's Heck-eased swirl (expected {expectedGlRot.eulerAngles}); the three-level " +
                "track chain must evaluate the same easing variant as its parent.");
            Assert.That(
                Vector3.Distance(glClone.position, expectedGlPos),
                Is.LessThan(0.5f),
                $"GlowLineL0 clone position {glClone.position} at beat {beat} does not match the game's " +
                $"Heck-eased swirl (expected {expectedGlPos}).");
        }

        // NoteTrackCameraRollFollowsHeckEaseInOutBack reproduces the global part of the reported desync: the
        // note track's 70-beat camera swing uses easeInOutBack, which Heck also evaluates with the sine
        // variant, so every object reads rotated when the camera roll is computed with the classic curve.
        [UnityTest]
        public IEnumerator NoteTrackCameraRollFollowsHeckEaseInOutBack()
        {
            EnterPlayingMode();
            yield return SeekTo(267f);
            var baseRotation = PlayingCamera().rotation;

            // Beat 276.5 sits early in the note track's first segment (event 266, duration 70, segment ending
            // at point time 0.75 with easeInOutBack): Heck's sine variant overshoots to about -18 degrees
            // while the classic curve only reaches about -10.7.
            const float beat = 276.5f;
            yield return SeekTo(beat);

            var actualDelta = PlayingCamera().rotation * Quaternion.Inverse(baseRotation);
            var expectedDelta = HeckNoteRotation(beat) * Quaternion.Inverse(HeckNoteRotation(267f));
            Assert.That(
                Quaternion.Angle(actualDelta, expectedDelta),
                Is.LessThan(2f),
                $"The playing camera's note-track roll delta {actualDelta.eulerAngles} at beat {beat} does " +
                $"not match the game's Heck-eased swing (expected {expectedDelta.eulerAngles}); Chroma " +
                "evaluates easeInOutBack with its sine variant, not the easings.net curve.");
        }

        // LoadedWorldCavesInFogLightsHaveRenderTransforms reproduces the reported load-time crash: the
        // player log shows repeated NullReferenceException in BloomFogObject.ApplyToQuad right after this
        // fixture's 190 environment enhancements load, because scene and cloned fog lights register in
        // AllBloomFogLights from OnEnable before their render transform exists (ApplyToQuad dereferences
        // CachedTransform). Every registered light must already have its render transform once the map
        // load hands control back.
        [Test]
        public void LoadedWorldCavesInFogLightsHaveRenderTransforms()
        {
            var activeFogLights = BloomFogObject.AllBloomFogLights
                .Where(light => light != null && light.gameObject.activeInHierarchy)
                .ToList();
            Assert.That(
                activeFogLights,
                Is.Not.Empty,
                "The loaded Timbaland map must expose at least one active bloom fog light.");

            var missingTransform = activeFogLights
                .Where(light => light.CachedTransform == null)
                .ToList();
            Assert.That(
                missingTransform,
                Is.Empty,
                $"{missingTransform.Count} active bloom fog light(s) have no render transform for " +
                $"ApplyToQuad to dereference: {string.Join(", ", missingTransform.Take(8).Select(light => light.name))}");
        }

        // TrackConstructionParent0's beat-266.055 kaleidoscope animation (duration 69), verbatim point data.
        private static Quaternion TcParent0Rot(float beat) => EvaluatePointTrackRotation(
            new[] { Vector3.zero, new Vector3(-1f, 4f, -108f), Vector3.zero },
            new[] { 0f, 0.769f, 1f },
            new[] { "easeLinear", "easeOutBack", "easeInOutCubic" },
            266.054993f, 69f, beat);

        private static Vector3 TcParent0Pos(float beat) => EvaluatePointTrack(
            new[] { Vector3.zero, new Vector3(5f, -51f, 12f), new Vector3(0f, 0f, 350f), new Vector3(696969f, 0f, 350f) },
            new[] { 0f, 0.769f, 0.999f, 1f },
            new[] { "easeLinear", "easeOutBack", "easeInOutCubic", "easeLinear" },
            266.054993f, 69f, beat) * LaneScale;

        // Per test only the mode/camera need restoring; the fixture map deliberately stays loaded for the
        // next case (the six shared-load tests used to each pay a full Timbaland + enhancement reload).
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

            yield break;
        }

        // Restore the canonical empty shared map and the editing camera so later fixtures do not inherit the
        // Timbaland environment, the fixture collections, or the transient Playing camera selection. Runs once
        // per class now that the tests share the fixture load.
        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            if (uiMode != null)
            {
                uiMode.SetUIMode(UIModeType.Normal, false);
            }

            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
            }

            if (animationsBeforeTest.HasValue)
            {
                Settings.Instance.Animations = animationsBeforeTest.Value;
                animationsBeforeTest = null;
            }

            // The camera rig offset is a ChroMapper viewing preference, not map parity; restore the user's value.
            Settings.Instance.PlayerCameraOffsetZ = playerCameraOffsetZBeforeTest;

            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // The fixture is the reported map's environment-enhancement essence: the verbatim 190-entry _environment
        // array, all 448 custom events, and the noodle notes around each reported moment, loaded at the map's real
        // 124 BPM against its real TimbalandEnvironment scene.
        private static string FixturePath => Path.Combine(
            Application.dataPath,
            "Tests",
            "Fixtures",
            "WorldCavesInEnvironmentEssence.json");

        private IEnumerator LoadReportedMap()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            // The camera rig offset is a ChroMapper viewing preference (default 3.6m behind the track position),
            // not map parity; zero it so the camera reads the player track position exactly.
            playerCameraOffsetZBeforeTest = Settings.Instance.PlayerCameraOffsetZ;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(FixturePath)),
                beatsPerMinute: 124,
                environmentName: "TimbalandEnvironment",
                songLengthSeconds: 260);
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        // Playing mode keeps the editing camera saved; selecting the production playing camera mirrors what the
        // digit-5 shortcut does through UIMode.UpdateCameraOnUIModeToggle.
        private void EnterPlayingMode()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
        }

        private IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        private Transform PlayingCamera() => cameraManager.CameraControllers[1].transform;

        private static EnvironmentDescriptor Descriptor() =>
            Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;

        private static ChromaIDMarker SingleMarker(string nameSuffix) => Descriptor().ChromaIDMarkers
            .Single(marker => marker.ChromaID.EndsWith(nameSuffix));

        // RunwayAndTrapezoidLightsFollowChromaLightIdsAroundBeat170 identifies the authored light in the
        // active Timbaland environment, so unrelated scene lights cannot make its lookup ambiguous.
        private static LightController OriginalSlotFiveLight(int sourceIndex, string name)
        {
            var chromaId = $"TimbalandEnvironment.[0]Environment.[{sourceIndex}]{name}";
            var markers = Descriptor().ChromaIDMarkers
                .Where(marker => marker.ChromaID == chromaId)
                .ToList();
            Assert.That(markers, Has.Count.EqualTo(1), $"Expected one original light marker '{chromaId}'.");
            var controller = markers[0].GetComponent<LightController>();
            Assert.That(controller, Is.Not.Null, $"Original light marker '{chromaId}' has no LightController.");
            Assert.That(controller.Type, Is.EqualTo(4), $"Original light '{chromaId}' is not an event-type-4 light.");
            return controller;
        }

        private static List<ChromaIDMarker> RingMarkers() => Descriptor().ChromaIDMarkers
            .Where(marker => RingChromaId.IsMatch(marker.ChromaID))
            .ToList();

        // Environment enhancements attach one ObjectAnimator per matched (or duplicated) object to their
        // enhancement's GeometryContainer, so the animator's LocalTarget is the enhanced scene transform.
        private static List<Transform> FindEnhancedTargets(string trackName) => Object
            .FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(container => container.EnvironmentEnhancement?.Track == trackName)
            .SelectMany(container => container.GetComponents<ObjectAnimator>())
            .Select(animator => animator.LocalTarget)
            .Where(target => target != null)
            .ToList();

        private static Transform SingleEnhancedTarget(string trackName, string description)
        {
            var targets = FindEnhancedTargets(trackName);
            Assert.That(
                targets,
                Has.Count.EqualTo(1),
                $"The {description} environment enhancement on track '{trackName}' must capture its scene object.");
            return targets[0];
        }

        private static TrackAnimator FindTrackAnimator(string name) => Object
            .FindObjectsByType<TrackAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(animator => animator.name == name);

        private void AssertConstructNearPlayer(string nameSuffix, string description)
        {
            var marker = SingleMarker(nameSuffix);
            Assert.That(
                Mathf.Abs(marker.transform.position.z - PlayerStartZ),
                Is.LessThan(150f),
                $"{description} ('{marker.ChromaID}') did not ride its beat-0 track to the player position z " +
                $"{PlayerStartZ} (actual z {marker.transform.position.z}); in game every construct around the " +
                "player moves with it at song start.");
        }

        private static void AssertCloneCount(string cloneSuffix, int expected, string sourceName)
        {
            var clones = Descriptor().ChromaIDMarkers
                .Where(marker => marker.ChromaID.EndsWith(cloneSuffix))
                .ToList();
            Assert.That(
                clones,
                Has.Count.EqualTo(expected),
                $"The map duplicates {sourceName} {expected} times through environment enhancements; " +
                $"ChroMapper created {clones.Count}.");
        }

        // AnimateTrack "note" (the player track), beat 102, duration 68: z 1337 -> 0 at point time 0.95.
        private static float PlayerZ(float beat)
        {
            if (beat < 102f) return PlayerStartZ;
            var progress = Mathf.Clamp01(((beat - 102f) / 68f) / 0.95f);
            return PlayerStartZ * (1f - Easing.Named("easeInOutCubic")(progress));
        }

        // AnimateTrack "GlowLineLParent"/"GlowLineRParent", beat 102, duration 30: z 1337 -> 1666, easeInCubic.
        private static float GlowLineZ(float beat)
        {
            if (beat < 102f) return PlayerStartZ;
            var eased = Easing.Named("easeInCubic")(Mathf.Clamp01((beat - 102f) / 30f));
            return Mathf.Lerp(1337f, 1666f, eased) * LaneScale;
        }

        // AnimateTrack "TriLightParent", beat 102, duration 30: z 1337 -> 2337, easeInOutCubic.
        private static float TriLightZ(float beat)
        {
            if (beat < 102f) return PlayerStartZ;
            var eased = Easing.Named("easeInOutCubic")(Mathf.Clamp01((beat - 102f) / 30f));
            return Mathf.Lerp(1337f, 2337f, eased) * LaneScale;
        }

        // AnimateTrack "RingsParent": beat 105, duration 30, z 1337 -> 3000 (easeInQuart); beat 135, duration 41,
        // z 420 -> 5 (easeInOutCubic).
        private static float RingTrackZ(float beat)
        {
            if (beat < 105f) return PlayerStartZ;
            if (beat < 135f)
            {
                var eased = Easing.Named("easeInQuart")(Mathf.Clamp01((beat - 105f) / 30f));
                return Mathf.Lerp(1337f, 3000f, eased) * LaneScale;
            }

            var approach = Easing.Named("easeInOutCubic")(Mathf.Clamp01((beat - 135f) / 41f));
            return Mathf.Lerp(420f, 5f, approach) * LaneScale;
        }

        // SimulateGameRingZoom replays the game's zoom pipeline on its 50 Hz fixed-tick grid: every type-9
        // event SetPositions ring[i].destination = i * step at the first fixed frame after its callback
        // (sameTypeIndex is 1-based, so even indices take _maxPositionStep), then each tick runs
        // posZ = Lerp(posZ, positionOffset.z + destination, 0.02 * moveSpeed), and the render state
        // interpolates the last two fixed states exactly like TrackLaneRing.LateUpdateRing.
        private static float[] SimulateGameRingZoom(
            float seedBeat,
            float endBeat,
            float[] seedZ,
            float[] positionOffsets,
            float minStep,
            float maxStep,
            float moveSpeed)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var fixedDeltaTime = TrackLaneRingsRotationEffect.EmulatedFixedDeltaTime;
            var ringCount = seedZ.Length;

            var assignments = BeatSaberSongContainer.Instance.Map.Events
                .Where(evt => evt.Type == 9)
                .OrderBy(evt => evt.JsonTime)
                .Select((evt, index) => (
                    Frame: TrackLaneRingsRotationEffect.GetFirstAssignmentFrame(
                        atsc.GetSecondsFromBeat(evt.JsonTime), fixedDeltaTime),
                    // The game numbers sameTypeIndex from 1, so the second, fourth, ... type-9 events
                    // take _maxPositionStep and the odd ones take _minPositionStep.
                    Step: evt.CustomStep ?? ((index + 1) % 2 == 0 ? maxStep : minStep),
                    Speed: evt.CustomSpeed ?? evt.CustomPreciseSpeed ?? moveSpeed))
                .ToList();

            TrackLaneRingsRotationEffect.GetPreviewRenderState(
                atsc.GetSecondsFromBeat(seedBeat), fixedDeltaTime, out _, out var seedFrame, out _);
            TrackLaneRingsRotationEffect.GetPreviewRenderState(
                atsc.GetSecondsFromBeat(endBeat), fixedDeltaTime, out _, out var endFrame, out var interpolation);

            var positions = (float[])seedZ.Clone();
            var previous = (float[])seedZ.Clone();
            var destinations = new float[ringCount];
            var speeds = new float[ringCount];
            var nextEvent = 0;
            for (var frame = seedFrame + 1; frame <= endFrame; frame++)
            {
                // One render callback can dispatch several events; all assignments land before the next
                // fixed tick, so the last one at this frame wins that tick's destination.
                while (nextEvent < assignments.Count && assignments[nextEvent].Frame <= frame)
                {
                    for (var i = 0; i < ringCount; i++)
                    {
                        destinations[i] = i * assignments[nextEvent].Step;
                        speeds[i] = assignments[nextEvent].Speed;
                    }

                    nextEvent++;
                }

                for (var i = 0; i < ringCount; i++)
                {
                    previous[i] = positions[i];
                    positions[i] = Mathf.Lerp(
                        positions[i],
                        positionOffsets[i] + destinations[i],
                        fixedDeltaTime * speeds[i]);
                }
            }

            var rendered = new float[ringCount];
            for (var i = 0; i < ringCount; i++)
            {
                rendered[i] = previous[i] + ((positions[i] - previous[i]) * interpolation);
            }

            return rendered;
        }

        // Independent reference implementation of Heck's point-definition easing variants
        // (Heck/Animation/PointDefinition/Easings.cs): the Back and Bounce families are sine/parabola
        // variants, NOT the easings.net curves. Every other name falls back to the shared curve so the
        // expected model tracks what Chroma's own easing table evaluates in game.
        private static System.Func<float, float> HeckEasingNamed(string name)
        {
            return name switch
            {
                "easeInBack" => HeckEaseInBack,
                "easeOutBack" => HeckEaseOutBack,
                "easeInOutBack" => HeckEaseInOutBack,
                "easeInBounce" => HeckEaseInBounce,
                "easeOutBounce" => HeckEaseOutBounce,
                "easeInOutBounce" => HeckEaseInOutBounce,
                _ => Easing.Named(name)
            };
        }

        private static float HeckEaseInBack(float p)
        {
            return (p * p * p) - (p * Mathf.Sin(p * Mathf.PI));
        }

        private static float HeckEaseOutBack(float p)
        {
            var f = 1f - p;
            return 1f - ((f * f * f) - (f * Mathf.Sin(f * Mathf.PI)));
        }

        private static float HeckEaseInOutBack(float p)
        {
            if (p < 0.5f)
            {
                var f = 2f * p;
                return 0.5f * ((f * f * f) - (f * Mathf.Sin(f * Mathf.PI)));
            }

            var g = 1f - ((2f * p) - 1f);
            return (0.5f * (1f - ((g * g * g) - (g * Mathf.Sin(g * Mathf.PI))))) + 0.5f;
        }

        private static float HeckEaseInBounce(float p)
        {
            return 1f - HeckEaseOutBounce(1f - p);
        }

        private static float HeckEaseOutBounce(float p)
        {
            const float amplitude = 1f;
            if (p < 0.25f)
            {
                var f = p * 2f;
                return amplitude * (f * f) * 0.25f;
            }
            if (p < 0.5f)
            {
                var f = (p * 2f) - 1f;
                return (amplitude * (f * f) * 0.5f) + 0.25f;
            }
            if (p < 0.75f)
            {
                var f = (p * 2f) - 1.5f;
                return (amplitude * (f * f) * 0.75f) + 0.5f;
            }
            var g = (p * 2f) - 2f;
            return (amplitude * (g * g)) + 0.75f;
        }

        private static float HeckEaseInOutBounce(float p)
        {
            if (p < 0.5f) return HeckEaseInBounce(2f * p) * 0.5f;
            return (HeckEaseOutBounce((2f * p) - 1f) * 0.5f) + 0.5f;
        }

        // The note track's authored _rotation point data (AnimateTrack at beat 266, duration 70), evaluated
        // with Heck's easing table like the game does: the camera rides this track via AssignPlayerToTrack.
        private static readonly Vector3[] NoteRotationPoints =
            { new(0f, 0f, 0f), new(0f, 0f, 115f), new(0f, 0f, 0f) };
        private static readonly float[] NoteRotationTimes = { 0f, 0.75f, 1f };
        private static readonly string[] NoteRotationEasings = { "easeLinear", "easeInOutBack", "easeInOutCubic" };

        private static Quaternion HeckNoteRotation(float beat)
        {
            return EvaluatePointTrackRotation(
                NoteRotationPoints, NoteRotationTimes, NoteRotationEasings, 266f, 70f, beat);
        }

        // WorldCavesIn's kaleidoscope expectations evaluate the authored point tracks exactly like production
        // PointDefinition.Interpolate: point times are normalized over the event duration and the easing named on
        // each point eases the segment ending at it (easings[i] eases values[i-1] -> values[i]). The easing
        // resolution is Heck's, since Chroma evaluates the same table in game.
        private static Vector3 EvaluatePointTrack(Vector3[] values, float[] times, string[] easings, float eventTime, float duration, float beat)
        {
            var t = (beat - eventTime) / duration;
            if (t <= times[0]) return values[0];
            for (var i = 1; i < values.Length; i++)
            {
                if (t > times[i]) continue;
                var eased = HeckEasingNamed(easings[i])((t - times[i - 1]) / (times[i] - times[i - 1]));
                return Vector3.LerpUnclamped(values[i - 1], values[i], eased);
            }

            return values[^1];
        }

        private static Quaternion EvaluatePointTrackRotation(Vector3[] values, float[] times, string[] easings, float eventTime, float duration, float beat)
        {
            var t = (beat - eventTime) / duration;
            if (t <= times[0]) return Quaternion.Euler(values[0]);
            for (var i = 1; i < values.Length; i++)
            {
                if (t > times[i]) continue;
                var eased = HeckEasingNamed(easings[i])((t - times[i - 1]) / (times[i] - times[i - 1]));
                return Quaternion.SlerpUnclamped(Quaternion.Euler(values[i - 1]), Quaternion.Euler(values[i]), eased);
            }

            return Quaternion.Euler(values[^1]);
        }

        // AnimateTrack "PlayersPlaceParent0" (event 266.08, duration 71.75).
        private static readonly Vector3[] PlayersPlace0PositionPoints =
            { new(0f, 0f, 0f), new(-7f, -35.5f, 63f), new(-2f, 350f, 0f), new(696969f, 350f, 0f) };
        private static readonly float[] PlayersPlace0PositionTimes = { 0f, 0.769f, 0.999f, 1f };
        private static readonly string[] PlayersPlace0PositionEasings = { "easeLinear", "easeOutBack", "easeInOutCubic", "easeLinear" };
        private static readonly Vector3[] PlayersPlace0RotationPoints =
            { new(0f, 0f, 0f), new(0f, -21f, -97f), new(0f, 0f, -4f) };
        private static readonly float[] PlayersPlace0RotationTimes = { 0f, 0.75f, 1f };
        private static readonly string[] PlayersPlace0RotationEasings = { "easeLinear", "easeOutBack", "easeInOutQuad" };

        // AnimateTrack "BuildingsParent0" (event 266, duration 69).
        private static readonly Vector3[] Buildings0PositionPoints =
            { new(0f, 0f, 0f), new(7f, -53f, -15f), new(0f, 0f, 350f), new(696969f, 350f, 0f) };
        private static readonly float[] Buildings0PositionTimes = { 0f, 0.769f, 0.999f, 1f };
        private static readonly string[] Buildings0PositionEasings = { "easeLinear", "easeOutBack", "easeInOutCubic", "easeLinear" };
        private static readonly Vector3[] Buildings0RotationPoints =
            { new(0f, 0f, 0f), new(0f, -7f, 69f), new(0f, 0f, 0f) };
        private static readonly float[] Buildings0RotationTimes = { 0f, 0.769f, 1f };
        private static readonly string[] Buildings0RotationEasings = { "easeLinear", "easeOutBack", "easeInOutCubic" };

        // The game's composed ParentObject transform for PlayersPlaceParent0: rotation applied above the
        // V2-scaled translation and the clone's authored local offset.
        private static Vector3 PlayersPlaceParent0World(float beat, Vector3 cloneLocal)
        {
            var position = EvaluatePointTrack(
                PlayersPlace0PositionPoints, PlayersPlace0PositionTimes, PlayersPlace0PositionEasings, 266.08f, 71.75f, beat);
            return PlayersPlaceParent0Rotation(beat) * (position * LaneScale + cloneLocal);
        }

        private static Quaternion PlayersPlaceParent0Rotation(float beat) => EvaluatePointTrackRotation(
            PlayersPlace0RotationPoints, PlayersPlace0RotationTimes, PlayersPlace0RotationEasings, 266.08f, 71.75f, beat);

        // The game's composed ParentObject transform for BuildingsParent0, evaluated at the PlayersPlace peak beat.
        private static Vector3 BuildingsParent0World(float beat, Vector3 cloneLocal)
        {
            var position = EvaluatePointTrack(
                Buildings0PositionPoints, Buildings0PositionTimes, Buildings0PositionEasings, 266f, 69f, beat);
            return EvaluatePointTrackRotation(
                Buildings0RotationPoints, Buildings0RotationTimes, Buildings0RotationEasings, 266f, 69f, beat)
                * (position * LaneScale + cloneLocal);
        }
    }
}
