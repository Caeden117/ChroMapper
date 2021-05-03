using NUnit.Framework;
using System.Collections;
using System.Linq;
using SimpleJSON;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class WallTest
    {
        [UnityOneTimeSetUp]
        public IEnumerator LoadMap() => TestUtils.LoadMapper();

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            TestUtils.CleanupObstacles();
        }

        public static void CheckWall(BeatmapObjectContainerCollection container, int idx, int time, int lineIndex, int type, int duration, int width, JSONNode customData = null)
        {
            var newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BeatmapObstacle>(newObjA);
            if (newObjA is BeatmapObstacle newNoteA)
            {
                Assert.AreEqual(time, newNoteA._time);
                Assert.AreEqual(type, newNoteA._type);
                Assert.AreEqual(lineIndex, newNoteA._lineIndex);
                Assert.AreEqual(duration, newNoteA._duration);
                Assert.AreEqual(width, newNoteA._width);
                
                if (customData != null) Assert.AreEqual(customData.ToString(), newNoteA._customData.ToString());
            }
        }
        
        [Test]
        public void HyperWall()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.OBSTACLE);
            if (collection is ObstaclesContainer obstaclesCollection)
            {
                var root = obstaclesCollection.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
                wallPlacement.RefreshVisuals();
                
                var wallA = new BeatmapObstacle(2, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapObstacle.VALUE_FULL_BARRIER, 2, 1);
                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData._time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData._duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                if (obstaclesCollection.LoadedContainers[wallA] is BeatmapObstacleContainer container)
                {
                    inputController.ToggleHyperWall(container);
                }

                var toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);
                
                actionContainer.Undo();
                
                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckWall(obstaclesCollection, 0, 4, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapObstacle.VALUE_FULL_BARRIER, -2, 1);

                actionContainer.Undo();
                CheckWall(obstaclesCollection, 0, 2, BeatmapNote.LINE_INDEX_FAR_LEFT, BeatmapObstacle.VALUE_FULL_BARRIER, 2, 1);
            }
        }
    }
}
