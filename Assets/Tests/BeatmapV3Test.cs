using System;
using System.Collections;
using Beatmap.Base;
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
            Assert.IsTrue(Settings.Instance.MapVersion == 3,
                "Settings Load Beatmap V3 should be true, otherwise may cause unnecessary issue");
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