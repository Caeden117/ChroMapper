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

namespace Tests
{
    public class TempoMapperPersistenceTest
    {
        private BaseInfo previousInfo;
        private InfoDifficulty previousDifficulty;
        private BaseDifficulty previousMap;
        private AudioClip previousSong;
        private bool previousTransitions;
        private bool captured;

        private void Capture()
        {
            var song = BeatSaberSongContainer.Instance;
            previousInfo = song.Info;
            previousDifficulty = song.MapDifficultyInfo;
            previousMap = song.Map;
            previousSong = song.LoadedSong;
            previousTransitions = PersistentUI.Instance.EnableTransitions;
            PersistentUI.Instance.EnableTransitions = false;
            captured = true;
        }

        private static TempoMap Timing() => new(new[]
        {
            new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 240000, StartBeat = 0, EndBeat = 7.5f },
            new BpmInfoBpmRegion { StartSampleIndex = 240000, EndSampleIndex = 480000, StartBeat = 7.5f, EndBeat = 20 }
        }, 48000);

        private static BaseInfo IsolatedInfo(int version)
        {
            var path = PathUtils.Combine(Application.temporaryCachePath, "cm-tempo-persistence-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            File.WriteAllText(PathUtils.Combine(path, "Info.dat"), "{}");
            return new BaseInfo
            {
                Version = version == 4 ? "4.0.1" : "2.1.0", Directory = path,
                BeatsPerMinute = 120, SongName = "Tempo persistence",
                EnvironmentNames = new List<string> { "DefaultEnvironment" }
            };
        }

        [UnityTest]
        public IEnumerator TempoSavePreservesExistingAudioMetadataAndBacksItUp()
        {
            yield return TestUtils.LoadMap(2);
            Capture();
            foreach (var version in new[] { 2, 4 })
            {
                var info = IsolatedInfo(version);
                BeatSaberSongContainer.Instance.Info = info;
                var path = PathUtils.Combine(info.Directory, BaseBpmInfo.GetOutputFileName(version, info));
                var original = JSON.Parse("{\"songChecksum\":\"keep-checksum\",\"lufsData\":[{\"si\":0,\"ei\":480000,\"l\":-12.5}],\"futureMetadata\":{\"nested\":[1,2,3]}}");
                File.WriteAllText(path, original.ToString(2));
                TempoMapFileStore.Save(info, Timing(), Array.Empty<DifficultySettings>());
                var output = JSON.Parse(File.ReadAllText(path));
                Assert.That(output["songChecksum"].Value, Is.EqualTo("keep-checksum"));
                Assert.That(output["lufsData"].ToString(), Is.EqualTo(original["lufsData"].ToString()));
                Assert.That(output["futureMetadata"].ToString(), Is.EqualTo(original["futureMetadata"].ToString()));
                var roundTrip = TempoMapFileStore.Load(info, null, 480000, 48000);
                Assert.That(roundTrip.Regions.Count, Is.EqualTo(2));
                Assert.That(roundTrip.Bpm(0), Is.EqualTo(90).Within(0.001));
                Assert.That(roundTrip.Bpm(1), Is.EqualTo(150).Within(0.001));
                var backup = Directory.GetFiles(PathUtils.Combine(info.Directory, ".tempo-backups"), Path.GetFileName(path), SearchOption.AllDirectories).Single();
                Assert.That(File.ReadAllText(backup), Is.EqualTo(original.ToString(2)));
                TempoMapFileStore.Save(info, Timing(), Array.Empty<DifficultySettings>());
                Assert.That(JSON.Parse(File.ReadAllText(path))["futureMetadata"].ToString(), Is.EqualTo(original["futureMetadata"].ToString()));
            }
        }

        [UnityTest]
        public IEnumerator NewDifficultyInheritsSavedTempoAndUntimedSongsKeepTheirDefaults()
        {
            yield return TestUtils.LoadMap(2);
            Capture();
            foreach (var scenario in new[] { (version: 2, timing: true), (version: 4, timing: true), (version: 2, timing: false) })
            {
                var info = IsolatedInfo(scenario.version);
                var song = BeatSaberSongContainer.Instance;
                song.Info = info;
                song.MapDifficultyInfo = null;
                if (scenario.timing) TempoMapFileStore.Save(info, Timing(), Array.Empty<DifficultySettings>());
                Assert.That(info.Save(), Is.True);
                SceneTransitionManager.Instance.LoadScene("02_SongEditMenu");
                yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "02_SongEditMenu" && !SceneTransitionManager.IsLoading);
                for (var frame = 0; frame < 20; frame++) yield return null;
                var selector = UnityEngine.Object.FindAnyObjectByType<DifficultySelect>();
                selector.SetCharacteristic("Standard");
                var row = new DifficultyRow(selector.transform.Cast<Transform>().Single(child => child.name == "ExpertPlus"));
                row.Toggle.isOn = true;
                row.Save.onClick.Invoke();
                var difficulty = info.DifficultySets.Single(set => set.Characteristic == "Standard").Difficulties.Single();
                var map = BeatSaberSongUtils.GetMapFromInfoFiles(info, difficulty);
                Assert.That(map, Is.Not.Null);
                if (scenario.timing)
                {
                    Assert.That(map.BpmEvents.Count, Is.EqualTo(2));
                    Assert.That(map.BpmEvents[0].Bpm, Is.EqualTo(90).Within(0.001));
                    Assert.That(map.BpmEvents[1].JsonTime, Is.EqualTo(7.5).Within(0.001));
                    Assert.That(map.BpmEvents[1].Bpm, Is.EqualTo(150).Within(0.001));
                }
                else Assert.That(map.BpmEvents, Is.Empty, "Creating an untimed difficulty must retain the existing empty-grid behavior.");
            }
        }

        [UnityTearDown]
        public IEnumerator RestoreSong()
        {
            if (!captured) yield break;
            var song = BeatSaberSongContainer.Instance;
            song.Info = previousInfo;
            song.MapDifficultyInfo = previousDifficulty;
            song.Map = previousMap;
            song.LoadedSong = previousSong;
            if (SceneManager.GetActiveScene().name != "03_Mapper") yield return TestUtils.LoadMap(2);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            PersistentUI.Instance.EnableTransitions = previousTransitions;
            captured = false;
        }
    }
}
