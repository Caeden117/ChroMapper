using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Tests.Util
{
    internal class TestUtils
    {
        // while not important for CI, it's affecting other dev looking into this if they have any of this changed
        private static readonly Dictionary<string, bool> preTestSettings = new Dictionary<string, bool>();

        private static bool mapperInit;
        private static int loadVersion = 3;

        private static IEnumerator InitMapper()
        {
            CMInputCallbackInstaller.TestMode = true;
            Settings.TestMode = true;
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            PersistentUI.Instance.EnableTransitions = false;

            // On pipeline this may be run fresh
            if (!Settings.ValidateDirectory())
            {
                var firstBootMenu = Object.FindObjectOfType<FirstBootMenu>();
                Settings.Instance.BeatSaberInstallation = "/root/bs";
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
                if (prevVersion == version) yield break;

                SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
                yield return new WaitUntil(() =>
                    SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            }

            yield return LoadMapper();
        }

        private static IEnumerator LoadMapper()
        {
            if (SceneManager.GetActiveScene().name.StartsWith("03")) yield break;

            if (!mapperInit) yield return InitMapper();

            BeatSaberSongContainer.Instance.Song =
                new BeatSaberSong("testmap", new JSONObject { ["_songName"] = "Test" });
            var parentSet = new BeatSaberSong.DifficultyBeatmapSet("Lawless");
            var diff = new BeatSaberSong.DifficultyBeatmap(parentSet)
            {
                CustomData = new JSONObject()
            };
            BeatSaberSongContainer.Instance.DifficultyData = diff;
            BeatSaberSongContainer.Instance.LoadedSong = AudioClip.Create("Fake", 44100 * 20, 1, 44100, false);
            BeatSaberSongContainer.Instance.Map = BeatmapFactory.GetDifficultyFromJson(loadVersion == 3
                ? new JSONObject { ["version"] = "3.2.0" }
                : new JSONObject { ["_version"] = "2.6.0" }, "testmap");
            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        public static void ReturnSettings() => Settings.TestMode = false;
    }
}