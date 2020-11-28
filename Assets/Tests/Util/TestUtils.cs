using SimpleJSON;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests.Util
{
    class TestUtils
    {

        public static IEnumerator LoadMapper()
        {
            if (SceneManager.GetActiveScene().name.StartsWith("03"))
                yield break;

            CMInputCallbackInstaller.TestMode = true;
            yield return SceneManager.LoadSceneAsync("00_FirstBoot", LoadSceneMode.Single);
            PersistentUI.Instance.enableTransitions = false;

            // On pipeline this may be run fresh
            if (!Settings.ValidateDirectory(null))
            {
                var firstBootMenu = Object.FindObjectOfType<FirstBootMenu>();
                Settings.Instance.BeatSaberInstallation = "/root/bs";
                firstBootMenu.HandleGenerateMissingFolders(0);
            }

            yield return new WaitUntil(() => SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);
            BeatSaberSongContainer.Instance.song = new BeatSaberSong("testmap", new JSONObject());
            var parentSet = new BeatSaberSong.DifficultyBeatmapSet();
            var diff = new BeatSaberSong.DifficultyBeatmap(parentSet);
            diff.customData = new JSONObject();
            BeatSaberSongContainer.Instance.difficultyData = diff;
            BeatSaberSongContainer.Instance.loadedSong = AudioClip.Create("Fake", 44100 * 2, 1, 44100, false);
            SceneTransitionManager.Instance.LoadScene("03_Mapper");
            yield return new WaitUntil(() => !SceneTransitionManager.IsLoading);
        }

        public static void CleanupNotes()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.NOTE);

            foreach (var note in notesContainer.LoadedObjects.ToArray())
                notesContainer.DeleteObject(note);
        }

        public static void CleanupEvents()
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.EVENT);

            foreach (var evt in eventsContainer.LoadedObjects.ToArray())
                eventsContainer.DeleteObject(evt);
        }
    }
}
