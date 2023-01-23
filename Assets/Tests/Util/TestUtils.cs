using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Util
{
    internal class TestUtils
    {
        // while not important for CI, it's affecting other dev looking into this if they have any of this changed
        private static readonly Dictionary<string, bool> preTestSettings = new Dictionary<string, bool>();

        public static IEnumerator LoadMapper()
        {
            InitSettings();
            
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                yield break;
            }
            
            CMInputCallbackInstaller.TestMode = true;
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            PersistentUI.Instance.EnableTransitions = false;

            // On pipeline this may be run fresh
            if (!Settings.ValidateDirectory(null))
            {
                FirstBootMenu firstBootMenu = Object.FindObjectOfType<FirstBootMenu>();
                Settings.Instance.BeatSaberInstallation = "/root/bs";
                firstBootMenu.HandleGenerateMissingFolders(0);
            }

            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            BeatSaberSongContainer.Instance.Song = new BeatSaberSong("testmap", new JSONObject() { ["_songName"] = "Unit Test"});
            BeatSaberSong.DifficultyBeatmapSet parentSet = new BeatSaberSong.DifficultyBeatmapSet("Lawless");
            BeatSaberSong.DifficultyBeatmap diff = new BeatSaberSong.DifficultyBeatmap(parentSet)
            {
                CustomData = new JSONObject()
            };
            BeatSaberSongContainer.Instance.DifficultyData = diff;
            BeatSaberSongContainer.Instance.LoadedSong = AudioClip.Create("Fake", 44100 * 2, 1, 44100, false);
            BeatSaberSongContainer.Instance.Map = BeatmapFactory.GetDifficultyFromJson(new JSONObject 
            {
                ["version"] = "3.2.0"
            }, "testmap");
            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        private static void InitSettings()
        {
            Settings.Instance.Reminder_Loading360Levels = false; // is this needed to be saved & returned?
            
            if (!preTestSettings.ContainsKey("Load_Notes"))
            {
                preTestSettings.Add("Load_Notes", Settings.Instance.Load_Notes);
                preTestSettings.Add("Load_Events", Settings.Instance.Load_Events);
                preTestSettings.Add("Load_Obstacles", Settings.Instance.Load_Obstacles);
                preTestSettings.Add("Load_Others", Settings.Instance.Load_Others);
                preTestSettings.Add("Load_MapV3", Settings.Instance.Load_MapV3);
            }
            
            Settings.Instance.Load_Notes = true;
            Settings.Instance.Load_Events = true;
            Settings.Instance.Load_Obstacles = true;
            Settings.Instance.Load_Others = true;
            Settings.Instance.Load_MapV3 = true;
        }

        public static void ReturnSettings()
        {
            Settings.Instance.Load_Notes = preTestSettings["Load_Notes"];
            Settings.Instance.Load_Events = preTestSettings["Load_Events"];
            Settings.Instance.Load_Obstacles = preTestSettings["Load_Obstacles"];
            Settings.Instance.Load_Others = preTestSettings["Load_Others"];
            Settings.Instance.Load_MapV3 = preTestSettings["Load_MapV3"];
        }
    }
}
