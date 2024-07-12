using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class RotationEventTest
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
            CleanupUtils.CleanupEvents();
        }

        [Test]
        [TestCase(new[] { 15, 30, 60 })]
        [TestCase(new[] { 3, 2, 1 })]
        [TestCase(new[] { 0, 15, -10 })]
        public void RotationCallbackProperties(int[] rotations)
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            var rotationEventA = new V3RotationEvent(1, (int)ExecutionTime.Late, rotations[0]);
            var rotationEventB = new V3RotationEvent(2, (int)ExecutionTime.Late, rotations[1]);
            var rotationEventC = new V3RotationEvent(3, (int)ExecutionTime.Late, rotations[2]);
            eventsContainer.SpawnObject(rotationEventA);
            eventsContainer.SpawnObject(rotationEventB);
            eventsContainer.SpawnObject(rotationEventC);

            var rotationController = Object.FindObjectOfType<RotationCallbackController>();
            var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

            // Rotations should add up
            atsc.MoveToJsonTime(0);
            Assert.AreSame(null, rotationController.LatestRotationEvent);
            Assert.AreEqual(0, rotationController.Rotation);

            atsc.MoveToJsonTime(1.5f);
            Assert.AreSame(rotationEventA, rotationController.LatestRotationEvent);
            Assert.AreEqual(rotations[0], rotationController.Rotation);

            atsc.MoveToJsonTime(2.5f);
            Assert.AreSame(rotationEventB, rotationController.LatestRotationEvent);
            Assert.AreEqual(rotations[0] + rotations[1], rotationController.Rotation);

            atsc.MoveToJsonTime(3.5f);
            Assert.AreSame(rotationEventC, rotationController.LatestRotationEvent);
            Assert.AreEqual(rotations[0] + rotations[1] + rotations[2], rotationController.Rotation);
        }
    }
}