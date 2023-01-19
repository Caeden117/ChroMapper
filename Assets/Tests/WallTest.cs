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

        public static void CheckWall(string msg, BeatmapObjectContainerCollection container, int idx, float time, int lineIndex, int lineLayer, float duration, int width, int height, int? type, JSONNode customData = null)
        {
            BaseObject newObjA = container.LoadedObjects.Skip(idx).First();
            Assert.IsInstanceOf<BaseObstacle>(newObjA);
            if (newObjA is BaseObstacle newNoteA)
            {
                Assert.AreEqual(time, newNoteA.Time, 0.001f, $"{msg}: Mismatched time");
                Assert.AreEqual(lineIndex, newNoteA.PosX, $"{msg}: Mismatched position X");
                Assert.AreEqual(lineLayer, newNoteA.PosY, $"{msg}: Mismatched position Y");
                Assert.AreEqual(duration, newNoteA.Duration, 0.001f, $"{msg}: Mismatched duration");
                Assert.AreEqual(width, newNoteA.Width, $"{msg}: Mismatched width");
                Assert.AreEqual(height, newNoteA.Height, $"{msg}: Mismatched height");
                
                if (type != null)
                {
                    Assert.AreEqual(type, newNoteA.Type, $"{msg}: Mismatched type");
                }

                if (customData != null)
                {
                    Assert.AreEqual(customData.ToString(), newNoteA.CustomData.ToString(), $"{msg}: Mismatched custom data");
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

                BaseObstacle wallA = new V2Obstacle(2, (int)GridX.Left, (int)ObstacleType.Full, 2, 1);
                wallPlacement.queuedData = wallA;
                wallPlacement.RoundedTime = wallPlacement.queuedData.Time;
                wallPlacement.instantiatedContainer.transform.localScale = new Vector3(0, 0, wallPlacement.queuedData.Duration * EditorScaleController.EditorScale);
                wallPlacement.ApplyToMap(); // Starts placement
                wallPlacement.ApplyToMap(); // Completes placement

                if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                {
                    inputController.ToggleHyperWall(container);
                }

                BaseObject toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);

                actionContainer.Undo();

                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckWall("Perform hyper wall", obstaclesCollection, 0, 4, (int)GridX.Left, 0, -2f, 1, 5, (int)ObstacleType.Full);

                actionContainer.Undo();
                CheckWall("Undo hyper wall", obstaclesCollection, 0, 2, (int)GridX.Left, 0, 2f, 1, 5, (int)ObstacleType.Full);
            }
        }
    }
}
