using System;
using System.Collections;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine.TestTools;

namespace Tests
{
    public class BeatmapV2Test
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(2);
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
        public void IsV2Map()
        {
            Assert.IsInstanceOf<V2Difficulty>(BeatSaberSongContainer.Instance.Map,
                "Beatmap instance version should be v2");
            Assert.IsFalse(Settings.Instance.MapVersion == 3,
                "Settings Load Beatmap V3 should be false, otherwise may cause unnecessary issue");
        }

        [Test]
        public void PlaceEvent()
        {
            Assert.IsInstanceOf<V2Event>(BeatmapFactory.Event(),
                "Factory default does not instantiate v2 event in beatmap v2");
            Assert.IsInstanceOf<V2Event>(BeatmapFactory.Event(0f, 0, 1),
                "Factory does not instantiate v2 event in beatmap v2");
            Assert.DoesNotThrow(() => BeatmapFactory.Event(new JSONObject
            {
                ["_time"] = 0f,
                ["_type"] = 0,
                ["_value"] = 1,
                ["_floatValue"] = 1f,
                ["_customData"] = new JSONObject()
            }), "Factory could not instantiate event with compatible JSON schema in beatmap v2");
            Assert.Throws<ArgumentException>(() => BeatmapFactory.Event(new JSONObject
            {
                ["b"] = 0f,
                ["et"] = 0,
                ["i"] = 1,
                ["f"] = 1f,
                ["customData"] = new JSONObject()
            }), "Factory should throw error instantiating event with incompatible JSON schema in beatmap v2");

            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
            if (collection is EventGridContainer eventsContainer)
            {
                var root = eventsContainer.transform.root;
                var eventPlacement = root.GetComponentInChildren<EventPlacement>();
                eventPlacement.RefreshVisuals();

                var eventA = BeatmapFactory.Event(2.5f, 1, 2, 0);
                PlaceUtils.PlaceEvent(eventPlacement, eventA);

                CheckUtils.CheckV2Object("Check note object version", eventsContainer, 0);
                CheckUtils.CheckEvent("Check note attributes", eventsContainer, 0, 2.5f, 0, 1);
            }
        }
    }
}