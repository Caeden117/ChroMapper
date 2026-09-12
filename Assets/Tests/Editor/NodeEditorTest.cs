using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    internal class NodeEditorTest : TestBase
    {
        protected override IEnumerator OnMapLoaded()
        {
            NodeEditorController.IsActive = true;
            Settings.Instance.MapVersion = 3;
            yield break;
        }

        protected override void OnReturnSettings()
        {
            NodeEditorController.IsActive = false;
        }

        [Test]
        public void JsonMerge()
        {
            Object.FindAnyObjectByType<EventPlacement>();
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.Off,
                FloatValue = 1,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1]}")
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.Off,
                FloatValue = 1,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2]}")
            };
            var eventC = new BaseEvent
            {
                JsonTime = 2, Type = (int)EventTypeValue.Event3, Value = (int)LightValue.Off
            };
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);
            eventC = PlaceUtils.Place(eventC);

            SelectionController.Select(eventC);
            Assert.AreEqual("{\n  \"b\" : 2,\n  \"et\" : 3,\n  \"i\" : 0,\n  \"f\" : 1\n}", inputField.text);

            SelectionController.Select(eventA);
            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : 0,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"lenDiffer\" : [\n      1\n    ]\n  }\n}",
                inputField.text);

            SelectionController.Select(eventB, true);
            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : 1,\n  \"customData\" : {\n    \"matches\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"b\" : true,\n      \"a\" : [\n        1,\n        2\n      ]\n    },\n    \"differs\" : {\n      \"i\" : -,\n      \"s\" : -,\n      \"b\" : -,\n      \"a\" : [\n        -,\n        2\n      ]\n    },\n    \"typeDiffer\" : {\n    }\n  }\n}",
                inputField.text);
        }

        [Test]
        public void JsonApply()
        {
            Object.FindAnyObjectByType<EventPlacement>();
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();

            var eventA = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event0,
                Value = (int)LightValue.Off,
                FloatValue = 1f,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""typeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]},""lenDiffer"":[1],""updatedLenDiffer"":[1],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}")
            };
            var eventB = new BaseEvent
            {
                JsonTime = 2,
                Type = (int)EventTypeValue.Event2,
                Value = (int)LightValue.Off,
                FloatValue = 0.5f,
                CustomData =
                    JSON.Parse(
                        @"{""matches"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""differs"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""typeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1},""lenDiffer"":[1,2],""updatedLenDiffer"":[1,2],""updated"":{""i"":1,""s"":""s"",""b"":true,""a"":[1,2]},""updatedDiffer"":{""i"":2,""s"":""t"",""b"":false,""a"":[2,2]},""updatedTypeDiffer"":{""i"":{},""s"":[],""o"":true,""a"":1}}")
            };
            eventA = PlaceUtils.Place(eventA);
            eventB = PlaceUtils.Place(eventB);

            SelectionController.Select(eventA);
            SelectionController.Select(eventB, true);

            nodeEditor.NodeEditor_EndEdit(
                @"{""b"": -, ""et"": -, ""i"": -, ""f"": -, ""customData"": {""matches"":{},""differs"":{},""typeDiffer"":{},""updatedLenDiffer"":[1],""updated"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedDiffer"":{""i"":4,""s"":""q"",""b"":false,""a"":[3,2]},""updatedTypeDiffer"":{""i"":1,""s"":""s"",""o"":{},""a"":[1,2]}}}");

            var selectedObjects = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(2, selectedObjects.Length, "Exactly two objects should be selected after NodeEditor_EndEdit");
            var selectedEvents = selectedObjects.OfType<BaseEvent>().ToArray();
            Assert.AreEqual(selectedObjects.Length, selectedEvents.Length);
            foreach (var sel in selectedEvents)
            {
                BeatmapAssertion.IsInCollection(sel, $"Selected object of type {sel.ObjectType} must be present in its BeatmapObjectContainerCollection");
            }

            Assert.AreEqual(
                "{\n  \"b\" : 2,\n  \"et\" : -,\n  \"i\" : 0,\n  \"f\" : -,\n  \"customData\" : {\n    \"matches\" : {\n    },\n    \"differs\" : {\n    },\n    \"typeDiffer\" : {\n    },\n    \"updatedLenDiffer\" : [\n      1\n    ],\n    \"updated\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedDiffer\" : {\n      \"i\" : 4,\n      \"s\" : \"q\",\n      \"b\" : false,\n      \"a\" : [\n        3,\n        2\n      ]\n    },\n    \"updatedTypeDiffer\" : {\n      \"i\" : 1,\n      \"s\" : \"s\",\n      \"o\" : {\n      },\n      \"a\" : [\n        1,\n        2\n      ]\n    }\n  }\n}",
                inputField.text);

            // Objects have been recreated, pick them up from the selection controller
            var events = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(
                "{\"b\":2,\"et\":0,\"i\":0,\"f\":1,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]},\"lenDiffer\":[1],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}",
                events[0].ToJson().ToString());
            Assert.AreEqual(
                "{\"b\":2,\"et\":2,\"i\":0,\"f\":0.5,\"customData\":{\"matches\":{},\"differs\":{},\"typeDiffer\":{\"i\":{},\"s\":[],\"o\":true,\"a\":1},\"lenDiffer\":[1,2],\"updatedLenDiffer\":[1],\"updated\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedDiffer\":{\"i\":4,\"s\":\"q\",\"b\":false,\"a\":[3,2]},\"updatedTypeDiffer\":{\"i\":1,\"s\":\"s\",\"o\":{},\"a\":[1,2]}}}",
                events[1].ToJson().ToString());
        }

        // ClosingNodeEditorCommitsAnimateTrackJson proves custom-event Data survives the same end-edit path as other objects.
        [Test]
        public void ClosingNodeEditorCommitsAnimateTrackJson()
        {
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();
            var customEvent = BeatmapFactory.CustomEvent(JSON.Parse(
                @"{ ""b"": 2, ""t"": ""AnimateTrack"", ""d"": { ""track"": ""animationTrack"", ""duration"": 1, ""position"": [[0, 0, 0, 0], [1, 0, 0, 1]] } }"));
            BeatmapObjectContainerCollection
                .GetCollectionForType<CustomEventGridContainer>(ObjectType.CustomEvent)
                .SpawnObject(customEvent, false, false, true);
            SelectionController.Select(customEvent);
            nodeEditor.NodeEditor_StartEdit(inputField.text);

            var editedJson =
                @"{ ""b"": 2, ""t"": ""AnimateTrack"", ""d"": { ""track"": ""animationTrack"", ""duration"": 1, ""position"": [[0, 0, 0, 0], [2, 0, 0, 1]] } }";
            inputField.SetTextWithoutNotify(editedJson);

            try
            {
                inputField.onEndEdit.Invoke(inputField.text);
                nodeEditor.Close();

                var savedEvent = SelectionController.SelectedObjects.Single() as BaseCustomEvent;
                Assert.IsNotNull(savedEvent, "The edited AnimateTrack event should remain selected after closing the node editor.");
                StringAssert.Contains(
                    "\"position\":[[0,0,0,0],[2,0,0,1]]",
                    savedEvent.ToJson().ToString(),
                    "Closing the node editor should preserve the edited AnimateTrack data.");
            }
            finally
            {
                NodeEditorController.IsActive = true;
            }
        }

        [Test]
        // Keep inner-node merging distinct from outer group merging so each selection path is covered.
        public void GlsJsonInnerEventMerge()
        {
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();

            // Merge only nodes from one active GLS group, matching ordinary node multi-selection semantics.
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [1, 0, 0] } }, { ""b"": 0.75, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 0, 1] } } ] } ] }"));

            provider.GroupContext = group;
            PlaceGlsGroup(group);

            var evt = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            SelectionController.Select(evt);
            StringAssert.Contains("\"customData\"", inputField.text);
            StringAssert.Contains("\"color\"", inputField.text);
            StringAssert.Contains("\"color\" : [\n      1,\n      0,\n      0\n    ]", inputField.text);

            var evt2 = group.ReadOnlyBoxes[0].ReadOnlyEvents[1];
            SelectionController.Select(evt2, true);

            StringAssert.Contains("\"customData\"", inputField.text);
            StringAssert.Contains("\"color\"", inputField.text);
            StringAssert.Contains("\"color\" : [\n      -,\n      0,\n      -\n    ]", inputField.text);
        }

        [Test]
        // Select complete GLS groups so the node editor must merge both group fields and their nested event boxes.
        public void GlsJsonOuterEventGroupMerge()
        {
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();

            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [1, 0, 0] } } ] } ] }"));

            provider.GroupContext = group;
            PlaceGlsGroup(group);
            SelectionController.Select(group);

            // Normalize presentation whitespace so these assertions focus on the complete group's JSON structure.
            var normalizedJson = inputField.text.Replace(" ", string.Empty).Replace("\n", string.Empty);
            StringAssert.Contains("\"b\":2,\"g\":1,\"e\":[{", normalizedJson);
            StringAssert.Contains("\"customData\":{\"color\":[1,0,0]}", normalizedJson);

            var group2 = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 3, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 0, 1] } } ] } ] }"));

            provider.GroupContext = group2;
            PlaceGlsGroup(group2);
            SelectionController.Select(group2, true);

            // Prove this path is merging two outer group objects rather than their flattened inner events.
            Assert.AreEqual(2, SelectionController.SelectedObjects.Count);
            Assert.IsTrue(SelectionController.SelectedObjects.All(obj => obj is BaseEventBoxGroup));

            // The outer beat differs while shared group data and recursively merged event-box data remain visible.
            normalizedJson = inputField.text.Replace(" ", string.Empty).Replace("\n", string.Empty);
            StringAssert.Contains("\"b\":-,\"g\":1,\"e\":[{", normalizedJson);
            StringAssert.Contains("\"customData\":{\"color\":[-,0,-]}", normalizedJson);
        }

        [Test]
        public void GLSJsonApply()
        {
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();

            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [1, 0, 0] } } ] } ] }"));

            provider.GroupContext = group;
            PlaceGlsGroup(group);

            var evt = group.ReadOnlyBoxes[0].ReadOnlyEvents[0];
            SelectionController.Select(evt);

            nodeEditor.NodeEditor_EndEdit(
                @"{ ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 1, 0] } }");

            var selected = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(1, selected.Length);
            Assert.AreEqual(
                "{\"b\":0.5,\"c\":0,\"s\":1,\"i\":0,\"f\":0,\"sb\":0,\"sf\":0,\"customData\":{\"color\":[0,1,0]}}",
                selected[0].ToJson().ToString());

            var group2 = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [1, 0, 0] } }, { ""b"": 0.75, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 0, 1] } } ] } ] }"));

            provider.GroupContext = group2;
            PlaceGlsGroup(group2);

            var events = group2.ReadOnlyBoxes[0].ReadOnlyEvents;
            SelectionController.Select(events[0]);
            SelectionController.Select(events[1], true);

            nodeEditor.NodeEditor_EndEdit(
                @"{ ""b"": -, ""c"": 0, ""s"": 0.5, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 0, 1] } }");

            selected = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(2, selected.Length);
            foreach (var obj in selected)
            {
                // Match the serialized GLS field name exactly; the previous expectation contained an extra quote.
                StringAssert.Contains("\"s\":0.5", obj.ToJson().ToString());
                // Include the customData wrapper used by GLS serialization when checking the applied color.
                StringAssert.Contains("\"customData\":{\"color\":[0,0,1]}", obj.ToJson().ToString());
            }
        }

        // Node-editor GLS edits must round-trip the authored node state without duplicating or orphaning inner nodes.
        [Test]
        public void GLSNodeEditorEditRoundTripsInnerNodeWithoutDuplicates()
        {
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();
            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [1, 0, 0] } } ] } ] }"));

            provider.GroupContext = group;
            PlaceGlsGroup(group);
            SelectionController.Select(group.ReadOnlyBoxes[0].ReadOnlyEvents[0]);
            // Isolate this edit so a second undo precisely detects an action leaked by the inner GLS replacement path.
            var actionContainer = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actionContainer.ClearBeatmapActions();

            nodeEditor.NodeEditor_EndEdit(
                @"{ ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0, ""customData"": { ""color"": [0, 1, 0] } }");
            AssertSingleInnerGlsColor(provider.GroupContext, Color.green);

            actionContainer.Undo();
            AssertSingleInnerGlsColor(provider.GroupContext, Color.red);
            actionContainer.Redo();
            AssertSingleInnerGlsColor(provider.GroupContext, Color.green);
            actionContainer.Undo();
            AssertSingleInnerGlsColor(provider.GroupContext, Color.red);
            Assert.IsNull(actionContainer.Undo(), "The node edit must not leave an extra inner-GLS undo action behind.");
        }

        // Assert the saved GLS group remains structurally valid after each history operation, rather than inspecting its action implementation.
        private static void AssertSingleInnerGlsColor(BaseEventBoxGroup group, Color expectedColor)
        {
            var colorEvents = group.ReadOnlyBoxes
                .SelectMany(box => box.ReadOnlyEvents)
                .OfType<BaseLightColorBase>()
                .ToArray();
            Assert.AreEqual(1, colorEvents.Length);
            Assert.True(colorEvents[0].CustomColor.HasValue);
            Assert.AreEqual(expectedColor, colorEvents[0].CustomColor.Value);
        }

        [Test]
        public void GLSGroupCustomDataApply()
        {
            // Test that editing outer event box group customData via Node Editor saves correctly
            var nodeEditor = Object.FindAnyObjectByType<NodeEditorController>();
            var inputField = nodeEditor.GetComponentInChildren<TMP_InputField>();
            var provider = Object.FindAnyObjectByType<GLSEventGridProvider>();

            var group = BeatmapFactory.LightColorEventBoxGroups(JSON.Parse(
                @"{ ""b"": 2, ""g"": 1, ""customData"": { ""groupData"": ""original"" }, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] } ] }"));

            provider.GroupContext = group;
            // Select only placed groups so the node editor exercises the real manager/container replacement path.
            PlaceGlsGroup(group);

            SelectionController.Select(group);

            nodeEditor.NodeEditor_EndEdit(
                @"{ ""b"": 2, ""g"": 1, ""customData"": { ""groupData"": ""edited"" }, ""e"": [ { ""f"": { ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }, ""w"": 1, ""d"": 0, ""r"": 0, ""t"": 0, ""b"": 0, ""i"": 0, ""e"": [ { ""b"": 0.5, ""c"": 0, ""s"": 1, ""i"": 0, ""f"": 0, ""sb"": 0, ""sf"": 0 } ] } ] }");

            var selected = SelectionController.SelectedObjects.ToArray();
            Assert.AreEqual(1, selected.Length);
            var json = selected[0].ToJson().ToString();
            StringAssert.Contains("\"groupData\":\"edited\"", json);
        }

        [Test]
        public void GLSEventApplyMethod()
        {
            // Test that BaseLightColorBase.Apply correctly copies customData
            var original = new BaseLightColorBase
            {
                JsonTime = 2,
                RelativeJsonTime = 0.5f,
                Color = 0,
                Brightness = 1,
                CustomData = JSON.Parse(@"{ ""color"": [1, 0, 0], ""lerpType"": ""smooth"" }")
            };

            var edited = new BaseLightColorBase
            {
                JsonTime = 2,
                RelativeJsonTime = 0.5f,
                Color = 0,
                Brightness = 1,
                CustomData = JSON.Parse(@"{ ""color"": [0, 1, 0] }")
            };

            edited.Apply(original);

            var json = edited.ToJson().ToString();
            StringAssert.Contains("\"color\":[1,0,0]", json); // Should replace the things that are in there
            StringAssert.Contains("\"lerpType\":\"smooth\"", json);
        }

        [Test]
        public void GLSEventBoxGroupApplyMethod()
        {
            // Test that BaseEventBoxGroup<T>.Apply correctly copies customData and boxes
            var originalBox = new BaseLightColorEventBox
            {
                IndexFilter = V3IndexFilter.GetFromJson(JSON.Parse(@"{ ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }")),
                Events = new[]
                {
                    new BaseLightColorBase
                    {
                        JsonTime = 2,
                        RelativeJsonTime = 0.5f,
                        Color = 0,
                        Brightness = 1,
                        CustomData = JSON.Parse(@"{ ""color"": [1, 0, 0] }")
                    }
                }
            };

            var originalGroup = new BaseLightColorEventBoxGroup
            {
                JsonTime = 2,
                ID = 1,
                CustomData = JSON.Parse(@"{ ""groupData"": ""original"" }"),
                Boxes = new List<BaseLightColorEventBox> { originalBox }
            };

            var editedGroup = new BaseLightColorEventBoxGroup
            {
                JsonTime = 2,
                ID = 1,
                CustomData = JSON.Parse(@"{ ""groupData"": ""edited"" }"),
                Boxes = new List<BaseLightColorEventBox> { originalBox }
            };

            editedGroup.Apply(originalGroup);

            var json = editedGroup.ToJson().ToString();
            StringAssert.Contains("\"groupData\":\"original\"", json);
            StringAssert.Contains("\"color\":[1,0,0]", json);
        }

        [Test]
        public void GLSEventBoxGroupCopyConstructor()
        {
            // Test that copy constructors preserve customData
            var original = new BaseLightColorEventBoxGroup
            {
                JsonTime = 2,
                ID = 1,
                CustomData = JSON.Parse(@"{ ""groupData"": ""original"" }"),
                Boxes = new List<BaseLightColorEventBox>
                {
                    new BaseLightColorEventBox
                    {
                        IndexFilter = V3IndexFilter.GetFromJson(JSON.Parse(@"{ ""f"": 0, ""p"": 0, ""t"": 0, ""r"": 0, ""c"": 0, ""n"": 0, ""s"": 0, ""l"": 0, ""d"": 0 }")),
                        Events = new[]
                        {
                            new BaseLightColorBase
                            {
                                JsonTime = 2,
                                RelativeJsonTime = 0.5f,
                                Color = 0,
                                Brightness = 1,
                                CustomData = JSON.Parse(@"{ ""color"": [1, 0, 0] }")
                            }
                        }
                    }
                }
            };

            var cloned = original.Clone() as BaseLightColorEventBoxGroup;

            Assert.IsNotNull(cloned);
            Assert.AreEqual(1, cloned.ID);
            Assert.AreEqual(2, cloned.JsonTime);

            var json = cloned.ToJson().ToString();
            StringAssert.Contains("\"groupData\":\"original\"", json);
            StringAssert.Contains("\"color\":[1,0,0]", json);
        }

        private static void PlaceGlsGroup(BaseEventBoxGroup group)
        {
            // Node apply must use a parent managed by GLSManager so child replacement and selection match editor behavior.
            BeatmapObjectContainerCollection.GetCollectionForType(group.ObjectType).SpawnObject(group, false, false, true);
        }
    }
}
