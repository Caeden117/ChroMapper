using System.Collections;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V2;
using Beatmap.V3;
using NUnit.Framework;
using SimpleJSON;
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
            var obstaclesContainer = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);
            
            var root = obstaclesContainer.transform.root;
            var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
            wallPlacement.RefreshVisuals();

            var wallA = new BaseObstacle
            {
                JsonTime = 0f,
                PosX = 1,
                Type = 0,
                Duration = 1f,
                Width = 1
            };
            PlaceUtils.PlaceWall(wallPlacement, wallA);

            CheckUtils.CheckWall("Check v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

            wallA.Type = 0;
            CheckUtils.CheckWall("Check type 0 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 0, 1f, 1, 5);

            wallA.Type = 1;
            CheckUtils.CheckWall("Check type 1 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 2, 1, 1f, 1, 3);

            // wallA.Type = 2;
            // CheckUtils.CheckWall("Check type 2 v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 2, 1f, 1, 5);

            wallA.Type = 5436;
            CheckUtils.CheckWall("Check arbitrary type v2 wall attributes", obstaclesContainer, 0, 0f, 1, 0, 5436, 1f, 1, 5);

            // test v3 wall
            var wallB = new BaseObstacle
            {
                JsonTime = 1f,
                PosX = 1,
                PosY = 0,
                Duration = 1f,
                Width = 1,
                Height = 5
            };
            PlaceUtils.PlaceWall(wallPlacement, wallB);

            CheckUtils.CheckWall("Check v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

            wallB.Type = 0;
            CheckUtils.CheckWall("Check type 0 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);

            wallB.Type = 1;
            CheckUtils.CheckWall("Check type 1 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 3);

            // wallB.Type = 2;
            // CheckUtils.CheckWall("Check type 2 v3 wall attributes", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 5);
            
            wallB.Height = 3;
            CheckUtils.CheckWall("Height 3 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 3);

            wallB.Height = 5;
            CheckUtils.CheckWall("Height 5 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 5);

            wallB.Height = 4;
            CheckUtils.CheckWall("Height 4 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 4);

            wallB.PosY = 2;
            CheckUtils.CheckWall("Pos Y 2 should change Type to crouch for v3 wall", obstaclesContainer, 1, 1f, 1, 2, 1, 1f, 1, 4);

            wallB.PosY = 0;
            CheckUtils.CheckWall("Pos Y 0 should change Type to full for v3 wall", obstaclesContainer, 1, 1f, 1, 0, 0, 1f, 1, 4);

            wallB.PosY = 1;
            CheckUtils.CheckWall("Pos Y 1 should change nothing else for v3 wall", obstaclesContainer, 1, 1f, 1, 1, 0, 1f, 1, 4);
            
        }

        [Test]
        public void HyperWall()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var obstaclesCollection = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);
            
            var root = obstaclesCollection.transform.root;
            var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
            var inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
            wallPlacement.RefreshVisuals();

            BaseObstacle wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            PlaceUtils.PlaceWall(wallPlacement, wallA);

            if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                inputController.ToggleHyperWall(container);

            var toDelete = obstaclesCollection.MapObjects.First();
            obstaclesCollection.DeleteObject(toDelete);

            Assert.AreEqual(0, obstaclesCollection.MapObjects.Count);

            actionContainer.Undo();

            Assert.AreEqual(1, obstaclesCollection.MapObjects.Count);
            CheckUtils.CheckWall("Perform hyper wall", obstaclesCollection, 0, 4, (int)GridX.Left, 0,
                (int)ObstacleType.Full, -2.0f, 1, 5);

            actionContainer.Undo();
            CheckUtils.CheckWall("Undo hyper wall", obstaclesCollection, 0, 2, (int)GridX.Left, 0,
                (int)ObstacleType.Full, 2.0f, 1, 5);
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            Settings.Instance.MapVersion = 2;
            
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var obstaclesCollection = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var root = obstaclesCollection.transform.root;
            var wallPlacement = root.GetComponentInChildren<ObstaclePlacement>();
            wallPlacement.RefreshVisuals();

            var customCoord = new JSONArray() { [0] = 0, [1] = 1 };
            var customSize = new JSONArray() { [0] = 0, [1] = null, [2] = 420 };

            BaseObstacle wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            wallA.CustomCoordinate = customCoord;
            wallA.CustomSize = customSize;
            PlaceUtils.PlaceWall(wallPlacement, wallA);

            CheckUtils.CheckWall("Applies CustomProperties to CustomData", obstaclesCollection, 0, 2, (int)GridX.Left, 0,
                (int)ObstacleType.Full, 2.0f, 1, 5,
                new JSONObject() { ["_position"] = customCoord, ["_scale"] = customSize });
        }
    }
}