using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class CreateNewSongTest
    {
        private string temporarySongRoot;

        // New-song tests need the real song-select controller while isolating all created files under a disposable directory.
        [UnitySetUp]
        public IEnumerator OpenSongSelectWithTemporarySongRoot()
        {
            yield return TestUtils.LoadMap(3);

            SceneTransitionManager.Instance.LoadScene("01_SongSelectMenu");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("01") && !SceneTransitionManager.IsLoading);

            // CreatingNewV2SongWritesInitialInfoBeforeOpeningEditor and its V4 counterpart can inherit dialogs leaked by
            // earlier batch tests. Closing them in UnitySetUp gives the creation assertion a clean modal baseline.
            foreach (var dialog in GetActiveDialogs())
            {
                dialog.Close();
            }

            temporarySongRoot = Path.Combine(
                Path.GetTempPath(),
                "ChroMapper CreateNewSong Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporarySongRoot);

            var songList = Object.FindAnyObjectByType<SongList>();
            Assert.IsNotNull(songList);

            var selectedFolderField = typeof(SongList).GetField(
                "selectedFolder",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(selectedFolderField);

            var songFolderPathsField = typeof(SongList).GetField(
                "songFolderPaths",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(songFolderPathsField);

            var selectedFolder = (int)selectedFolderField.GetValue(null);
            var songFolderPaths = (List<string>)songFolderPathsField.GetValue(songList);
            songFolderPaths[selectedFolder] = temporarySongRoot;
        }

        // Each test owns a unique, resolved temporary root so its generated map cannot leak into later runs.
        [TearDown]
        public void DeleteTemporarySongRoot()
        {
            if (temporarySongRoot != null && Directory.Exists(temporarySongRoot))
            {
                Directory.Delete(temporarySongRoot, true);
            }
        }

        // The V4 creation regression left Info.dat unwritten until a manual save and displayed a warning instead.
        [UnityTest]
        public IEnumerator CreatingNewV4SongWritesInitialInfoBeforeOpeningEditor()
        {
            yield return CreateSongAndAssertInitialInfo("Initial V4 Map", 0, '4');
        }

        // The same creation path supports legacy V2 maps, which must also persist their initial Info.dat automatically.
        [UnityTest]
        public IEnumerator CreatingNewV2SongWritesInitialInfoBeforeOpeningEditor()
        {
            yield return CreateSongAndAssertInitialInfo("Initial V2 Map", 1, '2');
        }

        // Driving the production dialog verifies HandleNewSong performs the save instead of only testing BaseInfo.Save in isolation.
        private IEnumerator CreateSongAndAssertInitialInfo(string songName, int versionIndex, char expectedMajorVersion)
        {
            var createNewSong = Object.FindAnyObjectByType<CreateNewSong>();
            Assert.IsNotNull(createNewSong);

            createNewSong.CreateSong();

            var folderTextBox = GetPrivateField<TextBoxComponent>(createNewSong, "folderTextBoxComponent");
            var versionDropdown = GetPrivateField<DropdownComponent>(createNewSong, "versionDropdownComponent");
            folderTextBox.Value = songName;
            versionDropdown.Value = versionIndex;

            var handleNewSong = typeof(CreateNewSong).GetMethod(
                "HandleNewSong",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(handleNewSong);
            handleNewSong.Invoke(createNewSong, null);

            // The obsolete save warning was an active preset dialog immediately after submission; creation should leave no dialog open.
            var activeDialogs = GetActiveDialogs();
            Assert.AreEqual(0, activeDialogs.Length, "New-map creation unexpectedly opened a dialog.");

            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name.StartsWith("02") && !SceneTransitionManager.IsLoading);

            var infoPath = Path.Combine(temporarySongRoot, songName, "Info.dat");
            Assert.IsTrue(File.Exists(infoPath), $"Expected new map metadata at {infoPath}.");

            var savedInfo = BeatSaberSongUtils.GetInfoFromFolder(Path.GetDirectoryName(infoPath));
            Assert.IsNotNull(savedInfo);
            Assert.AreEqual(expectedMajorVersion, savedInfo.Version[0]);
            Assert.AreEqual(songName, savedInfo.SongName);
        }

        // Both setup isolation and the post-creation regression assertion must use the same active-dialog definition.
        private static DialogBox[] GetActiveDialogs() =>
            Object.FindObjectsByType<DialogBox>(FindObjectsSortMode.None);

        // Reflection is limited to the private dialog fields required to drive the production creation callback.
        private static T GetPrivateField<T>(CreateNewSong createNewSong, string fieldName)
        {
            var field = typeof(CreateNewSong).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);

            var value = field.GetValue(createNewSong);
            Assert.IsNotNull(value);
            return (T)value;
        }
    }
}
