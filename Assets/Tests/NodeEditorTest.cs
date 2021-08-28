﻿using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Tests.Util;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    internal class NodeEditorTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMapper();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            NodeEditorController.IsActive = true;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            NodeEditorController.IsActive = false;
        }

        [Test]
        public void JsonMerge()
        {
            BeatmapObjectContainerCollection eventContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            NodeEditorController nodeEditor = Object.FindObjectOfType<NodeEditorController>();
            TMP_InputField inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            MapEvent eventA = new MapEvent(2, MapEvent.EventTypeBackLasers, MapEvent.LightValueOff, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1]}"));
            eventContainer.SpawnObject(eventA);

            MapEvent eventB = new MapEvent(2, MapEvent.EventTypeLeftLasers, MapEvent.LightValueOff, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2]}"));
            eventContainer.SpawnObject(eventB);

            MapEvent eventC = new MapEvent(2, MapEvent.EventTypeRightLasers, MapEvent.LightValueOff);
            eventContainer.SpawnObject(eventC);

            SelectionController.Select(eventC);
            Assert.AreEqual("{\n  \"_time\" : 2,\n  \"_type\" : 3,\n  \"_value\" : 0\n}", inputField.text);

            SelectionController.Select(eventA);
            Assert.AreEqual("{\n  \"_time\" : 2,\n  \"_type\" : 0,\n  \"_value\" : 0,\n  \"_customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"lenDiffer\" : [\n      1\n    ]\n  }\n}", inputField.text);

            SelectionController.Select(eventB, true);
            Assert.AreEqual("{\n  \"_time\" : 2,\n  \"_type\" : -,\n  \"_value\" : 0,\n  \"_customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : -,\n      \"s\" : -,\n      \"b\" : -,\n      \"a\" : [\n        -,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n    }\n  }\n}", inputField.text);
        }

        [Test]
        public void JsonApply()
        {
            BeatmapObjectContainerCollection eventContainer = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Event);
            NodeEditorController nodeEditor = Object.FindObjectOfType<NodeEditorController>();
            TMP_InputField inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            MapEvent eventA = new MapEvent(2, MapEvent.EventTypeBackLasers, MapEvent.LightValueOff, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1],""updatedLenDiffer"":[1],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}"));
            eventContainer.SpawnObject(eventA);

            MapEvent eventB = new MapEvent(2, MapEvent.EventTypeLeftLasers, MapEvent.LightValueOff, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2],""updatedLenDiffer"":[1,2],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""updatedTypeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1}}"));
            eventContainer.SpawnObject(eventB);

            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);

            nodeEditor.NodeEditor_EndEdit(@"{""_time"": -, ""_type"": -, ""_value"": -, ""_customData"": {""matches"":{},""differs"":{},""typeDiffer"":{},""updatedLenDiffer"":[1],""updated"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedDiffer"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}}");

            Assert.AreEqual("{\n  \"_time\" : 2,\n  \"_type\" : -,\n  \"_value\" : 0,\n  \"_customData\" : {\n    \"matches\" : {\n    },\n    \"differs\" : {\n    },\n    \"typeDiffer\" : {\n    },\n    \"updatedLenDiffer\" : [\n      1\n    ],\n    \"updated\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedDiffer\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedTypeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    }\n  }\n}", inputField.text);

            // Objects have been recreated, pick them up from the selection controller
            BeatmapObject[] events = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual("{\"_time\":2,\"_type\":0,\"_value\":0,\"_customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]},\"lenDiffer\":[1],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}", events[0].ConvertToJson().ToString());
            Assert.AreEqual("{\"_time\":2,\"_type\":2,\"_value\":0,\"_customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":{},\"s\":[],\"o\":true,\"a\":1},\"lenDiffer\":[1,2],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}", events[1].ConvertToJson().ToString());
        }

        [TearDown]
        public void ContainerCleanup()
        {
            TestUtils.CleanupEvents();
        }

    }
}
