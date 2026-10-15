using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class LaneCountEditorStateTest : TestBase
    {
        [UnityTest]
        public IEnumerator LaneCountSurvivesSaveAndReloadWithV2Info()
        {
            yield return SaveAndReloadLaneCount(V2Info.Version, 6);
        }

        [UnityTest]
        public IEnumerator LaneCountSurvivesSaveAndReloadWithV4Info()
        {
            yield return SaveAndReloadLaneCount(V4Info.Version, 7);
        }

        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" });
            GetLaneCountInput().Value = 4;
        }

        private static IEnumerator SaveAndReloadLaneCount(string infoVersion, int laneCount)
        {
            var laneCountInput = GetLaneCountInput();
            var textInput = laneCountInput.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(textInput, Is.Not.Null);
            var laneCountText = laneCount.ToString(CultureInfo.InvariantCulture);
            textInput.text = laneCountText;
            textInput.onEndEdit.Invoke(laneCountText);

            var laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            Assert.That(laneController.LaneCount, Is.EqualTo(laneCount), "The top-bar edit must change the note lanes before saving.");

            var songContainer = BeatSaberSongContainer.Instance;
            var info = songContainer.Info;
            var map = songContainer.Map;
            var originalDirectory = info.Directory;
            var originalVersion = info.Version;
            var originalMapPath = map.DirectoryAndFile;
            var saveDirectory = PathUtils.Combine(Path.GetTempPath(), "ChroMapper Lane Count Tests", Guid.NewGuid().ToString("N"));
            JSONObject savedEditorState;
            JSONNode savedMap;

            try
            {
                Directory.CreateDirectory(saveDirectory);
                info.Directory = saveDirectory;
                info.Version = infoVersion;
                map.DirectoryAndFile = PathUtils.Combine(saveDirectory, "ExpertPlus.dat");

                var saveController = Object.FindAnyObjectByType<AutoSaveController>();
                Assert.That(saveController, Is.Not.Null);
                saveController.Save();
                yield return new WaitUntil(() => !saveController.IsSaving);

                var savedInfo = BeatSaberSongUtils.GetInfoFromFolder(saveDirectory);
                Assert.That(savedInfo, Is.Not.Null);
                Assert.That(savedInfo.Version, Is.EqualTo(infoVersion));
                savedEditorState = savedInfo.CustomEditorsData.GetEditorData("editorState").AsObject;
                Assert.That(savedEditorState, Is.Not.Null);
                savedMap = BeatSaberSongUtils.GetNodeFromFile(map.DirectoryAndFile);
                Assert.That(savedMap, Is.Not.Null);
            }
            finally
            {
                info.Directory = originalDirectory;
                info.Version = originalVersion;
                map.DirectoryAndFile = originalMapPath;
                if (Directory.Exists(saveDirectory))
                    Directory.Delete(saveDirectory, true);
            }

            // Recreate the mapper scene so a surviving controller cannot hide a missing saved value.
            yield return TestUtils.ReloadMap(3, savedMap, savedEditorState, forceSceneReload: true);
            yield return null;

            laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            Assert.That(laneController.LaneCount, Is.EqualTo(laneCount), "Reloading the saved map must restore its note lane count.");
            laneCountInput = GetLaneCountInput();
            Assert.That(laneCountInput.Value, Is.EqualTo(laneCount), "The top bar must display the restored lane count.");
            textInput = laneCountInput.GetComponentInChildren<TMP_InputField>(true);
            Assert.That(textInput.text, Is.EqualTo(laneCountText));

            var laneField = typeof(PlacementLaneController).GetField("lane", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(laneField, Is.Not.Null);
            var gridLane = (GridLane)laneField.GetValue(laneController);
            Assert.That(gridLane.Lane, Is.EqualTo(laneCount), "The gameplay grid must use the restored lane count.");
        }

        private static TextBoxIntComponent GetLaneCountInput()
        {
            var view = Object.FindAnyObjectByType<GameplayViewController>(FindObjectsInactive.Include);
            Assert.That(view, Is.Not.Null);
            var inputField = typeof(GameplayViewController).GetField("laneCountInput", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(inputField, Is.Not.Null);
            return (TextBoxIntComponent)inputField.GetValue(view);
        }
    }
}
