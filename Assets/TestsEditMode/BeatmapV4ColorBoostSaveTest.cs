using System.IO;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Info;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    // Regression coverage for the reported "boost color changes are not saving in the v4 map format" bug.
    // V4 stores color boost events in the separate lightshow file, so these tests exercise the complete
    // BaseDifficulty.Save -> BeatSaberSongUtils.GetMapFromInfoFiles production path instead of only the
    // in-memory JSON serializers already covered by BeatmapV4Test.GetLightshowOutputJson.
    public class BeatmapV4ColorBoostSaveTest
    {
        private string testDirectory;
        private GameObject songContainerObject;
        private BeatSaberSongContainer songContainer;
        private object previousSongContainer;
        private PropertyInfo instanceProperty;
        private int previousMapVersion;
        private bool previousFormatJson;
        private bool previousSaveWithoutDefaultValues;

        [SetUp]
        public void Setup()
        {
            previousMapVersion = Settings.Instance.MapVersion;
            previousFormatJson = Settings.Instance.FormatJson;
            previousSaveWithoutDefaultValues = Settings.Instance.SaveWithoutDefaultValues;

            Settings.Instance.MapVersion = 4;
            Settings.Instance.FormatJson = false;
            Settings.Instance.SaveWithoutDefaultValues = false;

            testDirectory = PathUtils.Combine(
                Application.temporaryCachePath,
                nameof(BeatmapV4ColorBoostSaveTest));
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Settings.Instance.MapVersion = previousMapVersion;
            Settings.Instance.FormatJson = previousFormatJson;
            Settings.Instance.SaveWithoutDefaultValues = previousSaveWithoutDefaultValues;

            if (instanceProperty != null) instanceProperty.SetValue(null, previousSongContainer);
            if (songContainerObject != null) Object.DestroyImmediate(songContainerObject);
            if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        }

        // V4 must persist authored boost changes into colorBoostEvents/colorBoostEventsData inside the
        // lightshow file, not the difficulty file.
        [Test]
        public void SaveWritesColorBoostEventsToLightshowFile()
        {
            var map = CreateMapWithBoostEvents();
            InstallSongContainer(map, out var info, out var infoDifficulty);

            Assert.IsTrue(map.Save());

            var lightshowPath = PathUtils.Combine(testDirectory, infoDifficulty.LightshowFileName);
            Assert.IsTrue(File.Exists(lightshowPath), $"Lightshow file was not written to {lightshowPath}");

            var lightshowJson = JSONNode.Parse(File.ReadAllText(lightshowPath));
            var colorBoostEvents = lightshowJson["colorBoostEvents"].AsArray;
            Assert.IsNotNull(colorBoostEvents);
            Assert.AreEqual(3, colorBoostEvents.Count);

            var colorBoostEventsData = lightshowJson["colorBoostEventsData"].AsArray;
            Assert.IsNotNull(colorBoostEventsData);

            // Both boost states must be resolvable through the common-data table.
            var savedBoostValues = new System.Collections.Generic.List<int>();
            foreach (JSONNode colorBoostEvent in colorBoostEvents)
            {
                savedBoostValues.Add(colorBoostEventsData[colorBoostEvent["i"].AsInt]["b"].AsInt);
            }
            Assert.AreEqual(new[] { 1, 0, 1 }, savedBoostValues.ToArray());

            // Boost events must not leak into basicEvents, and the difficulty file itself must not carry them.
            foreach (JSONNode basicEvent in lightshowJson["basicEvents"].AsArray)
            {
                var type = lightshowJson["basicEventsData"][basicEvent["i"].AsInt]["t"].AsInt;
                Assert.AreNotEqual((int)EventTypeValue.ColorBoostEventType, type);
            }

            var difficultyJson = JSONNode.Parse(File.ReadAllText(map.DirectoryAndFile));
            Assert.IsFalse(difficultyJson.HasKey("colorBoostEvents"));
            Assert.IsFalse(difficultyJson.HasKey("colorBoostEventsData"));
            Assert.IsFalse(difficultyJson.HasKey("basicEvents"));
        }

        // Saving and then loading through the production info-file path must restore the authored boost values.
        [Test]
        public void SavedColorBoostEventsSurviveReload()
        {
            var map = CreateMapWithBoostEvents();
            InstallSongContainer(map, out var info, out var infoDifficulty);

            Assert.IsTrue(map.Save());

            var reloaded = BeatSaberSongUtils.GetMapFromInfoFiles(info, infoDifficulty);
            Assert.IsNotNull(reloaded);

            var boostEvents = reloaded.Events.FindAll(x => x.IsColorBoostEvent());
            Assert.AreEqual(3, boostEvents.Count);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[0], 4f, 5, 1, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[1], 8f, 5, 0, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[2], 12f, 5, 1, 0, null);
        }

        // The reported failure is a *change* not persisting, so toggle an existing boost off, save again,
        // and confirm the second save is what lands on disk and reloads.
        [Test]
        public void ToggledColorBoostValuePersistsAcrossSaves()
        {
            var map = CreateMapWithBoostEvents();
            InstallSongContainer(map, out var info, out var infoDifficulty);

            Assert.IsTrue(map.Save());

            var boostEvent = map.Events.Find(x => x.IsColorBoostEvent() && x.JsonTime == 4f);
            Assert.IsNotNull(boostEvent);
            boostEvent.Value = 0;

            Assert.IsTrue(map.Save());

            var reloaded = BeatSaberSongUtils.GetMapFromInfoFiles(info, infoDifficulty);
            Assert.IsNotNull(reloaded);

            var boostEvents = reloaded.Events.FindAll(x => x.IsColorBoostEvent());
            Assert.AreEqual(3, boostEvents.Count);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[0], 4f, 5, 0, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[1], 8f, 5, 0, 0, null);
            BeatmapAssert.EventPropertiesAreEqual(boostEvents[2], 12f, 5, 1, 0, null);
        }

        private static BaseDifficulty CreateMapWithBoostEvents()
        {
            var map = new BaseDifficulty
            {
                Version = V4Difficulty.BeatmapVersion,
                EventTypesWithKeywords = new BaseEventTypesWithKeywords()
            };
            map.Events.Add(new BaseEvent
            {
                JsonTime = 2f,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.BlueOn,
                FloatValue = 1f
            });
            map.Events.Add(new BaseEvent
            {
                JsonTime = 4f,
                Type = (int)EventTypeValue.ColorBoostEventType,
                Value = 1,
                FloatValue = 0f
            });
            map.Events.Add(new BaseEvent
            {
                JsonTime = 8f,
                Type = (int)EventTypeValue.ColorBoostEventType,
                Value = 0,
                FloatValue = 0f
            });
            map.Events.Add(new BaseEvent
            {
                JsonTime = 12f,
                Type = (int)EventTypeValue.ColorBoostEventType,
                Value = 1,
                FloatValue = 0f
            });
            return map;
        }

        // Save() reads BeatSaberSongContainer.Instance for the info directory and lightshow filename,
        // so install a minimal container the same way BeatmapVersionSwitchingTest does.
        private void InstallSongContainer(
            BaseDifficulty map,
            out BaseInfo info,
            out InfoDifficulty infoDifficulty)
        {
            var difficultySet = new InfoDifficultySet { Characteristic = "Standard" };
            infoDifficulty = new InfoDifficulty(difficultySet)
            {
                Difficulty = "ExpertPlus",
                BeatmapFileName = "ExpertPlusStandard.dat",
                LightshowFileName = "Lightshow.dat"
            };
            info = new BaseInfo
            {
                Version = "4.0.1",
                Directory = testDirectory
            };
            info.DifficultySets.Add(difficultySet);
            difficultySet.Difficulties.Add(infoDifficulty);

            map.DirectoryAndFile = PathUtils.Combine(testDirectory, infoDifficulty.BeatmapFileName);

            songContainerObject = new GameObject("BeatSaberSongContainer test");
            songContainer = songContainerObject.AddComponent<BeatSaberSongContainer>();
            songContainer.Info = info;
            songContainer.Map = map;
            songContainer.MapDifficultyInfo = infoDifficulty;

            instanceProperty = typeof(BeatSaberSongContainer).GetProperty(
                nameof(BeatSaberSongContainer.Instance),
                BindingFlags.Static | BindingFlags.Public);
            Assert.IsNotNull(instanceProperty);
            previousSongContainer = instanceProperty.GetValue(null);
            instanceProperty.SetValue(null, songContainer);
        }
    }
}
