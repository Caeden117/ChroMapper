using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class CountersPlusTest
    {
        private const float delta = 0.001f;

        private CountersPlusController countersPlusController;
        private NJSEventGridContainer njsEventGridContainer;
        private AudioTimeSyncController atsc;
        
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [SetUp]
        public void EnableCountersPlus()
        {
            Settings.Instance.CountersPlus["enabled"] = true;
            countersPlusController = Object.FindObjectOfType<CountersPlusController>();
            njsEventGridContainer = BeatmapObjectContainerCollection.GetCollectionForType<NJSEventGridContainer>(ObjectType.NJSEvent);
            atsc = Object.FindObjectOfType<AudioTimeSyncController>();
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            TestUtils.ReturnSettings();
        }

        [UnityTearDown]
        public IEnumerator ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupNJSEvents();
            countersPlusController.UpdateStatistic(CountersPlusStatistic.NJSEvents);
            yield return null;
        }

        [Test]
        public void NJSEventsStats_InitialState()
        {
            Assert.AreEqual(10f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }

        [UnityTest]
        public IEnumerator NJSEventsStats_CursorBeforeNJSEvent()
        {
            atsc.MoveToJsonTime(7.5f);
            
            njsEventGridContainer.SpawnObject(new BaseNJSEvent
            {
                JsonTime = 10,
                RelativeNJS = 10 // 20 NJS
            });
            yield return null;
            
            // 75% of the way from 10 to 20 -> NJS and JD increases
            Assert.AreEqual(17.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(42f, countersPlusController.CurrentJD, delta);
            
            njsEventGridContainer.MapObjects[0].Easing = 1; // Edit easing to Quad in
            njsEventGridContainer.UpdateHJDLine(); // Trigger manually since we're just editing the data without an action
            yield return null;
            
            // 75% of the way after easing is 56.25%
            Assert.AreEqual(15.625f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(37.5f, countersPlusController.CurrentJD, delta);
            
            njsEventGridContainer.MapObjects[0].Easing = 0; // Back to linear
            njsEventGridContainer.MapObjects[0].RelativeNJS = -8; // 2 NJS
            njsEventGridContainer.UpdateHJDLine();
            yield return null;
            
            // 70% of the way form 10 to 2 -> NJS decreases while HJD and RT increases
            Assert.AreEqual(4f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(5f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(3000f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }
        
        [UnityTest]
        public IEnumerator NJSEventsStats_CursorAfterNJSEvent()
        {
            atsc.MoveToJsonTime(7.5f);
            
            njsEventGridContainer.SpawnObject(new BaseNJSEvent
            {
                JsonTime = 0,
                RelativeNJS = 10 // 20 NJS
            });
            yield return null;
            
            // Doubled NJS and JD
            Assert.AreEqual(20f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(48f, countersPlusController.CurrentJD, delta);
            
            njsEventGridContainer.MapObjects[0].RelativeNJS = -5; // 5 NJS
            njsEventGridContainer.UpdateHJDLine(); // Trigger manually since we're just editing the data without an action
            yield return null;
            
            // Halved NJS and Doubled HJD and RT
            Assert.AreEqual(5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(4f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(2400f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
        }
        
        [UnityTest]
        public IEnumerator NJSEventsStats_CursorBetweenNJSEvents()
        {
            njsEventGridContainer.SpawnObject(new BaseNJSEvent
            {
                JsonTime = 0,
                RelativeNJS = -5 // 5 NJS
            });
            njsEventGridContainer.SpawnObject(new BaseNJSEvent
            {
                JsonTime = 10,
                RelativeNJS = 5 // 15 NJS
            });
            
            atsc.MoveToJsonTime(2.5f);
            yield return null;
            
            // Halved between first njs event and base njs 
            Assert.AreEqual(7.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2.666f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1600f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(24f, countersPlusController.CurrentJD, delta);
            
            atsc.MoveToJsonTime(7.5f);
            yield return null;

            // Halfway between base njs and second njs event
            Assert.AreEqual(12.5f, countersPlusController.CurrentNJS, delta);
            Assert.AreEqual(2f, countersPlusController.CurrentHJD, delta);
            Assert.AreEqual(1200f, countersPlusController.CurrentRT, delta);
            Assert.AreEqual(30f, countersPlusController.CurrentJD, delta);
        }
    }
}