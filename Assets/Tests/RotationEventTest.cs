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

            var rotationEventA = new BaseEvent { JsonTime = 1, Type = (int)EventTypeValue.LateLaneRotation, FloatValue = rotations[0] };
            var rotationEventB = new BaseEvent { JsonTime = 2, Type = (int)EventTypeValue.LateLaneRotation, FloatValue = rotations[1] };
            var rotationEventC = new BaseEvent { JsonTime = 3, Type = (int)EventTypeValue.LateLaneRotation, FloatValue = rotations[2] };
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

        [Test]
        public void RotationCallbackPropertiesOnTimeMatch()
        {
            var eventsContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);

            const int rotation = 15;
            const float timeA = 1f;
            const float timeB = 2f;
            var rotationEventA = new BaseEvent { JsonTime = timeA, Type = (int)EventTypeValue.LateLaneRotation, FloatValue = rotation };
            var rotationEventB = new BaseEvent { JsonTime = timeB, Type = (int)EventTypeValue.LateLaneRotation, FloatValue = rotation };
            eventsContainer.SpawnObject(rotationEventA);
            eventsContainer.SpawnObject(rotationEventB);

            var rotationController = Object.FindObjectOfType<RotationCallbackController>();
            var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

            // Should ignore events on same time
            atsc.MoveToJsonTime(timeA);
            Assert.AreSame(null, rotationController.LatestRotationEvent);
            Assert.AreEqual(0, rotationController.Rotation);

            atsc.MoveToJsonTime(timeB);
            Assert.AreSame(rotationEventA, rotationController.LatestRotationEvent);
            Assert.AreEqual(rotation, rotationController.Rotation);
        }
    }
}