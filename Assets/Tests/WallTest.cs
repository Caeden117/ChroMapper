using NUnit.Framework;
using SimpleJSON;
using System.Collections;
using System.Linq;
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
            BeatmapObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapObstacle>(newObjA);
            if (newObjA is BeatmapObstacle newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time);
                Assert.AreEqual(type, newNoteA.Type);
                Assert.AreEqual(lineIndex, newNoteA.LineIndex);
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
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.ObjectType.Obstacle);
            if (collection is ObstaclesContainer obstaclesCollection)
            {
                Transform root = obstaclesCollection.transform.root;
                ObstaclePlacement wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                BeatmapObstacleInputController inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
                wallPlacement.RefreshVisuals();

                BeatmapObstacle wallA = new BeatmapObstacle(2, BeatmapNote.LineIndexFarLeft, BeatmapObstacle.ValueFullBarrier, 2, 1);
                wallPlacement.QueuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.QueuedData.Time;
                wallPlacement.InstantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.QueuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                if (obstaclesCollection.LoadedContainers[wallA] is BeatmapObstacleContainer container)
                {
                    inputController.ToggleHyperWall(container);
                }

                BeatmapObject toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);

                actionContainer.Undo();

                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckWall(obstaclesCollection, 0, 4, BeatmapNote.LineIndexFarLeft, BeatmapObstacle.ValueFullBarrier, -2, 1);

                actionContainer.Undo();
                CheckWall(obstaclesCollection, 0, 2, BeatmapNote.LineIndexFarLeft, BeatmapObstacle.ValueFullBarrier, 2, 1);
            }
        }
    }
}
