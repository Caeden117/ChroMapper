using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class InfoFileReferenceTest
    {
        private string testDirectory;

        [SetUp]
        public void SetUp()
        {
            testDirectory = PathUtils.Combine(Path.GetTempPath(), "ChroMapper-InfoFileReference-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(testDirectory, true);
        }

        private static readonly string[] invalidFileNames =
        {
            "../outside.dat", "..\\outside.dat", "Nested/file.dat", "Nested\\file.dat", "C:outside.dat"
        };

        [TestCaseSource(nameof(InvalidReferences))]
        public void InfoRejectsPaths(string version, string reference, string fileName)
        {
            var json = CreateInfoJson(version);
            GetReference(json, reference).Value = fileName;
            if (IsDifficultyReference(reference))
            {
                var info = ParseInfo(version, json);
                Assert.That(info.DifficultySets.SelectMany(set => set.Difficulties), Is.Empty);
                return;
            }

            Assert.Throws<InvalidDataException>(() => ParseInfo(version, json));
        }

        [TestCaseSource(nameof(InvalidDifficultyReferences))]
        public void FolderLoadSkipsOnlyInvalidDifficulty(string version, string reference, string fileName)
        {
            var json = CreateInfoJson(version);
            var difficulties = version == V2Info.Version
                ? json["_difficultyBeatmapSets"][0]["_difficultyBeatmaps"].AsArray
                : json["difficultyBeatmaps"].AsArray;
            difficulties.Add(difficulties[0].Clone());
            GetReference(json, reference).Value = fileName;
            File.WriteAllText(PathUtils.Combine(testDirectory, "Info.dat"), json.ToString());

            var info = BeatSaberSongUtils.GetInfoFromFolder(testDirectory);

            Assert.That(info, Is.Not.Null, "One invalid difficulty must not reject the whole song.");
            var loadedDifficulties = info.DifficultySets.SelectMany(set => set.Difficulties).ToList();
            Assert.That(loadedDifficulties, Has.Count.EqualTo(1));
            Assert.That(loadedDifficulties[0].BeatmapFileName, Is.EqualTo("Expert.dat"));
            Assert.That(loadedDifficulties[0].LightshowFileName, Is.EqualTo(version == V2Info.Version ? null : "Lightshow.dat"));
        }

        [TestCaseSource(nameof(InvalidSongReferences))]
        public void FolderLoadSkipsInvalidSongAndStillLoadsNextSong(string version, string reference, string fileName)
        {
            var json = CreateInfoJson(version);
            GetReference(json, reference).Value = fileName;
            var infoPath = PathUtils.Combine(testDirectory, "Info.dat");
            File.WriteAllText(infoPath, json.ToString());

            Assert.That(BeatSaberSongUtils.GetInfoFromFolder(testDirectory), Is.Null);

            var nextSongDirectory = PathUtils.Combine(testDirectory, "NextSong");
            Directory.CreateDirectory(nextSongDirectory);
            File.WriteAllText(PathUtils.Combine(nextSongDirectory, "Info.dat"), CreateInfoJson(version).ToString());
            Assert.That(BeatSaberSongUtils.GetInfoFromFolder(nextSongDirectory), Is.Not.Null);
        }

        [TestCaseSource(nameof(ValidReferences))]
        public void InfoRetainsPlainFileNames(string version, string reference)
        {
            const string fileName = "Custom 日本語 File.dat";
            var json = CreateInfoJson(version);
            GetReference(json, reference).Value = fileName;
            var info = ParseInfo(version, json);
            var saved = version == V2Info.Version
                ? V2Info.GetOutputJson(info)
                : V4Info.GetOutputJson(info);
            Assert.That(GetReference(saved, reference).Value, Is.EqualTo(fileName));
        }

        [TestCase("../outside.dat")]
        [TestCase("..\\outside.dat")]
        [TestCase("C:outside.dat")]
        public void RuntimeEditsAlsoRejectPaths(string fileName)
        {
            var info = new BaseInfo();
            var set = new InfoDifficultySet();
            var difficulty = new InfoDifficulty(set);
            var contributor = new BaseContributor();
            var assignments = new Action<string>[]
            {
                value => info.SongFilename = value,
                value => info.SongPreviewFilename = value,
                value => info.AudioDataFilename = value,
                value => info.CoverImageFilename = value,
                value => difficulty.BeatmapFileName = value,
                value => difficulty.LightshowFileName = value,
                value => contributor.LocalImageLocation = value,
                value => set.CustomCharacteristicIconImageFileName = value,
                value => set.Characteristic = value,
                value => difficulty.Difficulty = value
            };
            foreach (var assign in assignments)
                Assert.Throws<InvalidDataException>(() => assign(fileName));
        }

        [TestCase(null)]
        [TestCase("")]
        public void OptionalReferencesCanRemainEmpty(string fileName)
        {
            var info = new BaseInfo { SongPreviewFilename = fileName };
            var set = new InfoDifficultySet { CustomCharacteristicIconImageFileName = fileName };
            var difficulty = new InfoDifficulty(set) { LightshowFileName = fileName };
            var contributor = new BaseContributor { LocalImageLocation = fileName };
            Assert.That(info.SongPreviewFilename, Is.EqualTo(fileName));
            Assert.That(difficulty.LightshowFileName, Is.EqualTo(fileName));
            Assert.That(set.CustomCharacteristicIconImageFileName, Is.EqualTo(fileName));
            Assert.That(contributor.LocalImageLocation, Is.EqualTo(fileName));
        }

        private static IEnumerable<TestCaseData> InvalidReferences()
        {
            foreach (var version in new[] { V2Info.Version, V4Info.Version })
            {
                foreach (var reference in References(version))
                {
                    foreach (var fileName in invalidFileNames)
                        yield return new TestCaseData(version, reference, fileName);
                }
            }
        }

        private static IEnumerable<TestCaseData> ValidReferences()
        {
            foreach (var version in new[] { V2Info.Version, V4Info.Version })
            {
                foreach (var reference in References(version))
                    yield return new TestCaseData(version, reference);
            }
        }

        private static IEnumerable<TestCaseData> InvalidDifficultyReferences() => InvalidReferences()
            .Where(testCase => IsDifficultyReference((string)testCase.Arguments[1]));

        private static IEnumerable<TestCaseData> InvalidSongReferences() => InvalidReferences()
            .Where(testCase => !IsDifficultyReference((string)testCase.Arguments[1]));

        private static bool IsDifficultyReference(string reference) =>
            reference.Contains("._difficultyBeatmaps.") || reference.StartsWith("difficultyBeatmaps.", StringComparison.Ordinal) &&
            !reference.EndsWith(".characteristic", StringComparison.Ordinal);

        private static string[] References(string version) => version == V2Info.Version
            ? new[]
            {
                "_songFilename", "_coverImageFilename",
                "_difficultyBeatmapSets.0._difficultyBeatmaps.0._beatmapFilename",
                "_difficultyBeatmapSets.0._difficultyBeatmaps.0._difficulty",
                "_difficultyBeatmapSets.0._beatmapCharacteristicName",
                "_difficultyBeatmapSets.0._customData._characteristicIconImageFilename",
                "_customData._contributors.0._iconPath"
            }
            : new[]
            {
                "audio.songFilename", "audio.audioDataFilename", "songPreviewFilename", "coverImageFilename",
                "difficultyBeatmaps.0.beatmapDataFilename", "difficultyBeatmaps.0.lightshowDataFilename",
                "difficultyBeatmaps.0.difficulty", "difficultyBeatmaps.0.characteristic",
                "customData.characteristicData.0.iconPath", "customData.contributors.0.iconPath"
            };

        private static JSONNode CreateInfoJson(string version)
        {
            var info = new BaseInfo { Version = version };
            var set = new InfoDifficultySet
            {
                Characteristic = "Standard", CustomCharacteristicIconImageFileName = "characteristic.png"
            };
            set.Difficulties.Add(new InfoDifficulty(set)
            {
                Difficulty = "Expert", BeatmapFileName = "Expert.dat", LightshowFileName = "Lightshow.dat"
            });
            info.DifficultySets.Add(set);
            info.CustomContributors.Add(new BaseContributor("contributor.png", "Mapper", "Mapper"));
            return version == V2Info.Version
                ? V2Info.GetOutputJson(info)
                : V4Info.GetOutputJson(info);
        }

        private static BaseInfo ParseInfo(string version, JSONNode json) => version == V2Info.Version
            ? V2Info.GetFromJson(json)
            : V4Info.GetFromJson(json);

        private static JSONNode GetReference(JSONNode node, string reference)
        {
            foreach (var segment in reference.Split('.'))
                node = int.TryParse(segment, out var index)
                    ? node[index]
                    : node[segment];

            return node;
        }
    }
}
