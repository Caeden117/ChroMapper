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
            CleanupUtils.CleanupObstacles();
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
                PlaceUtils.PlaceWall(wallPlacement, wallA);

                if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                {
                    inputController.ToggleHyperWall(container);
                }

                BaseObject toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);

                actionContainer.Undo();

                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckUtils.CheckWall("Perform hyper wall", obstaclesCollection, 0, 4, (int)GridX.Left, 0, -2.0f, 1, 5, (int)ObstacleType.Full);

                actionContainer.Undo();
                CheckUtils.CheckWall("Undo hyper wall", obstaclesCollection, 0, 2, (int)GridX.Left, 0, 2.0f, 1, 5, (int)ObstacleType.Full);
            }
        }
    }
}
