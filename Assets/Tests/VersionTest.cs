using System;
using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.Helper;
using Beatmap.V3;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class VersionTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMapper();
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

        [OneTimeTearDown]
        public void FinalCleanup()
        {
            TestUtils.ReturnSettings();
        }

        [Test]
        public void PlaceV3Note()
        {
            Assert.IsInstanceOf<V3ColorNote>(BeatmapFactory.Note(), "Factory default does not instantiate v3 note in beatmap v3");
            Assert.IsInstanceOf<V3ColorNote>(BeatmapFactory.Note(0f, 1, 2, 0, 1, 0), "Factory does not instantiate v3 note in beatmap v3");
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
            
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is NoteGridContainer notesContainer)
            {
                Transform root = notesContainer.transform.root;
                NotePlacement notePlacement = root.GetComponentInChildren<NotePlacement>();
                notePlacement.RefreshVisuals();

                BaseNote noteA = BeatmapFactory.Note(0f, 1, 2, 0, 1, 0);
                PlaceUtils.PlaceNote(notePlacement, noteA);
                
                CheckUtils.CheckV3Object("Check note object version", notesContainer, 0);
                CheckUtils.CheckNote("Check note attributes", notesContainer, 0, 0f, 0, 1, 2, 1, 0);
            }
        }

        [Test]
        public void PlaceV3Wall()
        {
            Assert.IsInstanceOf<V3Obstacle>(BeatmapFactory.Obstacle(), "Factory default does not instantiate v3 wall in beatmap v3");
            Assert.IsInstanceOf<V3Obstacle>(BeatmapFactory.Obstacle(0f, 1, 0, 0, 1f, 1, 5), "Factory does not instantiate v3 wall in beatmap v3");
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
            
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (collection is ObstacleGridContainer obstaclesContainer)
            {
                Transform root = obstaclesContainer.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                BaseObstacle wallA = BeatmapFactory.Obstacle(0f, 1, 0, 0, 1f, 1, 5);
                PlaceUtils.PlaceWall(wallPlacement, wallA);
                
                CheckUtils.CheckV3Object("Check wall object version", obstaclesContainer, 0);
                CheckUtils.CheckWall("Check wall attributes", obstaclesContainer, 0, 0f, 1, 0, 1f, 1, 5, 0);
            }
        }
        
        [Test]
        public void PlaceV3Event()
        {
            Assert.IsInstanceOf<V3BasicEvent>(BeatmapFactory.Event(), "Factory default does not instantiate v3 event in beatmap v3");
            Assert.IsInstanceOf<V3BasicEvent>(BeatmapFactory.Event(0f, 0, 1, 1f), "Factory does not instantiate v3 event in beatmap v3");
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
            
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is EventGridContainer eventsContainer)
            {
                Transform root = eventsContainer.transform.root;
                EventPlacement eventPlacement = root.GetComponentInChildren<EventPlacement>();
                eventPlacement.RefreshVisuals();

                BaseEvent eventA = BeatmapFactory.Event(2.5f, 1, 2, 0);
                PlaceUtils.PlaceEvent(eventPlacement, eventA);
                
                CheckUtils.CheckV3Object("Check note object version", eventsContainer, 0);
                CheckUtils.CheckEvent("Check note attributes", eventsContainer, 0, 2.5f, 0, 1, 1f);
            }
        }
    }
}
