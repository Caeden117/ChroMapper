using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V3;
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
            BeatmapObjectContainerCollection eventContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            NodeEditorController nodeEditor = Object.FindObjectOfType<NodeEditorController>();
            TMP_InputField inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.BackLasers, (int)LightValue.Off, 1, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1]}"));
            eventContainer.SpawnObject(baseEventA);

            BaseEvent baseEventB = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.Off, 1, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2]}"));
            eventContainer.SpawnObject(baseEventB);

            BaseEvent baseEventC = new V3BasicEvent(2, (int)EventTypeValue.RightLasers, (int)LightValue.Off);
            eventContainer.SpawnObject(baseEventC);

            SelectionController.Select(baseEventC);
            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : 3,\n  \"i\" : 0,\n  \"f\" : 1\n}", inputField.text);

            SelectionController.Select(baseEventA);
            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : 0,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"lenDiffer\" : [\n      1\n    ]\n  }\n}", inputField.text);

            SelectionController.Select(baseEventB, true);
            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : -,\n      \"s\" : -,\n      \"b\" : -,\n      \"a\" : [\n        -,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n    }\n  }\n}", inputField.text);
        }

        [Test]
        public void JsonApply()
        {
            BeatmapObjectContainerCollection eventContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            NodeEditorController nodeEditor = Object.FindObjectOfType<NodeEditorController>();
            TMP_InputField inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.BackLasers, (int)LightValue.Off, 1f, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1],""updatedLenDiffer"":[1],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}"));
            eventContainer.SpawnObject(baseEventA);

            BaseEvent baseEventB = new V3BasicEvent(2, (int)EventTypeValue.LeftLasers, (int)LightValue.Off, 0.5f, JSON.Parse(@"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2],""updatedLenDiffer"":[1,2],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""updatedTypeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1}}"));
            eventContainer.SpawnObject(baseEventB);

            SelectionController.Select(baseEventA);
            SelectionController.Select(baseEventB, true);

            nodeEditor.NodeEditor_EndEdit(@"{""b"": -, ""et"": -, ""i"": -, ""f"": -, ""customData"": {""matches"":{},""differs"":{},""typeDiffer"":{},""updatedLenDiffer"":[1],""updated"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedDiffer"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}}");

            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : -,\n  \"customData\" : {\n    \"matches\" : {\n    },\n    \"differs\" : {\n    },\n    \"typeDiffer\" : {\n    },\n    \"updatedLenDiffer\" : [\n      1\n    ],\n    \"updated\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedDiffer\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedTypeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    }\n  }\n}", inputField.text);

            // Objects have been recreated, pick them up from the selection controller
            BaseObject[] events = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual("{\"b\":2,\"et\":0,\"i\":0,\"f\":1,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]},\"lenDiffer\":[1],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}", events[0].ToJson().ToString());
            Assert.AreEqual("{\"b\":2,\"et\":2,\"i\":0,\"f\":0.5,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":{},\"s\":[],\"o\":true,\"a\":1},\"lenDiffer\":[1,2],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}", events[1].ToJson().ToString());
        }

        [TearDown]
        public void ContainerCleanup()
        {
            TestUtils.CleanupEvents();
        }

    }
}
