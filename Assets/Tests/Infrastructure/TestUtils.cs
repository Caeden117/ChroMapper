using System;
using System.Collections;
using Beatmap.Base;
using Beatmap.Helper;
using Beatmap.Info;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Infrastructure
{
    internal class TestUtils
    {
        private static bool mapperInit;
        private static int loadVersion = 3;
        private static BaseInfo baselineInfo;
        private static InfoDifficulty baselineDifficulty;
        private static BaseDifficulty baselineMap;
        private static AudioClip baselineSong;
        private static AudioClip ownedTestSong;
        // Preserve project input routing while tests force deterministic delivery without requiring Game view focus.
        private static UnityEngine.InputSystem.InputSettings.BackgroundBehavior? baselineBackgroundBehavior;
        private static UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode? baselineEditorInputBehavior;

        private static IEnumerator InitMapper()
        {
            CMInputCallbackInstaller.TestMode = true;
            Settings.TestMode = true;
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            PersistentUI.Instance.EnableTransitions = false;

            // On pipeline this may be run fresh
            if (Settings.TestMode)
            {
                var firstBootMenu = Object.FindAnyObjectByType<FirstBootMenu>();
                firstBootMenu.HandleGenerateMissingFolders(0);
            }

            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            mapperInit = true;
        }

        public static IEnumerator LoadMap(int version)
        {
            if (version != 2 && version != 3) throw new ArgumentException("Only beatmap version 2 and 3 is available");

            var prevVersion = loadVersion;
            loadVersion = version;

            // check map version, switch if different
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                if (prevVersion == version)
                {
                    // The first fixture can inherit an already loaded mapper scene, so capture its map before later tests can mutate it.
                    CaptureBaseline();
                    yield break;
                }

                SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
                yield return new WaitUntil(() =>
                    SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            }

            Settings.TestRunnerSettings.MapVersion = version;

            yield return LoadMapper();
        }

        // Capture the first standard map once so repeated fixture setup can restore the same metadata and map timing basis.
        private static void CaptureBaseline()
        {
            if (baselineMap != null)
            {
                return;
            }

            var songContainer = BeatSaberSongContainer.Instance;
            baselineInfo = songContainer.Info;
            baselineDifficulty = songContainer.MapDifficultyInfo;
            baselineMap = songContainer.Map;
            baselineSong = songContainer.LoadedSong;
        }

        // Restore the canonical empty test map so direct singleton mutations cannot desynchronize metadata from map timing caches between tests.
        internal static void ResetSharedMapState()
        {
            if (baselineMap == null)
            {
                return;
            }

            var songContainer = BeatSaberSongContainer.Instance;
            songContainer.Info = baselineInfo;
            songContainer.MapDifficultyInfo = baselineDifficulty;
            songContainer.Map = baselineMap;
            songContainer.LoadedSong = baselineSong;
            baselineMap.ValidateBpmEventsAndObjectTimes(baselineInfo.BeatsPerMinute);
        }

        // BasicEventDenseMapChunkingTest reloads a scene-backed empty map after its dense fixture. Adopt that map and
        // metadata together so later SetUp calls cannot swap in an older map behind the new scene's manager collections.
        internal static void CaptureCurrentMapAsSharedBaseline()
        {
            var songContainer = BeatSaberSongContainer.Instance;
            baselineInfo = songContainer.Info;
            baselineDifficulty = songContainer.MapDifficultyInfo;
            baselineMap = songContainer.Map;
            baselineSong = songContainer.LoadedSong;
        }

        // Keep physical shortcut emulation independent of whichever Unity editor window happens to own focus during a bulk run.
        internal static void ResetSharedInputState()
        {
            var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
            baselineBackgroundBehavior ??= inputSettings.backgroundBehavior;
            baselineEditorInputBehavior ??= inputSettings.editorInputBehaviorInPlayMode;
            inputSettings.backgroundBehavior =
                UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            inputSettings.editorInputBehaviorInPlayMode =
                UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

            CMInputCallbackInstaller.ResetTestState();
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (!device.added)
                {
                    continue;
                }

                if (!device.enabled)
                {
                    UnityEngine.InputSystem.InputSystem.EnableDevice(device);
                }

                UnityEngine.InputSystem.InputSystem.ResetDevice(device);
            }

            // EventNextPrevTest's bulk-run double-action regression showed a prior interrupted shortcut can leave the
            // shared SelectionController's modifier latch true even after its keyboard is reset. Clear both production
            // callback latches so one Shift+Arrow cannot execute ShiftSelection and MoveSelection in the next test.
            var selectionControllers = Object.FindObjectsByType<SelectionController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (var selectionController in selectionControllers)
            {
                selectionController.OnActivateShiftinPlace(default);
                selectionController.OnActivateShiftinTime(default);
            }
        }

        // Load a fresh test map after a scene transition so transition tests recreate the map-scoped services used by later fixtures.
        // BasicEventDenseMapChunkingTest needs the production scene-loading path with a scaled BPM that keeps its
        // high-beat fixture inside the shared short test clip; other callers retain the standard metadata by default.
        public static IEnumerator ReloadMap(
            int version,
            JSONNode difficultyJson,
            JSONObject editorState = null,
            float? beatsPerMinute = null,
            string environmentName = null,
            int songLengthSeconds = 60,
            bool forceSceneReload = false,
            InfoDifficulty difficultyInfo = null)
        {
            if (version != 2 && version != 3) throw new ArgumentException("Only beatmap version 2 and 3 is available");

            loadVersion = version;
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                // Match PauseManager's normal non-multiplayer exit path before loading the next selected difficulty.
                SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
                yield return new WaitUntil(() =>
                    SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);
            }

            Settings.TestRunnerSettings.MapVersion = version;
            // BasicEventDenseMapChunkingTest must load the high-beat fixture through the production scene path
            // without allocating a multi-minute audio clip, so permit that fixture to provide an equivalent scaled BPM.
            yield return LoadMapper(difficultyJson, editorState, beatsPerMinute, environmentName, songLengthSeconds, difficultyInfo);
        }

        // Carry the optional fixture BPM into BaseInfo before BeatmapFactory computes every object's SongBpmTime.
        private static IEnumerator LoadMapper(
            JSONNode difficultyJson = null,
            JSONObject editorState = null,
            float? beatsPerMinute = null,
            string environmentName = null,
            int songLengthSeconds = 60,
            InfoDifficulty difficultyInfo = null)
        {
            if (SceneManager.GetActiveScene().name.StartsWith("03")) yield break;

            if (!mapperInit || SceneTransitionManager.Instance == null)
            {
                yield return InitMapper();
            }

            var info = new BaseInfo { Directory = "testmap", SongName = "test", Version = "2.1.0" };
            // BasicEventDenseMapChunkingTest scales the real map's 145 BPM timing to fit its high-beat events in
            // the shared 60-second clip while preserving the same JsonTime-to-SongBpmTime ratios.
            if (beatsPerMinute.HasValue)
            {
                info.BeatsPerMinute = beatsPerMinute.Value;
            }

            // WorldCavesInEnvironmentTest loads environment enhancements authored against TimbalandEnvironment
            // object IDs; without the matching production environment scene, every enhancement lookup matches
            // nothing and the fixture cannot reproduce the reported map behavior.
            if (environmentName != null)
            {
                info.EnvironmentName = environmentName;
            }

            // WorldCavesInEnvironmentTest scrubs to beats beyond the shared 60-second clip (beat 266+ at 124 BPM),
            // and AudioTimeSyncController clamps seeks to LoadedSong.length, so its clip must actually cover them.
            songLengthSeconds = Mathf.Max(60, songLengthSeconds);
            // Inject map-owned editor metadata before scene loading so providers restore it through the same LoadInitialMap path as production maps.
            if (editorState != null)
            {
                info.CustomEditorsData.SetEditorData("editorState", editorState);
            }
            BeatSaberSongContainer.Instance.Info = info;
            var parentSet = new InfoDifficultySet { Characteristic = "Lawless" };
            var diff = difficultyInfo ?? new InfoDifficulty(parentSet) { LightshowFileName = "MissingTestLightshow.dat" };

            BeatSaberSongContainer.Instance.MapDifficultyInfo = diff;
            // Cursor and paste tests must reach anchors beyond beat 33 at the default 100 BPM without AudioTimeSyncController clamping them to the fake clip's end.
            // Only clip.length matters to ATSC and the preview paths, so an 8 kHz clip covers the same song
            // length at ~5x less PCM memory per reload than a 44.1 kHz clip.
            ReplaceTestSong(songLengthSeconds);
            BeatSaberSongContainer.Instance.Map = BeatmapFactory.GetDifficultyFromJson(
                difficultyJson ?? (loadVersion == 3
                    ? new JSONObject { ["version"] = "3.2.0" }
                    : new JSONObject { ["_version"] = "2.6.0" }),
                "testmap",
                info,
                diff);

            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);

            // Every collection's MapObjects aliases the freshly loaded map's own lists, so the shared
            // baseline must track every completed load. If a reload leaves baseline pointing at a superseded
            // map, the next test's ResetSharedMapState installs that dead object: placements then write to the
            // live map's lists while obj.Map lookups search the dead one (BPMTest.SongBpmTimes collapsed to
            // JsonTime after fixture tests reloaded without recapturing).
            CaptureCurrentMapAsSharedBaseline();
        }

        // Native AudioClips need explicit destruction. Rebind the source before releasing only the clip this helper owns.
        private static void ReplaceTestSong(int songLengthSeconds)
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null && atsc.IsPlaying)
            {
                atsc.CancelPlaying();
            }

            var previousSong = ownedTestSong;
            ownedTestSong = AudioClip.Create("Fake", 8000 * songLengthSeconds, 1, 8000, false);
            BeatSaberSongContainer.Instance.LoadedSong = ownedTestSong;
            if (atsc != null)
            {
                atsc.SongAudioSource.clip = ownedTestSong;
            }

            if (previousSong != null)
            {
                Object.DestroyImmediate(previousSong);
            }
        }

        public static void ReturnSettings()
        {
            if (baselineBackgroundBehavior.HasValue)
            {
                // Restore project focus behavior after the fixture no longer needs deterministic synthetic input delivery.
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = baselineBackgroundBehavior.Value;
                baselineBackgroundBehavior = null;
            }

            if (baselineEditorInputBehavior.HasValue)
            {
                // Restore editor routing independently because either setting may have been captured before a fixture abort.
                UnityEngine.InputSystem.InputSystem.settings.editorInputBehaviorInPlayMode =
                    baselineEditorInputBehavior.Value;
                baselineEditorInputBehavior = null;
            }

            Settings.TestMode = false;
        }
    }
}
