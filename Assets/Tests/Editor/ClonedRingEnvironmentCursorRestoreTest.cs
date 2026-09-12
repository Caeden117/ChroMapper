using System.Collections;
using System.Linq;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    // The source map restores its saved cursor after cloning a managed ring but before OnLevelLoaded reinitializes
    // movement effects; this fixture executes those calls synchronously so the expected failure cannot wedge scene loading.
    public class ClonedRingEnvironmentCursorRestoreTest : TestBase
    {
        // RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot reproduces Black Out's load-time
        // ApplyVisual IndexOutOfRangeException without leaving SceneTransitionManager stuck behind the thrown callback.
        [UnityTest]
        public IEnumerator RestoringEditorCursorAfterCloningRingDoesNotUsePreCloneRotationSnapshot()
        {
            var song = BeatSaberSongContainer.Instance;
            var map = BeatmapFactory.GetDifficultyFromJson(
                CreateDifficultyWithClonedRing(),
                "testmap",
                song.Info,
                song.MapDifficultyInfo);
            song.Map = map;

            var manager = Object.FindObjectsByType<TrackLaneRingsManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Single(candidate => candidate.Rings.Any(ring => ring.name == "BigTrackLaneRing(Clone)"));
            var originalRingCount = manager.Rings.Count;

            // HardRefresh is the exact production boundary which appends enhancement clones after the environment's
            // movement effects have initialized, but before EditorStateService restores the saved cursor.
            var loader = Object.FindAnyObjectByType<MapLoader>();
            loader.UpdateMapData(map);
            loader.HardRefreshBeforeEditorStateRestore(
                Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor);

            Assert.That(
                manager.Rings,
                Has.Count.GreaterThan(originalRingCount),
                "The fixture did not append cloned BigTrackLaneRing objects to their initialized manager.");

            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Assert.DoesNotThrow(
                () => atsc.LoadEditorState(CreateSavedCursor()["components"]["currentJsonTime"]),
                "Restoring the source map's cursor rendered a pre-clone ring-rotation snapshot.");
            yield break;
        }

        // The cloned-ring fixture loads a different environment and map; restore the canonical shared map once so
        // subsequent editor tests do not inherit Panic's descriptor or the temporary enhancement collection.
        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        // DefaultEnvironment exposes the same externally managed TrackLaneRing relationship as Panic; cloning all
        // matching BigTrackLaneRing roots reproduces the stale snapshot shape without loading the hanging scene path.
        private static JSONNode CreateDifficultyWithClonedRing()
        {
            var environment = new JSONArray();
            environment.Add(new JSONObject
            {
                ["id"] = @"BigTrackLaneRing\(Clone\)$",
                ["lookupMethod"] = "Regex",
                ["duplicate"] = 1
            });

            return new JSONObject
            {
                ["version"] = "3.3.0",
                ["bpmEvents"] = JSON.Parse("[{\"b\":0,\"m\":150}]"),
                ["basicBeatmapEvents"] = new JSONArray(),
                ["customData"] = new JSONObject { ["environment"] = environment }
            };
        }

        // Black Out's legacy Info.dat stores 11.934 without a map-local grid denominator; the production loader
        // retains the session grid fallback and renders its nearest interval before LightshowController receives OnLevelLoaded.
        private static JSONObject CreateSavedCursor() => new()
        {
            ["components"] = new JSONObject
            {
                ["currentJsonTime"] = new JSONObject { ["value"] = 11.934f }
            }
        };
    }
}
