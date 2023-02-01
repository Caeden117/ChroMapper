using System;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapV3Test
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
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
            CleanupUtils.CleanupObstacles();
            CleanupUtils.CleanupEvents();
            CleanupUtils.CleanupArcs();
            CleanupUtils.CleanupChains();
        }

        [Test]
        public void IsV3Map()
        {
            Assert.IsInstanceOf<V3Difficulty>(BeatSaberSongContainer.Instance.Map,
                "Beatmap instance version should be v3");
            Assert.IsTrue(Settings.Instance.Load_MapV3,
                "Settings Load Beatmap V3 should be true, otherwise may cause unnecessary issue");
        }

        [Test]
        public void PlaceNote()
        {
            Assert.IsInstanceOf<V3ColorNote>(BeatmapFactory.Note(),
                "Factory default does not instantiate v3 note in beatmap v3");
            Assert.IsInstanceOf<V3ColorNote>(BeatmapFactory.Note(0f, 1, 2, 0, 1, 0),
                "Factory does not instantiate v3 note in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Note(new JSONObject
            {
                ["b"] = 0f,
                ["x"] = 1,
                ["y"] = 2,
                ["c"] = 0,
                ["d"] = 1,
                ["a"] = 0,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate note with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Note(new JSONObject
            {
                ["_time"] = 0f,
                ["_lineIndex"] = 1,
                ["_lineLayer"] = 2,
                ["_type"] = 0,
                ["_cutDirection"] = 1,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating note with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                notePlacement.RefreshVisuals();

                var noteA = BeatmapFactory.Note(0f, 1, 2, 0, 1, 0);
                PlaceUtils.PlaceNote(notePlacement, noteA);

                CheckUtils.CheckV3Object("Check note object version", notesContainer, 0);
                CheckUtils.CheckNote("Check note attributes", notesContainer, 0, 0f, 1, 2, 0, 1, 0);
            }
        }

        [Test]
        public void PlaceBomb()
        {
            Assert.IsInstanceOf<V3BombNote>(BeatmapFactory.Bomb(),
                "Factory default does not instantiate v3 bomb in beatmap v3");
            Assert.IsInstanceOf<V3BombNote>(BeatmapFactory.Bomb(0f, 1, 2),
                "Factory does not instantiate v3 bomb in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Bomb(new JSONObject
            {
                ["b"] = 0f,
                ["x"] = 1,
                ["y"] = 2,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate bomb with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Note(new JSONObject
            {
                ["_time"] = 0f,
                ["_lineIndex"] = 1,
                ["_lineLayer"] = 2,
                ["_type"] = 3,
                ["_cutDirection"] = 0,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating bomb with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is NoteGridContainer notesContainer)
            {
                var root = notesContainer.transform.root;
                var notePlacement = root.GetComponentInChildren<NotePlacement>();
                notePlacement.RefreshVisuals();

                var noteA = BeatmapFactory.Bomb(0f, 1, 2);
                PlaceUtils.PlaceNote(notePlacement, noteA);

                CheckUtils.CheckV3Object("Check bomb object version", notesContainer, 0);
                CheckUtils.CheckNote("Check bomb attributes", notesContainer, 0, 0f, 1, 2, 3, 0, 0);
            }
        }

        [Test]
        public void PlaceArc()
        {
            Assert.IsInstanceOf<V3Arc>(BeatmapFactory.Arc(),
                "Factory default does not instantiate v3 arc in beatmap v3");
            Assert.IsInstanceOf<V3Arc>(BeatmapFactory.Arc(0f, 1, 2, 0, 1, 0, 1, 1f, 2, 1, 0, 1, 0),
                "Factory does not instantiate v3 arc in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Arc(new JSONObject
            {
                ["b"] = 0f,
                ["x"] = 1,
                ["y"] = 2,
                ["c"] = 0,
                ["d"] = 1,
                ["mu"] = 1f,
                ["tb"] = 1f,
                ["tx"] = 2,
                ["ty"] = 1,
                ["tc"] = 0,
                ["tmu"] = 1f,
                ["m"] = 0,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate arc with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Arc(new JSONObject
            {
                ["_colorType"] = 0,
                ["_headTime"] = 0f,
                ["_headLineIndex"] = 1,
                ["_headLineLayer"] = 2,
                ["_headCutDirection"] = 1,
                ["_headControlPointLengthMultiplier"] = 1f,
                ["_tailTime"] = 1f,
                ["_tailLineIndex"] = 2,
                ["_tailLineLayer"] = 1,
                ["_tailCutDirection"] = 0,
                ["_tailControlPointLengthMultiplier"] = 1f,
                ["_sliderMidAnchorMode"] = 0,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating arc with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Arc);
            if (collection is ArcGridContainer arcsContainer)
            {
                var root = arcsContainer.transform.root;
                var arcPlacement = root.GetComponentInChildren<ArcPlacement>();
                arcPlacement.RefreshVisuals();

                var arcA = BeatmapFactory.Arc(0f, 1, 2, 0, 1, 0, 1, 1f, 2, 1, 0, 1f, 0);
                PlaceUtils.PlaceArc(arcPlacement, arcA);

                CheckUtils.CheckV3Object("Check arc object version", arcsContainer, 0);
                CheckUtils.CheckArc("Check arc attributes", arcsContainer, 0, 0f, 1, 2, 0, 1, 0, 1, 1f, 2, 1, 0, 1f, 0);
            }
        }

        [Test]
        public void PlaceChain()
        {
            Assert.IsInstanceOf<V3Chain>(BeatmapFactory.Chain(),
                "Factory default does not instantiate v3 chain in beatmap v3");
            Assert.IsInstanceOf<V3Chain>(BeatmapFactory.Chain(0f, 1, 2, 0, 1, 0, 1f, 1, 2, 5, 1),
                "Factory does not instantiate v3 chain in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Chain(new JSONObject
            {
                ["b"] = 0f,
                ["x"] = 1,
                ["y"] = 2,
                ["c"] = 0,
                ["d"] = 1,
                ["tb"] = 1f,
                ["tx"] = 2,
                ["ty"] = 1,
                ["tc"] = 0,
                ["sc"] = 5,
                ["s"] = 1,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate chain with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Chain(new JSONObject
            {
                ["_colorType"] = 0,
                ["_headTime"] = 0f,
                ["_headLineIndex"] = 1,
                ["_headLineLayer"] = 2,
                ["_headCutDirection"] = 1,
                ["_headControlPointLengthMultiplier"] = 1f,
                ["_tailTime"] = 1f,
                ["_tailLineIndex"] = 2,
                ["_tailLineLayer"] = 1,
                ["_tailCutDirection"] = 0,
                ["_tailControlPointLengthMultiplier"] = 1f,
                ["_sliderMidAnchorMode"] = 0,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating chain with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Chain);
            if (collection is ChainGridContainer chainsContainer)
            {
                var root = chainsContainer.transform.root;
                var chainPlacement = root.GetComponentInChildren<ChainPlacement>();
                chainPlacement.RefreshVisuals();

                var chainA = BeatmapFactory.Chain(0f, 1, 2, 0, 1, 0, 1f, 1, 2, 5, 1);
                PlaceUtils.PlaceChain(chainPlacement, chainA);

                CheckUtils.CheckV3Object("Check chain object version", chainsContainer, 0);
                CheckUtils.CheckChain("Check chain attributes", chainsContainer, 0, 0f, 1, 2, 0, 1, 0, 1f, 1, 2, 5, 1);
            }
        }

        [Test]
        public void PlaceWall()
        {
            Assert.IsInstanceOf<V3Obstacle>(BeatmapFactory.Obstacle(),
                "Factory default does not instantiate v3 wall in beatmap v3");
            Assert.IsInstanceOf<V3Obstacle>(BeatmapFactory.Obstacle(0f, 1, 0, 1f, 1, 5),
                "Factory does not instantiate v3 wall in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Obstacle(new JSONObject
            {
                ["b"] = 0f,
                ["x"] = 1,
                ["y"] = 0,
                ["d"] = 1f,
                ["w"] = 1,
                ["h"] = 5,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate wall with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Obstacle(new JSONObject
            {
                ["_time"] = 0f,
                ["_lineIndex"] = 1,
                ["_lineLayer"] = 0,
                ["_type"] = 0,
                ["_duration"] = 1f,
                ["_width"] = 1,
                ["_height"] = 5,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating wall with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (collection is ObstacleGridContainer obstaclesContainer)
            {
                var root = obstaclesContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                var wallA = BeatmapFactory.Obstacle(0f, 1, 0, 1f, 1, 5);
                PlaceUtils.PlaceWall(wallPlacement, wallA);

                CheckUtils.CheckV3Object("Check wall object version", obstaclesContainer, 0);
                CheckUtils.CheckWall("Check wall attributes", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);
            }
        }

        [Test]
        public void PlaceEvent()
        {
            Assert.IsInstanceOf<V3BasicEvent>(BeatmapFactory.Event(),
                "Factory default does not instantiate v3 event in beatmap v3");
            Assert.IsInstanceOf<V3BasicEvent>(BeatmapFactory.Event(0f, 0, 1),
                "Factory does not instantiate v3 event in beatmap v3");
            Assert.DoesNotThrow(() => BeatmapFactory.Event(new JSONObject
            {
                ["b"] = 0f,
                ["et"] = 0,
                ["i"] = 1,
                ["f"] = 1f,
                ["customData"] = new JSONObject()
            }), "Factory could not instantiate event with compatible JSON schema in beatmap v3");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Event(new JSONObject
            {
                ["_time"] = 0f,
                ["_type"] = 0,
                ["_value"] = 1,
                ["_floatValue"] = 1f,
                ["_customData"] = new JSONObject()
            }), "Factory should throw error instantiating event with incompatible JSON schema in beatmap v3");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                eventPlacement.RefreshVisuals();

                var eventA = BeatmapFactory.Event(2.5f, 1, 2, 0);
                PlaceUtils.PlaceEvent(eventPlacement, eventA);

                CheckUtils.CheckV3Object("Check note object version", eventsContainer, 0);
                CheckUtils.CheckEvent("Check note attributes", eventsContainer, 0, 2.5f, 0, 1);
            }
        }
    }
}