using System;
using System.IO;
using System.IO.Compression;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace Tests.Editor
{
    public class EditorStateFileTest
    {
        private string mapDirectory;

        [SetUp]
        public void CreateMapDirectory()
        {
            mapDirectory = PathUtils.Combine(
                Path.GetTempPath(),
                "ChroMapper Editor State Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(mapDirectory);
        }

        [TearDown]
        public void DeleteMapDirectory()
        {
            Directory.Delete(mapDirectory, true);
        }

        [TestCase(V2Info.Version)]
        [TestCase(V4Info.Version)]
        public void SavingUsesDefaultStateFileAndOnlyLeavesReferenceInInfo(string version)
        {
            var info = CreateInfo(version);
            var state = CreateState();
            info.CustomEditorsData.SetEditorData("editorState", state);

            Assert.That(info.Save(), Is.True);
            Assert.That(File.Exists(PathUtils.Combine(mapDirectory, "CmState.dat")), Is.True, "Editor state must be saved beside Info.dat.");
            var descriptor = ReadDescriptor(version);
            Assert.That(descriptor.Count, Is.EqualTo(2));
            Assert.That(descriptor["_version"].Value, Is.EqualTo(Application.version));
            Assert.That(descriptor["_state"].Value, Is.EqualTo("CmState.dat"));
            Assert.That(ReadStateFile("CmState.dat").ToString(), Is.EqualTo(state.ToString()));
        }

        [TestCase(V2Info.Version)]
        [TestCase(V4Info.Version)]
        public void LegacyStateAndPalettesSurviveMigrationAndReload(string version)
        {
            var state = CreateState();
            WriteInfo(version, new JSONObject
            {
                ["version"] = "old-version",
                ["editorState"] = state,
                ["unknownEditorSetting"] = "keep me"
            });

            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
            Assert.That(info, Is.Not.Null);
            Assert.That(info.CustomEditorsData.GetEditorData("editorState").ToString(), Is.EqualTo(state.ToString()));
            Assert.That(info.Save(), Is.True);
            Assert.That(File.Exists(PathUtils.Combine(mapDirectory, "CmState.dat")), Is.True, "Legacy metadata must move to the default state file on save.");
            Assert.That(ReadDescriptor(version).HasKey("editorState"), Is.False);
            Assert.That(ReadStateFile("CmState.dat").ToString(), Is.EqualTo(state.ToString()));

            var reloaded = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
            Assert.That(reloaded.CustomEditorsData.GetEditorData("editorState").ToString(), Is.EqualTo(state.ToString()));
            Assert.That(reloaded.CustomEditorsData.GetEditorData("unknownEditorSetting").Value, Is.EqualTo("keep me"));
            var editors = ReadEditors(version);
            Assert.That(editors["AnotherEditor"]["setting"].AsInt, Is.EqualTo(42));
        }

        [TestCase("PersonalState.dat")]
        [TestCase("My Editor State.json")]
        public void ReferencedFileIsAuthoritativeAndKeepsItsName(string fileName)
        {
            var state = CreateState();
            WriteInfo(V2Info.Version, new JSONObject
            {
                ["_version"] = "old-version",
                ["_state"] = fileName,
                ["editorState"] = new JSONObject { ["obsolete"] = true }
            });
            var statePath = PathUtils.Combine(mapDirectory, fileName);
            File.WriteAllText(statePath, state.ToString());

            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
            Assert.That(info.CustomEditorsData.GetEditorData("editorState").ToString(), Is.EqualTo(state.ToString()), "The referenced file must take precedence over stale embedded data.");
            Assert.That(info.Save(), Is.True);
            Assert.That(ReadDescriptor(V2Info.Version)["_state"].Value, Is.EqualTo(fileName));
            Assert.That(File.Exists(PathUtils.Combine(mapDirectory, "CmState.dat")), Is.False);
            Assert.That(ReadStateFile(fileName).ToString(), Is.EqualTo(state.ToString()));
        }

        [TestCase("CmState.dat")]
        [TestCase("PersonalState.dat")]
        public void MapZipContainsReferencedStateFile(string fileName)
        {
            WriteInfo(V2Info.Version, new JSONObject { ["_state"] = fileName });
            var statePath = PathUtils.Combine(mapDirectory, fileName);
            var stateJson = CreateState().ToString();
            File.WriteAllText(statePath, stateJson);
            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);

            Assert.That(new MapExporter(info).PackageZip(), Is.True);
            using var archive = ZipFile.OpenRead(PathUtils.Combine(mapDirectory, info.CleanSongName + ".zip"));
            var stateEntry = archive.GetEntry(fileName);
            Assert.That(stateEntry, Is.Not.Null, "A shared map must include its referenced editor state.");
            using var reader = new StreamReader(stateEntry.Open());
            Assert.That(reader.ReadToEnd(), Is.EqualTo(stateJson));
        }

        [Test]
        public void CaptureRetainsStateForControlsThatHaveNotRegistered()
        {
            var info = CreateInfo(V2Info.Version);
            var state = CreateState();
            state["components"]["unregisteredPicker"] = state["components"]["chromaPicker"];
            state["components"].Remove("chromaPicker");
            state["components"]["editorStateFileTest"] = new JSONObject { ["value"] = 7, ["futureSetting"] = true };
            info.CustomEditorsData.SetEditorData("editorState", state.Clone());
            var provider = new TestStateProvider();
            EditorStateService.Register(provider);
            try
            {
                EditorStateService.CaptureMapData(info);
                var savedState = info.CustomEditorsData.GetEditorData("editorState");
                Assert.That(savedState["components"]["unregisteredPicker"].ToString(), Is.EqualTo(state["components"]["unregisteredPicker"].ToString()), "Saving before opening the picker must retain its saved palettes.");
                Assert.That(savedState["components"][provider.StateKey]["value"].AsInt, Is.EqualTo(23));
                Assert.That(savedState["components"][provider.StateKey]["futureSetting"].AsBool, Is.True);
                Assert.That(savedState["futureState"].Value, Is.EqualTo("keep me"));
            }
            finally
            {
                EditorStateService.Unregister(provider);
            }
        }

        [Test]
        public void FailedStateWriteDoesNotRemoveLegacyDataFromInfo()
        {
            WriteInfo(V2Info.Version, new JSONObject { ["editorState"] = CreateState() });
            var originalInfo = File.ReadAllText(PathUtils.Combine(mapDirectory, "Info.dat"));
            Directory.CreateDirectory(PathUtils.Combine(mapDirectory, "CmState.dat"));
            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);

            Assert.Catch(() => info.Save(), "The failed state write must stop the metadata save.");
            Assert.That(File.ReadAllText(PathUtils.Combine(mapDirectory, "Info.dat")), Is.EqualTo(originalInfo));
        }

        [Test]
        public void InvalidStateIsReplacedWithAnEmptyObjectOnSave()
        {
            WriteInfo(V2Info.Version, new JSONObject { ["_state"] = "CmState.dat" });
            var statePath = PathUtils.Combine(mapDirectory, "CmState.dat");
            const string invalidState = "[]";
            File.WriteAllText(statePath, invalidState);
            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
            Assert.That(info, Is.Not.Null);

            Assert.That(info.CustomEditorsData.GetEditorData("editorState").IsObject, Is.True);
            Assert.That(info.Save(), Is.True);
            Assert.That(ReadStateFile("CmState.dat").ToString(), Is.EqualTo("{}"));
        }

        [TestCase("Editor/PersonalState.dat")]
        [TestCase("Editor\\PersonalState.dat")]
        [TestCase("./CmState.dat")]
        [TestCase(".\\CmState.dat")]
        [TestCase("../Outside.dat")]
        [TestCase("..\\Outside.dat")]
        [TestCase("/Outside.dat")]
        [TestCase("\\Outside.dat")]
        [TestCase("C:Outside.dat")]
        [TestCase("C:\\Outside.dat")]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("Info.dat")]
        [TestCase("INFO.DAT")]
        [TestCase("./INFO.DAT")]
        [TestCase("Info.dat ")]
        [TestCase("Info.dat.")]
        [TestCase("State?.dat")]
        [TestCase("State\n.dat")]
        [TestCase("NUL.dat")]
        public void InvalidStateFileNameThrowsWhenMetadataIsParsed(string fileName)
        {
            WriteInfo(V2Info.Version, new JSONObject { ["_state"] = fileName });
            var originalInfo = File.ReadAllText(PathUtils.Combine(mapDirectory, "Info.dat"));
            Assert.Throws<InvalidDataException>(() => new BaseInfo.CustomEditorsMetadata(ReadEditors(V2Info.Version)));
            Assert.That(File.ReadAllText(PathUtils.Combine(mapDirectory, "Info.dat")), Is.EqualTo(originalInfo));
        }

        [Test]
        public void MovingAnUnopenedMapRetainsStateFromItsOriginalDirectory()
        {
            WriteInfo(V2Info.Version, new JSONObject { ["_state"] = "CmState.dat" });
            var state = CreateState();
            File.WriteAllText(
                PathUtils.Combine(mapDirectory, "CmState.dat"),
                state.ToString());
            var info = BeatSaberSongUtils.GetInfoFromFolder(mapDirectory);
            var destination = PathUtils.Combine(mapDirectory, "moved");
            info.Directory = destination;
            Assert.That(info.Save(), Is.True);

            var reloaded = BeatSaberSongUtils.GetInfoFromFolder(destination);
            Assert.That(reloaded.CustomEditorsData.GetEditorData("editorState").ToString(), Is.EqualTo(state.ToString()));
        }

        private BaseInfo CreateInfo(string version) => new BaseInfo { Directory = mapDirectory, Version = version };

        private void WriteInfo(string version, JSONObject metadata)
        {
            var info = CreateInfo(version);
            var output = version[0] == '2'
                ? V2Info.GetOutputJson(info)
                : V4Info.GetOutputJson(info);
            var customDataKey = version[0] == '2'
                ? "_customData"
                : "customData";
            var editorsKey = version[0] == '2'
                ? "_editors"
                : "editors";
            output[customDataKey][editorsKey] = new JSONObject
            {
                [Application.productName] = metadata,
                ["AnotherEditor"] = new JSONObject { ["setting"] = 42 }
            };
            File.WriteAllText(PathUtils.Combine(mapDirectory, "Info.dat"), output.ToString());
        }

        private JSONNode ReadInfo() => BeatSaberSongUtils.GetNodeFromFile(PathUtils.Combine(mapDirectory, "Info.dat"));

        private JSONNode ReadEditors(string version)
        {
            var info = ReadInfo();
            return version[0] == '2'
                ? info["_customData"]["_editors"]
                : info["customData"]["editors"];
        }

        private JSONNode ReadDescriptor(string version) => ReadEditors(version)[Application.productName];

        private JSONNode ReadStateFile(string fileName) => BeatSaberSongUtils.GetNodeFromFile(PathUtils.Combine(mapDirectory, fileName));

        private static JSONObject CreateState() => new JSONObject
        {
            ["components"] = new JSONObject
            {
                ["gameplayPlacement"] = new JSONObject { ["laneCount"] = 6 },
                ["chromaPicker"] = JSON.Parse(@"{""presets"":{""My palette"":[{""r"":0.2,""g"":0.4,""b"":0.8,""a"":1}]},""selectedColor"":{""r"":0.2,""g"":0.4,""b"":0.8,""a"":1}}")
            },
            ["futureState"] = "keep me"
        };

        private sealed class TestStateProvider : IEditorStateProvider
        {
            public string StateKey => "editorStateFileTest";

            public void CaptureEditorState(JSONObject data) => data["value"] = 23;

            public void LoadEditorState(JSONNode data)
            {
            }
        }
    }
}
