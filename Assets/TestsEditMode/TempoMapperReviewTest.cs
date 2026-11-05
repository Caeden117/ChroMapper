using System;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Info;
using Beatmap.V2;
using Beatmap.V3;
using Beatmap.V4;
using NUnit.Framework;
using SimpleJSON;
using UnityEditor;
using UnityEngine;

namespace TestsEditMode
{
    public class TempoMapperReviewTest
    {
        private int previousMapVersion;
        private BeatSaberSongContainer previousContainer;
        private GameObject testContainer;

        [SetUp]
        public void SetUp() => previousMapVersion = Settings.Instance.MapVersion;

        [TearDown]
        public void TearDown()
        {
            Settings.Instance.MapVersion = previousMapVersion;
            if (testContainer != null)
            {
                typeof(BeatSaberSongContainer).GetProperty(nameof(BeatSaberSongContainer.Instance)).SetValue(null, previousContainer);
                UnityEngine.Object.DestroyImmediate(testContainer);
            }
        }

        private void SetSongInfo(BaseInfo info)
        {
            previousContainer = BeatSaberSongContainer.Instance;
            testContainer = new GameObject("Tempo Save Test");
            testContainer.SetActive(false);
            var container = testContainer.AddComponent<BeatSaberSongContainer>();
            container.Info = info;
            typeof(BeatSaberSongContainer).GetProperty(nameof(BeatSaberSongContainer.Instance)).SetValue(null, container);
        }

        private static TempoMap Tempo() => new(new[]
        {
            new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 4000, StartBeat = 0, EndBeat = 8 },
            new BpmInfoBpmRegion { StartSampleIndex = 4000, EndSampleIndex = 12000, StartBeat = 8, EndBeat = 40 }
        }, 1000);

        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        public void LegacyMigrationPrecedesEditsForNotesLightsAndObstacleEndpoints(int version, bool editTempo)
        {
            Settings.Instance.MapVersion = version;
            var json = version == 2
                ? @"{""_version"":""2.6.0"",""_notes"":[{""_time"":12,""_lineIndex"":0,""_lineLayer"":0,""_type"":0,""_cutDirection"":1}],""_obstacles"":[{""_time"":6,""_duration"":8,""_lineIndex"":0,""_type"":0,""_width"":1}],""_events"":[{""_time"":12,""_type"":0,""_value"":1}],""_customData"":{""_BPMChanges"":[{""_time"":8,""_BPM"":240}],""keep"":""yes""}}"
                : @"{""version"":""3.3.0"",""colorNotes"":[{""b"":12,""x"":0,""y"":0,""c"":0,""d"":1}],""obstacles"":[{""b"":6,""d"":8,""x"":0,""y"":0,""w"":1,""h"":5}],""basicBeatmapEvents"":[{""b"":12,""et"":0,""i"":1,""f"":1}],""customData"":{""BPMChanges"":[{""b"":8,""m"":240}],""keep"":""yes""}}";
            var tempo = Tempo();
            if (editTempo)
            {
                tempo.SetBpm(0, 150);
                tempo.SetBpm(1, 180);
            }
            var output = TempoMapFileStore.WithTempo(JSON.Parse(json), tempo, 120);
            var migrated = TempoMapFileStore.WithTempo(JSON.Parse(json), Tempo(), 120);
            var migratedThenEdited = TempoMapFileStore.WithTempo(migrated, tempo, 120);
            Assert.That(output.ToString(), Is.EqualTo(migratedThenEdited.ToString()),
                "First-save edits must behave exactly like edits after an unedited migration save.");
            // Go through serialized JSON and the normal difficulty parser, rather than only checking patched values.
            var reloaded = version == 2
                ? V2Difficulty.GetFromJson(JSON.Parse(output.ToString()), "test.dat")
                : V3Difficulty.GetFromJson(JSON.Parse(output.ToString()), "test.dat");
            Assert.That(reloaded, Is.Not.Null);
            reloaded.BootstrapBpmEvents(120);
            Assert.That(reloaded.BpmChanges, Is.Empty);
            Assert.That(reloaded.Notes[0].JsonTime, Is.EqualTo(16));
            Assert.That(reloaded.Events[0].JsonTime, Is.EqualTo(16));
            var wall = reloaded.Obstacles[0];
            Assert.That(wall.JsonTime, Is.EqualTo(6));
            Assert.That(wall.JsonTime + wall.Duration, Is.EqualTo(20));
            if (!editTempo)
            {
                Assert.That(reloaded.JsonTimeToSongBpmTime(reloaded.Notes[0].JsonTime) / 2, Is.EqualTo(6).Within(0.00001));
                Assert.That(reloaded.JsonTimeToSongBpmTime(reloaded.Events[0].JsonTime) / 2, Is.EqualTo(6).Within(0.00001));
                Assert.That(reloaded.JsonTimeToSongBpmTime(wall.JsonTime) / 2, Is.EqualTo(3).Within(0.00001));
                Assert.That(reloaded.JsonTimeToSongBpmTime(wall.JsonTime + wall.Duration) / 2, Is.EqualTo(7).Within(0.00001));
            }
            Assert.That(output[version == 2 ? "_customData" : "customData"]["keep"].Value, Is.EqualTo("yes"));
            var again = TempoMapFileStore.WithTempo(JSON.Parse(output.ToString()), tempo, 120);
            Assert.That(again.ToString(), Is.EqualTo(output.ToString()), "Repeated saves must not convert timestamps again.");
        }

