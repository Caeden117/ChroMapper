using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using UnityEngine;

namespace TestsEditMode
{
    public class BeatmapVersionSwitchingTest
    {
        private const string fileJson = @"
{
    ""version"": ""3.3.0"",
    ""colorNotes"": [
        {
            ""b"": 10,
            ""x"": 1,
            ""y"": 0,
            ""c"": 0,
            ""d"": 1,
            ""a"": 0,
            ""customData"": {
                ""coordinates"": [5,6],
                ""somePropertyThatCMShouldNotTouch"" : ""HelloWorld!""
            }
        }
    ],
    ""customData"": {
        ""foo"": ""bar"",
        ""time"": ""123.456""
    }
}";


        // For use in PlayMode
        public void TestEverything()
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void RemoveEmptyCustomDataFromOutputTest()
        {
            Settings.Instance.MapVersion = 3;
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            difficulty.Notes[0].CustomData.Remove("coordinates");
            difficulty.Notes[0].CustomData.Remove("somePropertyThatCMShouldNotTouch");

            Assert.AreEqual(0, difficulty.Notes[0].CustomData.Children.Count());

            difficulty.CustomData.Remove("foo");
            difficulty.CustomData.Remove("time");
            difficulty.Time = 0;
            Assert.AreEqual(0, difficulty.CustomData.Children.Count());

            var outputJson = V3Difficulty.GetOutputJson(difficulty);
            Assert.IsFalse(outputJson["colorNotes"].HasKey("customData"));
            Assert.IsFalse(outputJson.HasKey("customData"));
        }
        

        [Test]
        public void NoteV3ToV2ToV3CustomDataTest()
        {
            Settings.Instance.MapVersion = 3;
            var difficulty = V3Difficulty.GetFromJson(JSONNode.Parse(fileJson), "");

            Assert.AreEqual("3.3.0", difficulty.Version);
            Assert.AreEqual(1, difficulty.Notes.Count);
            
            // Making sure the CustomData is loaded as expected
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["coordinates"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["coordinates"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.CustomData["time"].AsFloat, 0.001);
            
            difficulty.ConvertCustomDataVersion(fromVersion: 3, toVersion: 2);
            
            // Check CustomData
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["_position"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["_position"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.CustomData["_time"].AsFloat, 0.001);
            Assert.IsFalse(difficulty.CustomData.HasKey("time"));
            
            difficulty.ConvertCustomDataVersion(fromVersion: 2, toVersion: 3);
            
            // Back to original
            Assert.IsTrue(difficulty.Notes[0].CustomData != null);
            Assert.AreEqual(2, difficulty.Notes[0].CustomData.Children.Count());
            Assert.AreEqual("HelloWorld!", difficulty.Notes[0].CustomData["somePropertyThatCMShouldNotTouch"].Value);
            Assert.AreEqual(5, difficulty.Notes[0].CustomData["coordinates"][0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomData["coordinates"][1].AsInt);
            Assert.IsTrue(difficulty.Notes[0].CustomCoordinate is JSONArray);
            Assert.AreEqual(5, difficulty.Notes[0].CustomCoordinate[0].AsInt);
            Assert.AreEqual(6, difficulty.Notes[0].CustomCoordinate[1].AsInt);
            
            Assert.AreEqual("bar", difficulty.CustomData["foo"].Value);
            Assert.AreEqual(123.456f, difficulty.CustomData["time"].AsFloat, 0.001);
            Assert.IsFalse(difficulty.CustomData.HasKey("_time"));
        }
    }
}