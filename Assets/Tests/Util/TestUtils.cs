using SimpleJSON;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Util
{
    internal class TestUtils
    {

        public static IEnumerator LoadMapper()
        {
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
            {
                yield break;
            }

            Settings.Instance.Reminder_Loading360Levels = false;

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
            BeatSaberSongContainer.Instance.Song = new BeatSaberSong("testmap", new JSONObject());
            BeatSaberSong.DifficultyBeatmapSet parentSet = new BeatSaberSong.DifficultyBeatmapSet("Lawless");
            BeatSaberSong.DifficultyBeatmap diff = new BeatSaberSong.DifficultyBeatmap(parentSet)
            {
                CustomData = new JSONObject()
            };
            BeatSaberSongContainer.Instance.DifficultyData = diff;
            BeatSaberSongContainer.Instance.LoadedSong = AudioClip.Create("Fake", 44100 * 2, 1, 44100, false);
            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        public static void CleanupNotes()
        {
            CleanupType(BeatmapObject.ObjectType.Note);
        }

        public static void CleanupEvents()
        {
            CleanupType(BeatmapObject.ObjectType.Event);
        }

        public static void CleanupObstacles()
        {
            CleanupType(BeatmapObject.ObjectType.Obstacle);
        }

        public static void CleanupBPMChanges()
        {
            CleanupType(BeatmapObject.ObjectType.BpmChange);
        }

        private static void CleanupType(BeatmapObject.ObjectType type)
        {
            BeatmapObjectContainerCollection eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(type);

            foreach (BeatmapObject evt in eventsContainer.LoadedObjects.ToArray())
            {
                eventsContainer.DeleteObject(evt);
            }
        }
    }
}
