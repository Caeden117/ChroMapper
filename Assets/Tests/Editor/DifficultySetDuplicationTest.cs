using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class DifficultySetDuplicationTest
    {
        private BaseInfo previousInfo;
        private InfoDifficulty previousDifficulty;
        private BaseDifficulty previousMap;
        private AudioClip previousSong;
        private bool previousTransitions;
        private bool capturedTransitions;

        // SongInfoSaveDoesNotDuplicateStandard follows the reported UI-only route: metadata is saved
        // before a staged ExpertPlus, then the song menu is reopened and Expert and ExpertPlus are saved.
        // The V2 file must contain one Standard set and one ExpertPlus despite the intermediate empty set.
        [UnityTest]
        public IEnumerator SongInfoSaveDoesNotDuplicateStandard()
        {
            yield return TestUtils.LoadMap(2);
            var song = BeatSaberSongContainer.Instance;
            previousInfo = song.Info;
            previousDifficulty = song.MapDifficultyInfo;
            previousMap = song.Map;
            previousSong = song.LoadedSong;
            previousTransitions = PersistentUI.Instance.EnableTransitions;
            capturedTransitions = true;
            PersistentUI.Instance.EnableTransitions = false;

            var directory = PathUtils.Combine(Application.temporaryCachePath,
                $"cm-difficulty-set-repro-{Guid.NewGuid():N}");
            var info = new BaseInfo
            {
                Version = "2.1.0",
                SongName = "DupeRepro",
                Directory = directory,
                EnvironmentNames = new List<string> { "DefaultEnvironment" }
            };
            song.Info = info;
            song.MapDifficultyInfo = null;
            Assert.IsTrue(info.Save(), "The isolated V2 map must be writable before opening song info.");

            yield return OpenSongMenu();
            var difficultySelect = UnityEngine.Object.FindAnyObjectByType<DifficultySelect>();
            var songInfoUi = UnityEngine.Object.FindAnyObjectByType<SongInfoEditUI>();
            Assert.IsNotNull(difficultySelect);
            Assert.IsNotNull(songInfoUi);
            difficultySelect.SetCharacteristic("Standard");
            var expertPlus = FindRow(difficultySelect, "ExpertPlus");
            expertPlus.Toggle.isOn = true;
            songInfoUi.SaveToSong();
            // Metadata saving must not create phantom sets for the seven untouched characteristics.
            Assert.That(info.DifficultySets.Count, Is.EqualTo(1));
            difficultySelect.SaveAllDiffs();

            // Reloading the menu without re-reading Info.dat matches the mapper's return-to-song-info
            // path: the singleton retains both an empty metadata set and the newly saved set.
            yield return OpenSongMenu();
            difficultySelect = UnityEngine.Object.FindAnyObjectByType<DifficultySelect>();
            Assert.IsNotNull(difficultySelect);
            difficultySelect.SetCharacteristic("Standard");
            var expert = FindRow(difficultySelect, "Expert");
            expert.Toggle.isOn = true;
            expert.Save.onClick.Invoke();
            expertPlus = FindRow(difficultySelect, "ExpertPlus");
            expertPlus.Button.onClick.Invoke();
            difficultySelect.Characteristics["Standard"]["ExpertPlus"].NoteJumpStartBeatOffset = 0.5625f;
            expertPlus.Save.onClick.Invoke();

            var output = JSON.Parse(File.ReadAllText(PathUtils.Combine(directory, "Info.dat")));
            var sets = output["_difficultyBeatmapSets"].AsArray.Children
                .Where(set => set["_beatmapCharacteristicName"].Value == "Standard").ToList();
            Assert.That(sets.Count, Is.EqualTo(1),
                "Saving ExpertPlus after returning to song info must not write two Standard sets.");
            Assert.That(sets[0]["_difficultyBeatmaps"].AsArray.Children
                    .Count(diff => diff["_difficulty"].Value == "ExpertPlus"),
                Is.EqualTo(1), "ExpertPlusStandard.dat must be declared only once.");
        }

        // The production scene owns DifficultySelect and its row callbacks; reloading it creates a
        // fresh selection controller while deliberately preserving the persistent song-info object.
        private static IEnumerator OpenSongMenu()
        {
            SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);
            for (var frame = 0; frame < 20; frame++)
                yield return null;
        }

        private static DifficultyRow FindRow(DifficultySelect controller, string name)
        {
            var row = controller.transform.Cast<Transform>().SingleOrDefault(child => child.name == name);
            Assert.IsNotNull(row, $"The song-info scene has no {name} row.");
            return new DifficultyRow(row);
        }

        // Scene transitions and assertions may interrupt the reproduction, so always restore the
        // shared mapper and song singleton before the next Unity fixture starts.
        [UnityTearDown]
        public IEnumerator RestoreMapper()
        {
            var song = BeatSaberSongContainer.Instance;
            if (song != null && previousInfo != null)
            {
                song.Info = previousInfo;
                song.MapDifficultyInfo = previousDifficulty;
                song.Map = previousMap;
                song.LoadedSong = previousSong;
            }
            if (!SceneManager.GetActiveScene().name.StartsWith("03"))
                yield return TestUtils.LoadMap(2);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            if (capturedTransitions)
                PersistentUI.Instance.EnableTransitions = previousTransitions;
        }

        [OneTimeTearDown]
        public void ReturnSettings() => TestUtils.ReturnSettings();
    }
}
