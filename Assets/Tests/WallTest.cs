using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using Beatmap.V2;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class WallTest
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
            TestUtils.CleanupObstacles();
        }

        public static void CheckWall(BeatmapObjectContainerCollection container, int idx, int time, int lineIndex, int type, int duration, int width, JSONNode customData = null)
        {
            IObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<IObstacle>(newObjA);
            if (newObjA is IObstacle newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(lineIndex, newNoteA.PosX);
                Assert.AreEqual(duration, newNoteA.Duration);
                Assert.AreEqual(width, newNoteA.Width);

                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.CustomData.ToString());
                }
            }
        }

        [Test]
        public void HyperWall()
        {
            BeatmapActionContainer actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (collection is ObstacleGridContainer obstaclesCollection)
            {
                Transform root = obstaclesCollection.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                BeatmapObstacleInputController inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
                wallPlacement.RefreshVisuals();

                IObstacle wallA = new V2Obstacle(2, (int)GridX.Left, (int)ObstacleType.Full, 2, 1);
                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData.Time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                {
                    inputController.ToggleHyperWall(container);
                }

                IObject toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);

                actionContainer.Undo();

                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckWall(obstaclesCollection, 0, 4, (int)GridX.Left, (int)ObstacleType.Full, -2, 1);

                actionContainer.Undo();
                CheckWall(obstaclesCollection, 0, 2, (int)GridX.Left, (int)ObstacleType.Full, 2, 1);
            }
        }
    }
}
