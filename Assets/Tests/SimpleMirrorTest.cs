﻿using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class SimpleMirrorTest
    {
        private BeatmapActionContainer _actionContainer;
        private MirrorSelection _mirror;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            yield return TestUtils.LoadMap(3);

            _actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            _mirror = Object.FindObjectOfType<MirrorSelection>();
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNotes();
            CleanupUtils.CleanupEvents();
            CleanupUtils.CleanupObstacles();
        }

        [Test]
        public void MirrorME()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA =
                new V3ColorNote(2, -2345, (int)GridY.Base, (int)NoteType.Red, (int)NoteCutDirection.Left);

            PlaceUtils.PlaceNote(notePlacement, baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform note mirror", notesContainer, 0, 2, 5345, (int)GridY.Base, (int)NoteType.Blue,
                (int)NoteCutDirection.Right, 0);

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo note mirror", notesContainer, 0, 2, -2345, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Left, 0);
        }

        [Test]
        public void MirrorNoteNE()
        {
            var notesContainer = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            var root = notesContainer.transform.root;
            var notePlacement = root.GetComponentInChildren<NotePlacement>();

            BaseNote baseNoteA = new V3ColorNote(2, (int)GridX.Left, (int)GridY.Base, (int)NoteType.Red,
                (int)NoteCutDirection.Left, JSON.Parse("{\"coordinates\": [-1, 0]}"));

            PlaceUtils.PlaceNote(notePlacement, baseNoteA);

            SelectionController.Select(baseNoteA);

            _mirror.Mirror();
            CheckUtils.CheckNote("Perform NE note mirror", notesContainer, 0, 2, (int)GridX.Right, (int)GridY.Base,
                (int)NoteType.Blue, (int)NoteCutDirection.Right, 0,
                JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [0, 0]}}"));

            // Undo mirror
            _actionContainer.Undo();
            CheckUtils.CheckNote("Undo NE note inversion", notesContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                (int)NoteType.Red, (int)NoteCutDirection.Left, 0,
                JSON.Parse($"{{\"{baseNoteA.CustomKeyCoordinate}\": [-1, 0]}}"));
        }

        [Test]
        public void MirrorProp()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3BasicEvent(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f,
                    JSON.Parse("{\"lightID\": 2}"));

                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

                SelectionController.Select(baseEventA);

                eventsContainer.EventTypeToPropagate = baseEventA.Type;
                eventsContainer.PropagationEditing = EventGridContainer.PropMode.Light;

                _mirror.Mirror();
                // I'm sorry if you're here after changing the prop mapping for default env
                CheckUtils.CheckEvent("Perform mirror prop event", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f, JSON.Parse("{\"lightID\": [9]}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckEvent("Undo mirror prop event", eventsContainer, 0, 2, (int)EventTypeValue.BackLasers,
                    (int)LightValue.RedFade, 1f, JSON.Parse("{\"lightID\": [2]}"));

                eventsContainer.PropagationEditing = EventGridContainer.PropMode.Off;
            }
        }

        [Test]
        public void MirrorGradient()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V2Event(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f,
                    JSON.Parse(
                        "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));

                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

                SelectionController.Select(baseEventA);

                _mirror.Mirror();
                CheckUtils.CheckEvent("Perform mirror gradient event", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f,
                    JSON.Parse(
                        "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [0, 1, 0, 1], \"_endColor\": [1, 0, 0, 1], \"_easing\": \"easeLinear\"}}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckEvent("Undo mirror gradient event", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f,
                    JSON.Parse(
                        "{\"_lightGradient\": {\"_duration\": 1, \"_startColor\": [1, 0, 0, 1], \"_endColor\": [0, 1, 0, 1], \"_easing\": \"easeLinear\"}}"));
            }
        }

        [Test]
        public void MirrorEventRedBlue()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V2Event(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);

                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

                SelectionController.Select(baseEventA);

                _mirror.Mirror();
                CheckUtils.CheckEvent("Perform mirror event", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f);

                _mirror.Mirror();
                CheckUtils.CheckEvent("Perform mirror event again", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);
            }
        }

        [Test]
        public void MirrorEventRedWhiteBlue()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V2Event(2, (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);

                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

                SelectionController.Select(baseEventA);

                _mirror.Mirror(false);
                CheckUtils.CheckEvent("Perform mirror cycle event", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.WhiteFade, 1f);

                _mirror.Mirror(false);
                CheckUtils.CheckEvent("Perform mirror cycle event 2", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.BlueFade, 1f);

                _mirror.Mirror(false);
                CheckUtils.CheckEvent("Perform mirror cycle event 3", eventsContainer, 0, 2,
                    (int)EventTypeValue.BackLasers, (int)LightValue.RedFade, 1f);
            }
        }

        [Test]
        public void MirrorWallME()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (container is ObstacleGridContainer wallsContainer)
            {
                var root = wallsContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                // What the actual fuck - example from mirroring in MMA2
                //{"_time":1.5,"_lineIndex":1446,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                //{"_time":1.5,"_lineIndex":2958,"_type":595141,"_duration":0.051851850003004074,"_width":2596}
                BaseObstacle wallA = new V2Obstacle(2, 1446, 595141, 1, 2596);

                PlaceUtils.PlaceWall(wallPlacement, wallA);

                SelectionController.Select(wallA);

                _mirror.Mirror();
                CheckUtils.CheckWall("Perform ME wall mirror", wallsContainer, 0, 2, 2958, 0, 595141, 1, 2596, 5);

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckWall("Undo ME wall mirror", wallsContainer, 0, 2, 1446, 0, 595141, 1, 2596, 5);
            }
        }

        [Test]
        public void MirrorWallNE()
        {
            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (container is ObstacleGridContainer wallsContainer)
            {
                var root = wallsContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                BaseObstacle wallA = new V3Obstacle(2, (int)GridX.Left, (int)GridY.Base, 1, 2, 5,
                    JSON.Parse("{\"coordinates\": [-1.5, 0]}"));

                PlaceUtils.PlaceWall(wallPlacement, wallA);

                SelectionController.Select(wallA);

                _mirror.Mirror();
                CheckUtils.CheckWall("Perform NE wall mirror", wallsContainer, 0, 2, (int)GridX.MiddleRight,
                    (int)GridY.Base, (int)ObstacleType.Full, 1, 2, 5,
                    JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-0.5, 0]}}"));

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckWall("Undo NE wall mirror", wallsContainer, 0, 2, (int)GridX.Left, (int)GridY.Base,
                    (int)ObstacleType.Full, 1, 2, 5, JSON.Parse($"{{\"{wallA.CustomKeyCoordinate}\": [-1.5, 0]}}"));
            }
        }

        // TODO: update rotation event test for more representative
        [Test]
        public void MirrorRotation()
        {
            var rotationCb = Object.FindObjectOfType<RotationCallbackController>();
            rotationCb.Start();

            var container = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Event);
            if (container is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();

                BaseEvent baseEventA = new V3RotationEvent(2, 1, 33);

                PlaceUtils.PlaceEvent(eventPlacement, baseEventA);

                SelectionController.Select(baseEventA);

                _mirror.Mirror();
                CheckUtils.CheckRotationEvent("Perform mirror rotation event", eventsContainer, 0, 2, 1, -33);

                // Undo mirror
                _actionContainer.Undo();
                CheckUtils.CheckRotationEvent("Undo mirror rotation event", eventsContainer, 0, 2, 1, 33);
            }
        }
    }
}