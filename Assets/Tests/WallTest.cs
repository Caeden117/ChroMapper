using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
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
        public void EnsureWallIntegrity()
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (collection is ObstacleGridContainer obstaclesContainer)
            {
                var root = obstaclesContainer.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                wallPlacement.RefreshVisuals();

                var wallA = new V2Obstacle(0f, 1, 0, 1f, 1);
                PlaceUtils.PlaceWall(wallPlacement, wallA);

                CheckUtils.CheckWall("Check v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

                wallA.Type = 0;
                CheckUtils.CheckWall("Check type 0 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

                wallA.Type = 1;
                CheckUtils.CheckWall("Check type 1 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 2, 1, 1f, 1, 3);

                wallA.Type = 2;
                CheckUtils.CheckWall("Check type 2 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 2, 1f, 1, 5);

                wallA.Type = 5436;
                CheckUtils.CheckWall("Check arbitrary type v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 5436, 1f, 1, 5);

                // i generally dont expect user to do this, but i know it will happen
                wallA.Height = 3;
                CheckUtils.CheckWall("Height 3 make crouch height wall for v2 wall", obstaclesContainer, 0, 0f, 1, 2, 1, 1f, 1, 3);
                
                wallA.Height = 5;
                CheckUtils.CheckWall("Height 5 make full height wall for v2 wall", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

                wallA.Height = 4;
                CheckUtils.CheckWall("Invalid height should revert back to full height wall for v2 wall", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

                wallA.PosY = 0;
                CheckUtils.CheckWall("Pos Y 0 make wall full wall for v2 wall", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

                wallA.PosY = 2;
                CheckUtils.CheckWall("Pos Y 2 make wall crouch wall for v2 wall", obstaclesContainer, 0, 0f, 1, 2, 1, 1f, 1, 3);
                
                wallA.PosY = 1;
                CheckUtils.CheckWall("Invalid pos Y should revert back to full height wall for v2 wall", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);
                
                // test v3 wall
                var wallB = new V3Obstacle(1f, 1, 0, 1f, 1, 5);
                PlaceUtils.PlaceWall(wallPlacement, wallB);

                CheckUtils.CheckWall("Check v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

                wallB.Type = 0;
                CheckUtils.CheckWall("Check type 0 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

                wallB.Type = 1;
                CheckUtils.CheckWall("Check type 1 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 3);

                wallB.Type = 2;
                CheckUtils.CheckWall("Check type 2 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

                wallB.Type = 5436;
                CheckUtils.CheckWall("Check arbitrary type v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

                wallB.Height = 3;
                CheckUtils.CheckWall("Height 3 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 3);
                
                wallB.Height = 5;
                CheckUtils.CheckWall("Height 5 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

                wallB.Height = 4;
                CheckUtils.CheckWall("Arbitrary height should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 4);

                wallB.PosY = 0;
                CheckUtils.CheckWall("Pos Y 0 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 4);

                wallB.PosY = 2;
                CheckUtils.CheckWall("Pos Y 2 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 2, 0, 1f, 1, 4);
                
                wallB.PosY = 1;
                CheckUtils.CheckWall("Arbitrary pos Y should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 1, 0, 1f, 1, 4);
            }
        }

        [Test]
        public void HyperWall()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Obstacle);
            if (collection is ObstacleGridContainer obstaclesCollection)
            {
                var root = obstaclesCollection.transform.root;
                var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
                var inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
                wallPlacement.RefreshVisuals();

                BaseObstacle wallA = new V2Obstacle(2, (int)GridX.Left, (int)ObstacleType.Full, 2, 1);
                PlaceUtils.PlaceWall(wallPlacement, wallA);

                if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                    inputController.ToggleHyperWall(container);

                var toDelete = obstaclesCollection.LoadedObjects.First();
                obstaclesCollection.DeleteObject(toDelete);

                Assert.AreEqual(0, obstaclesCollection.LoadedObjects.Count);

                actionContainer.Undo();

                Assert.AreEqual(1, obstaclesCollection.LoadedObjects.Count);
                CheckUtils.CheckWall("Perform hyper wall", obstaclesCollection, 0, 4, (int)GridX.Left, 0,
                    (int)ObstacleType.Full, -2.0f, 1, 5);

                actionContainer.Undo();
                CheckUtils.CheckWall("Undo hyper wall", obstaclesCollection, 0, 2, (int)GridX.Left, 0,
                    (int)ObstacleType.Full, 2.0f, 1, 5);
            }
        }
    }
}