        [Test]
        public void ChangedV3TempoSurvivesReopeningWithoutLegacyGridOverridingIt()
        {
            Settings.Instance.MapVersion = 3;
            var tempo = Tempo();
            tempo.SetBpm(1, 180);
            var node = JSON.Parse(@"{""version"":""3.3.0"",""customData"":{""BPMChanges"":[{""b"":8,""m"":240}]}}");
            var output = TempoMapFileStore.WithTempo(node, tempo, 120);
            var reloaded = V3Difficulty.GetFromJson(JSON.Parse(output.ToString()), "test.dat");
            Assert.That(reloaded.BpmChanges, Is.Empty);
            Assert.That(reloaded.BpmEvents[1].Bpm, Is.EqualTo(180).Within(0.001));
        }

        [Test]
        public void LoadingPrefersLegacyGridOverStaleOfficialEventsAndSidecar()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChroMapper-Tempo-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var info = new BaseInfo { Version = "2.1.0", Directory = directory, BeatsPerMinute = 120 };
                SetSongInfo(info);
                var stale = Tempo();
                stale.SetBpm(1, 180);
                TempoMapFileStore.Save(info, stale, Array.Empty<DifficultySettings>());
                var map = new BaseDifficulty { BpmEvents = stale.ToEvents() };
                map.BpmChanges.Add(new BaseBpmChange { JsonTime = 8, Bpm = 240 });
                var loaded = TempoMapFileStore.Load(info, map, 12000, 1000);
                Assert.That(loaded.Bpm(1), Is.EqualTo(240).Within(0.001));
                Assert.That(loaded.BeatAt(6000), Is.EqualTo(16).Within(0.001));
            }
            finally { System.IO.Directory.Delete(directory, true); }
        }

        [Test]
        public void SavingDoesNotCreatePermanentBackupDirectories()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChroMapper-Tempo-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var info = new BaseInfo { Version = "2.1.0", Directory = directory, BeatsPerMinute = 120 };
                SetSongInfo(info);
                TempoMapFileStore.Save(info, Tempo(), Array.Empty<DifficultySettings>());
                TempoMapFileStore.Save(info, Tempo(), Array.Empty<DifficultySettings>());
                Assert.That(System.IO.Directory.GetDirectories(directory), Is.Empty);
                Assert.That(System.IO.Directory.GetFiles(directory, "*.tempo-tmp"), Is.Empty);
                Assert.That(System.IO.File.Exists(System.IO.Path.Combine(directory, BaseBpmInfo.GetOutputFileName(2, info))), Is.True);
            }
            finally { System.IO.Directory.Delete(directory, true); }
        }

        [Test]
        public void LegacyConversionPreservesSliderHeadAndTailAndDoesNotRewriteUnknownData()
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""sliders"":[{""b"":6,""tb"":14}],""burstSliders"":[{""b"":12,""tb"":16}],""customData"":{""BPMChanges"":[{""b"":8,""m"":240}],""unknown"":{""b"":99}}}");
            TempoMapFileStore.WithTempo(node, Tempo(), 120);
            Assert.That(node["sliders"][0]["b"].AsDouble, Is.EqualTo(6));
            Assert.That(node["sliders"][0]["tb"].AsDouble, Is.EqualTo(20));
            Assert.That(node["burstSliders"][0]["b"].AsDouble, Is.EqualTo(16));
            Assert.That(node["burstSliders"][0]["tb"].AsDouble, Is.EqualTo(24));
            Assert.That(node["customData"]["unknown"]["b"].AsInt, Is.EqualTo(99));
        }

        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        public void SavingLegacyDifficultyToDiskInvalidatesCacheAndDoesNotConvertTwice(int version, bool editTempo)
        {
            Settings.Instance.MapVersion = version;
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChroMapper-Tempo-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var info = new BaseInfo { Version = "2.1.0", Directory = directory, BeatsPerMinute = 120 };
                SetSongInfo(info);
                var path = System.IO.Path.Combine(directory, "Expert.dat");
                System.IO.File.WriteAllText(path, version == 2
                    ? @"{""_version"":""2.6.0"",""_notes"":[{""_time"":12,""_lineIndex"":0,""_lineLayer"":0,""_type"":0,""_cutDirection"":1}],""_customData"":{""_BPMChanges"":[{""_time"":8,""_BPM"":240}]}}"
                    : @"{""version"":""3.3.0"",""colorNotes"":[{""b"":12,""x"":0,""y"":0,""c"":0,""d"":1}],""customData"":{""BPMChanges"":[{""b"":8,""m"":240}]}}");
                var infoDifficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                    { Difficulty = "Expert", BeatmapFileName = "Expert.dat" };
                var settings = new DifficultySettings(infoDifficulty);
                var original = settings.Map;
                Assert.That(original.BpmChanges, Has.Count.EqualTo(1));
                var tempo = TempoMapFileStore.Load(info, original, 12000, 1000);
                if (editTempo) tempo.SetBpm(1, 180);
                TempoMapFileStore.Save(info, tempo, new[] { settings });
                var reloaded = settings.Map;
                Assert.That(reloaded, Is.Not.SameAs(original));
                Assert.That(reloaded.BpmChanges, Is.Empty);
                Assert.That(reloaded.Notes[0].JsonTime, Is.EqualTo(16).Within(0.00001));
                Assert.That(reloaded.Notes[0].SongBpmTime / 2, Is.EqualTo(editTempo ? 20d / 3 : 6).Within(0.00001));
                var firstSave = System.IO.File.ReadAllText(path);
                TempoMapFileStore.Save(info, tempo, new[] { settings });
                Assert.That(System.IO.File.ReadAllText(path), Is.EqualTo(firstSave));
                // After migration, official edits retain migrated stored beats.
                tempo.SetBpm(1, 300);
                TempoMapFileStore.Save(info, tempo, new[] { settings });
                Assert.That(settings.Map.Notes[0].JsonTime, Is.EqualTo(16).Within(0.00001));
                Assert.That(System.IO.Directory.GetDirectories(directory), Is.Empty);
            }
            finally { System.IO.Directory.Delete(directory, true); }
        }

        [TestCase(125)]
        [TestCase(180)]
        [TestCase(100)]
        public void FractionalLegacyV2MigrationPrecedesFirstTempoEdit(int bpm)
        {
            Settings.Instance.MapVersion = 2;
            // Representative fractional grid and note positions from Zero Visibility (10a28).
            var node = JSON.Parse(@"{""_version"":""2.0.0"",""_notes"":[{""_time"":20.647484,""_lineIndex"":1,""_lineLayer"":0,""_type"":1,""_cutDirection"":6},{""_time"":119.215309,""_lineIndex"":0,""_lineLayer"":0,""_type"":0,""_cutDirection"":1}],""_customData"":{""_BPMChanges"":[{""_time"":20,""_BPM"":139},{""_time"":117.122292,""_BPM"":129},{""_time"":119.215309,""_BPM"":121}]}}");
            var original = V2Difficulty.GetFromJson(node.Clone(), "test.dat");
            var originalTempo = TempoMapFileStore.Load(new BaseInfo
                { Version = "2.1.0", Directory = System.IO.Path.GetTempPath(), BeatsPerMinute = 135 },
                original, 120000000, 1000000);
            var migrated = TempoMapFileStore.WithTempo(node.Clone(), originalTempo, 135);
            var tempo = new TempoMap(new[] { new BpmInfoBpmRegion
                { StartSampleIndex = 0, EndSampleIndex = 120000000, StartBeat = 0, EndBeat = bpm * 2 } }, 1000000);
            var edited = TempoMapFileStore.WithTempo(node.Clone(), tempo, 135);
            var editedAfterMigration = TempoMapFileStore.WithTempo(migrated.Clone(), tempo, 135);
            Assert.That(edited.ToString(), Is.EqualTo(editedAfterMigration.ToString()));
            var map = V2Difficulty.GetFromJson(migrated, "test.dat");
            map.BootstrapBpmEvents(135);
            Assert.That(map.JsonTimeToSongBpmTime(map.Notes[0].JsonTime) * 60 / 135,
                Is.EqualTo(20.647484 * 60 / 135).Within(0.00002));
            Assert.That(map.JsonTimeToSongBpmTime(map.Notes[1].JsonTime) * 60 / 135,
                Is.EqualTo(119.215309 * 60 / 135).Within(0.00002));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void OfficialEditsPreserveAllObjectDataAcrossRepeatedSaves(int version)
        {
            var node = JSON.Parse(version == 2
                ? @"{""_version"":""2.6.0"",""_notes"":[{""_time"":12.125}],""_obstacles"":[{""_time"":6,""_duration"":8}],""_events"":[{""_time"":12,""_type"":0,""_value"":1},{""_time"":0,""_type"":100,""_floatValue"":120}],""_customData"":{""_bookmarks"":[{""_time"":16}]}}"
                : @"{""version"":""3.3.0"",""colorNotes"":[{""b"":12.125}],""obstacles"":[{""b"":6,""d"":8}],""basicBeatmapEvents"":[{""b"":12}],""bpmEvents"":[{""b"":0,""m"":120}],""customData"":{""bookmarks"":[{""b"":16}]}}");
            JSONNode ObjectsOnly(JSONNode source)
            {
                var result = source.Clone();
                if (version == 2)
                {
                    var lights = new JSONArray();
                    foreach (JSONNode evt in result["_events"])
                        if (evt["_type"].AsInt != 100) lights.Add(evt);
                    result["_events"] = lights;
                }
                else result.Remove("bpmEvents");
                return result;
            }
            var before = ObjectsOnly(node).ToString();
            var tempo = Tempo();
            foreach (var bpm in new[] { 180, 300, 120 })
            {
                tempo.SetBpm(1, bpm);
                node = JSON.Parse(TempoMapFileStore.WithTempo(node, tempo, 120).ToString());
                Assert.That(ObjectsOnly(node).ToString(), Is.EqualTo(before));
                var events = node[version == 2 ? "_events" : "bpmEvents"];
                JSONNode lastBpm = null;
                foreach (JSONNode evt in events)
                    if (version == 3 || evt["_type"].AsInt == 100) lastBpm = evt;
                Assert.That(lastBpm, Is.Not.Null);
                Assert.That(lastBpm[version == 2 ? "_floatValue" : "m"].AsFloat, Is.EqualTo(bpm));
            }
        }

        [Test]
        public void OfficialV3EditPreservesStoredEndpointsAndRelativeLightingTimes()
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":120},{""b"":8,""m"":240}],""sliders"":[{""b"":6,""tb"":20}],""burstSliders"":[{""b"":16,""tb"":24}],""lightColorEventBoxGroups"":[{""b"":6,""e"":[{""w"":0,""e"":[{""b"":10}]}]}],""customData"":{""bookmarksUseOfficialBpmEvents"":true,""bookmarks"":[{""b"":16}]}}");
            var tempo = Tempo();
            tempo.SetBpm(1, 180);
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node["sliders"][0]["b"].AsDouble, Is.EqualTo(6));
            Assert.That(node["sliders"][0]["tb"].AsDouble, Is.EqualTo(20));
            Assert.That(node["burstSliders"][0]["b"].AsDouble, Is.EqualTo(16));
            Assert.That(node["burstSliders"][0]["tb"].AsDouble, Is.EqualTo(24));
            Assert.That(node["lightColorEventBoxGroups"][0]["e"][0]["e"][0]["b"].AsDouble, Is.EqualTo(10));
            Assert.That(node["customData"]["bookmarks"][0]["b"].AsDouble, Is.EqualTo(16));
        }

        [TestCase(2)]
        [TestCase(3)]
        public void OfficialGridWithoutBpmEventsPreservesStoredBeats(int version)
        {
            var node = JSON.Parse(version == 2
                ? @"{""_version"":""2.0.0"",""_notes"":[{""_time"":20}]}"
                : @"{""version"":""3.3.0"",""colorNotes"":[{""b"":20}]}");
            TempoMapFileStore.WithTempo(node, Tempo(), 120);
            var beat = node[version == 2 ? "_notes" : "colorNotes"][0][version == 2 ? "_time" : "b"].AsDouble;
            Assert.That(beat, Is.EqualTo(20), "Official edits must retain stored object beats.");
        }

        [Test]
        public void LegacyBookmarksAndNestedLightingUseTheConvertedGrid()
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[{""b"":6,""e"":[{""w"":0,""e"":[{""b"":6}]}]}],""customData"":{""BPMChanges"":[{""b"":8,""m"":240}],""bookmarks"":[{""b"":12}]}}");
            TempoMapFileStore.WithTempo(node, Tempo(), 120);
            Assert.That(node["lightColorEventBoxGroups"][0]["e"][0]["e"][0]["b"].AsDouble, Is.EqualTo(10));
            Assert.That(node["customData"]["bookmarks"][0]["b"].AsDouble, Is.EqualTo(16));
            Assert.That(node["customData"]["bookmarksUseOfficialBpmEvents"].AsBool, Is.True);
        }

        [Test]
        public void LegacyDistributedLightingFailsSafelyInsteadOfWritingIncorrectTiming()
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""lightColorEventBoxGroups"":[{""b"":6,""e"":[{""w"":4,""d"":1,""e"":[{""b"":0}]}]}],""customData"":{""BPMChanges"":[{""b"":8,""m"":240}]}}");
            Assert.Throws<System.IO.InvalidDataException>(() => TempoMapFileStore.WithTempo(node, Tempo(), 120));
            Assert.That(node["customData"].HasKey("BPMChanges"), Is.True);
        }

        [Test]
        public void LegacyGridWithoutConversionContextFailsBeforeRemovingData()
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""customData"":{""BPMChanges"":[{""b"":8,""m"":240}]}}");
            var original = node.ToString();
            Assert.Throws<System.IO.InvalidDataException>(() => TempoMapFileStore.WithTempo(node, Tempo()));
            Assert.That(node.ToString(), Is.EqualTo(original));
        }

        [Test]
        public void V4OfficialEditsPreserveNotesEndpointsAndSharedDurations()
        {
            var node = JSON.Parse(@"{""version"":""4.1.0"",""colorNotes"":[{""b"":16,""i"":0}],""colorNotesData"":[{""x"":1,""y"":2,""c"":0,""d"":1}],""arcs"":[{""hb"":6,""tb"":20}],""chains"":[{""hb"":16,""tb"":24}],""obstacles"":[{""b"":6,""i"":0},{""b"":16,""i"":0}],""obstaclesData"":[{""d"":4,""x"":1,""w"":2,""h"":3}],""keep"":""yes""}");
            var tempo = Tempo();
            tempo.SetBpm(1, 180);
            var before = node.ToString();
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
            tempo.SetBpm(1, 300);
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void V4OfficialEditsPreserveSharedLightshowData()
        {
            var node = JSON.Parse(@"{""version"":""4.0.0"",""basicEvents"":[{""b"":16}],""lightColorEventBoxes"":[{""w"":2,""d"":1,""r"":0.5}],""eventBoxGroups"":[{""b"":2,""t"":1,""e"":[{""e"":0,""f"":0,""l"":[{""b"":0,""i"":0}]}]},{""b"":16,""t"":1,""e"":[{""e"":0,""f"":0,""l"":[{""b"":0,""i"":0}]}]}]}");
            var tempo = Tempo();
            tempo.SetBpm(1, 180);
            var before = node.ToString();
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
            tempo.SetBpm(1, 300);
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void OfficialEditsPreserveStoredWaveAndStepLightingDistributions(int distributionType)
        {
            var node = JSON.Parse(@"{""version"":""3.3.0"",""bpmEvents"":[{""b"":0,""m"":120}],""lightColorEventBoxGroups"":[{""b"":10,""e"":[{""w"":4,""e"":[{""b"":2},{""b"":6}]}]}]}");
            node["lightColorEventBoxGroups"][0]["e"][0]["d"] = distributionType;
            var tempo = new TempoMap(new[] { new BpmInfoBpmRegion
                { StartSampleIndex = 0, EndSampleIndex = 12000, StartBeat = 0, EndBeat = 48 } }, 1000);
            TempoMapFileStore.WithTempo(node, tempo, 120);
            var group = node["lightColorEventBoxGroups"][0];
            Assert.That(group["b"].AsDouble, Is.EqualTo(10));
            Assert.That(group["e"][0]["w"].AsDouble, Is.EqualTo(4).Within(0.00001));
            Assert.That(group["e"][0]["e"][0]["b"].AsDouble, Is.EqualTo(2));
            Assert.That(group["e"][0]["e"][1]["b"].AsDouble, Is.EqualTo(6));
        }

        [TestCase("bpm")]
        [TestCase("beats")]
        public void FocusedRegionFieldShowsThePausedRegionsValue(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/Tempo Mapper.prefab");
            var view = UnityEngine.Object.Instantiate(prefab).GetComponent<TempoMapperView>();
            AudioClip clip = null;
            try
            {
                typeof(TempoMapperView).GetProperty(nameof(TempoMapperView.Map)).SetValue(view, Tempo());
                var field = (TextBoxFloatComponent)typeof(TempoMapperView).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                var input = (TMPro.TMP_InputField)field.Selectable;
                var music = (AudioSource)typeof(TempoMapperView).GetField("music", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                clip = AudioClip.Create("Region Focus Test", 12000, 1, 1000, false);
                music.clip = clip;
                music.timeSamples = 6000;
                input.SetTextWithoutNotify("120");
                typeof(TMPro.TMP_InputField).GetField("m_AllowInput", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(input, true);
                typeof(TempoMapperView).GetMethod("BindRegionFields", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(view, null);
                typeof(TextBoxNumberComponent<float>).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(field, null);
                input.onSelect.Invoke("120");
                Assert.That(music.isPlaying, Is.False);
                Assert.That(view.SelectedRegion, Is.EqualTo(1));
                Assert.That(input.text, Is.EqualTo(name == "bpm" ? "240" : "32"));
                Assert.That(input.isFocused, Is.True);
            }
            finally
            {
                foreach (var field in view.GetComponentsInChildren<TextBoxFloatComponent>(true)) field.OnEndEdit(null);
                UnityEngine.Object.DestroyImmediate(view.gameObject);
                if (clip != null) UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void V4DiskSavePreservesDifficultyLightshowAndBookmarkTimestamps()
        {
            Settings.Instance.MapVersion = 4;
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ChroMapper-Tempo-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);
            try
            {
                var info = new BaseInfo { Version = "4.0.1", Directory = directory, BeatsPerMinute = 120, AudioDataFilename = "AudioData.dat" };
                SetSongInfo(info);
                var audio = new BaseBpmInfo { Version = "4.0.0", AudioFrequency = 1000, AudioSamples = 12000, BpmRegions = Tempo().Regions };
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "AudioData.dat"), V4AudioData.GetOutputJson(audio).ToString());
                const string mapJson = @"{""version"":""4.1.0"",""colorNotes"":[{""b"":16,""i"":0}],""colorNotesData"":[{""x"":1,""y"":2,""c"":0,""d"":1}]}";
                var settings = new System.Collections.Generic.List<DifficultySettings>();
                foreach (var name in new[] { "Expert", "Hard" })
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(directory, name + ".dat"), mapJson);
                    var diff = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                        { Difficulty = name, BeatmapFileName = name + ".dat", LightshowFileName = "Lightshow.dat" };
                    settings.Add(new DifficultySettings(diff));
                }
                var lightshowPath = System.IO.Path.Combine(directory, "Lightshow.dat");
                System.IO.File.WriteAllText(lightshowPath, @"{""version"":""4.0.0"",""basicEvents"":[{""b"":16,""i"":0}],""basicEventsData"":[{""t"":0,""i"":1,""f"":1}]}" );
                var bookmarksDir = System.IO.Path.Combine(directory, "Bookmarks");
                System.IO.Directory.CreateDirectory(bookmarksDir);
                var bookmarksPath = System.IO.Path.Combine(bookmarksDir, settings[0].InfoDifficulty.BookmarkFileName);
                System.IO.File.WriteAllText(bookmarksPath, @"{""color"":""#ffffff"",""bookmarks"":[{""beat"":16,""text"":""Keep me""}]}" );
                var unchangedFiles = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var path in new[] { lightshowPath, bookmarksPath,
                             System.IO.Path.Combine(directory, "Expert.dat"), System.IO.Path.Combine(directory, "Hard.dat") })
                    unchangedFiles[path] = System.IO.File.ReadAllText(path);
                var tempo = Tempo();
                tempo.SetBpm(1, 180);
                TempoMapFileStore.Save(info, tempo, settings);
                var lights = JSON.Parse(System.IO.File.ReadAllText(lightshowPath));
                Assert.That(lights["basicEvents"][0]["b"].AsDouble, Is.EqualTo(16));
                var bookmarks = JSON.Parse(System.IO.File.ReadAllText(bookmarksPath));
                Assert.That(bookmarks["bookmarks"][0]["beat"].AsDouble, Is.EqualTo(16));
                Assert.That(bookmarks["bookmarks"][0]["text"].Value, Is.EqualTo("Keep me"));
                foreach (var diff in settings)
                {
                    Assert.That(diff.Map.Notes[0].JsonTime, Is.EqualTo(16));
                    Assert.That(diff.Map.Events[0].JsonTime, Is.EqualTo(16));
                }
                tempo.SetBpm(1, 300);
                TempoMapFileStore.Save(info, tempo, settings);
                foreach (var diff in settings)
                    Assert.That(diff.Map.Notes[0].JsonTime, Is.EqualTo(16));
                foreach (var file in unchangedFiles)
                    Assert.That(System.IO.File.ReadAllText(file.Key), Is.EqualTo(file.Value));
            }
            finally { System.IO.Directory.Delete(directory, true); }
        }

        [Test]
        public void V4OfficialEditsDoNotQuantizeObjectTimestamps()
        {
            var tempo = new TempoMap(new[]
            {
                new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 23415, StartBeat = 0, EndBeat = 1 },
                new BpmInfoBpmRegion { StartSampleIndex = 23415, EndSampleIndex = 480000, StartBeat = 1, EndBeat = 19 }
            }, 48000);
            var node = JSON.Parse(@"{""version"":""4.1.0"",""colorNotes"":[{""b"":12,""i"":0}]}");
            var before = node.ToString();
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
            tempo.SetBpm(1, 300);
            TempoMapFileStore.WithTempo(node, tempo, 120);
            Assert.That(node.ToString(), Is.EqualTo(before));
        }

        [Test]
        public void KnownRegionBeatCalculationMatchesSearchingAtBoundariesAndFractionalSamples()
        {
            var tempo = Tempo();
            foreach (var sample in new[] { 0, 3999, 4000, 4001, 11999, 12000 })
                Assert.That(tempo.BeatAt(tempo.RegionAt(sample), sample), Is.EqualTo(tempo.BeatAt(sample)));
            Assert.That(tempo.BeatAt(1, 4000.5), Is.EqualTo(8.002).Within(0.000001));
        }

        [Test]
        public void PlaybackRegionChangeRefreshesFieldsWithoutOverwritingEditsWithinSameRegion()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/UI/Tempo Mapper.prefab");
            var view = UnityEngine.Object.Instantiate(prefab).GetComponent<TempoMapperView>();
            try
            {
                typeof(TempoMapperView).GetProperty(nameof(TempoMapperView.Map)).SetValue(view, Tempo());
                var refresh = typeof(TempoMapperView).GetMethod("RefreshRegionFields", BindingFlags.NonPublic | BindingFlags.Instance);
                TextBoxFloatComponent Field(string name) => (TextBoxFloatComponent)typeof(TempoMapperView)
                    .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
                refresh.Invoke(view, new object[] { false });
                Assert.That(Field("bpm").Value, Is.EqualTo(120));
                Field("bpm").SetValueWithoutNotify(123);
                refresh.Invoke(view, new object[] { false });
                Assert.That(Field("bpm").Value, Is.EqualTo(123));
                typeof(TempoMapperView).GetProperty(nameof(TempoMapperView.SelectedRegion)).SetValue(view, 1);
                refresh.Invoke(view, new object[] { false });
                Assert.That(Field("bpm").Value, Is.EqualTo(240));
                Assert.That(Field("beats").Value, Is.EqualTo(32));
            }
            finally { UnityEngine.Object.DestroyImmediate(view.gameObject); }
        }
    }
}
