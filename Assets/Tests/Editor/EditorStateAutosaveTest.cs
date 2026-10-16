using System;
using System.Collections;
using System.IO;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class EditorStateAutosaveTest : TestBase
    {
        [UnityTest]
        public IEnumerator AutosaveBacksUpReferencedStateBesideInfoWithoutChangingOriginalFile()
        {
            var songContainer = BeatSaberSongContainer.Instance;
            var originalInfo = songContainer.Info;
            var map = songContainer.Map;
            var originalMapPath = map.DirectoryAndFile;
            var difficulty = songContainer.MapDifficultyInfo;
            var originalBeatmapFileName = difficulty.BeatmapFileName;
            var laneController = Object.FindAnyObjectByType<PlacementLaneController>();
            var originalLaneCount = laneController.LaneCount;
            var mapDirectory = PathUtils.Combine(
                Path.GetTempPath(),
                "ChroMapper Autosave State Tests",
                Guid.NewGuid().ToString("N"));
            const string stateFileName = "PersonalState.dat";

            try
            {
                Directory.CreateDirectory(mapDirectory);
                var statePath = PathUtils.Combine(mapDirectory, stateFileName);
                var originalState = new JSONObject
                {
                    ["components"] = new JSONObject
                    {
                        ["gameplayPlacement"] = new JSONObject { ["laneCount"] = 6 }
                    }
                }.ToString();
                File.WriteAllText(statePath, originalState);
                var infoJson = V2Info.GetOutputJson(originalInfo);
                infoJson["_customData"]["_editors"][Application.productName] = new JSONObject { ["_state"] = stateFileName };
                File.WriteAllText(PathUtils.Combine(mapDirectory, "Info.dat"), infoJson.ToString());
                songContainer.Info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
                map.DirectoryAndFile = PathUtils.Combine(mapDirectory, "ExpertPlus.dat");
                difficulty.BeatmapFileName = "ExpertPlus.dat";
                laneController.LaneCount = 9;

                var saveController = Object.FindAnyObjectByType<AutoSaveController>();
                saveController.Save(true);
                yield return new WaitUntil(() => !saveController.IsSaving);

                var backups = Directory.GetDirectories(PathUtils.Combine(mapDirectory, "autosaves"));
                Assert.That(backups, Has.Length.EqualTo(1));
                var backupDirectory = backups[0];
                var backupStatePath = PathUtils.Combine(backupDirectory, stateFileName);
                Assert.That(File.Exists(backupStatePath), Is.True, "Every autosave must contain its own referenced editor-state file.");
                var backupInfo = BeatSaberSongUtils.GetInfoFromFolder(backupDirectory);
                var savedState = backupInfo.CustomEditorsData.GetEditorData("editorState");
                Assert.That(savedState["components"]["gameplayPlacement"]["laneCount"].AsInt, Is.EqualTo(9));
                Assert.That(
                    BeatSaberSongUtils.GetNodeFromFile(backupStatePath).ToString(),
                    Is.EqualTo(savedState.ToString()),
                    "The state file must contain the editorState body directly.");
                var backupJson = BeatSaberSongUtils.GetNodeFromFile(PathUtils.Combine(backupDirectory, "Info.dat"));
                Assert.That(backupJson["_customData"]["_editors"][Application.productName]["_state"].Value, Is.EqualTo(stateFileName));
                Assert.That(File.ReadAllText(statePath), Is.EqualTo(originalState), "Autosaving must leave the main state file alone.");
                Assert.That(songContainer.Info.Directory, Is.EqualTo(mapDirectory));
            }
            finally
            {
                songContainer.Info = originalInfo;
                map.DirectoryAndFile = originalMapPath;
                difficulty.BeatmapFileName = originalBeatmapFileName;
                laneController.LaneCount = originalLaneCount;
                if (Directory.Exists(mapDirectory))
                    Directory.Delete(mapDirectory, true);
            }
        }
    }
}
