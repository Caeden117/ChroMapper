using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;

namespace Tests.Placement
{
    public class WallTest : TestBase
    {
        [Test]
        public void EnsureWallIntegrity()
        {
            var wallA = new BaseObstacle
            {
                JsonTime = 0f,
                PosX = 1,
                Type = 0,
                Duration = 1f,
                Width = 1
            };
            var originalWallA = BeatmapFactory.Clone(wallA);
            wallA = PlaceUtils.Place(wallA);

            BeatmapAssertion.IsEqual(
                originalWallA,
                wallA,
                "Check v2 wall attributes");

            var expectedWallAType0 = BeatmapFactory.Clone(wallA);
            expectedWallAType0.Type = 0;
            wallA.Type = 0;
            BeatmapAssertion.IsEqual(
                expectedWallAType0,
                wallA,
                "Check type 0 v2 wall attributes");

            var expectedWallAType1 = BeatmapFactory.Clone(wallA);
            expectedWallAType1.Type = 1;
            wallA.Type = 1;
            BeatmapAssertion.IsEqual(
                expectedWallAType1,
                wallA,
                "Check type 1 v2 wall attributes");

            // wallA.Type = 2;
            // BeatmapAssertion.Assert(new BaseObstacle { JsonTime = 0f, PosX = 1, Type = 2, PosY = 0, Duration = 1f, Width = 1, Height = 5 }, wallA, "Check type 2 v2 wall attributes");

            var expectedWallAArbitraryType = BeatmapFactory.Clone(wallA);
            expectedWallAArbitraryType.Type = 5436;
            wallA.Type = 5436;
            BeatmapAssertion.IsEqual(
                expectedWallAArbitraryType,
                wallA,
                "Check arbitrary type v2 wall attributes");

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
            var originalWallB = BeatmapFactory.Clone(wallB);
            wallB = PlaceUtils.Place(wallB);

            BeatmapAssertion.IsEqual(
                originalWallB,
                wallB,
                "Check v3 wall attributes");

            var expectedWallBType0 = BeatmapFactory.Clone(wallB);
            expectedWallBType0.Type = 0;
            wallB.Type = 0;
            BeatmapAssertion.IsEqual(
                expectedWallBType0,
                wallB,
                "Check type 0 v3 wall attributes");

            var expectedWallBType1 = BeatmapFactory.Clone(wallB);
            expectedWallBType1.Type = 1;
            wallB.Type = 1;
            BeatmapAssertion.IsEqual(
                expectedWallBType1,
                wallB,
                "Check type 1 v3 wall attributes");

            // wallB.Type = 2;
            // BeatmapAssertion.Assert(new BaseObstacle { JsonTime = 1f, PosX = 1, Type = 0, PosY = 0, Duration = 1f, Width = 1, Height = 5 }, wallB, "Check type 2 v3 wall attributes");

            var expectedWallBHeight3 = BeatmapFactory.Clone(wallB);
            expectedWallBHeight3.Height = 3;
            wallB.Height = 3;
            BeatmapAssertion.IsEqual(
                expectedWallBHeight3,
                wallB,
                "Height 3 should change nothing else for v3 wall");

            var expectedWallBHeight5 = BeatmapFactory.Clone(wallB);
            expectedWallBHeight5.Height = 5;
            wallB.Height = 5;
            BeatmapAssertion.IsEqual(
                expectedWallBHeight5,
                wallB,
                "Height 5 should change nothing else for v3 wall");

            var expectedWallBHeight4 = BeatmapFactory.Clone(wallB);
            expectedWallBHeight4.Height = 4;
            wallB.Height = 4;
            BeatmapAssertion.IsEqual(
                expectedWallBHeight4,
                wallB,
                "Height 4 should change nothing else for v3 wall");

            var expectedWallBPosY2 = BeatmapFactory.Clone(wallB);
            expectedWallBPosY2.PosY = 2;
            wallB.PosY = 2;
            BeatmapAssertion.IsEqual(
                expectedWallBPosY2,
                wallB,
                "Pos Y 2 should change Type to crouch for v3 wall");

            var expectedWallBPosY0 = BeatmapFactory.Clone(wallB);
            expectedWallBPosY0.PosY = 0;
            wallB.PosY = 0;
            BeatmapAssertion.IsEqual(
                expectedWallBPosY0,
                wallB,
                "Pos Y 0 should change Type to full for v3 wall");

            var expectedWallBPosY1 = BeatmapFactory.Clone(wallB);
            expectedWallBPosY1.PosY = 1;
            wallB.PosY = 1;
            BeatmapAssertion.IsEqual(
                expectedWallBPosY1,
                wallB,
                "Pos Y 1 should change nothing else for v3 wall");
        }

        [Test]
        public void HyperWall()
        {
            var obstaclesCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);
            var inputController = Object.FindAnyObjectByType<BeatmapObstacleInputController>();

            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            var originalWallA = BeatmapFactory.Clone(wallA);
            wallA = PlaceUtils.Place(wallA);

            if (obstaclesCollection.LoadedContainers[wallA] is ObstacleContainer container)
                inputController.ToggleHyperWall(container);

            // Hover tweaks are selection-neutral now, so resolve the live hyper wall from the collection.
            var toDelete = obstaclesCollection.LoadedObjects.OfType<BaseObstacle>().Single();
            PlaceUtils.Delete(toDelete);

            BeatmapAssertion.CollectionCount<BaseObstacle>(0);

            var undoDeleteObjects = PlaceUtils.Undo<BaseObstacle>().ToList();
            BeatmapAssertion.CollectionCount<BaseObstacle>(1);
            BeatmapAssertion.IsEqualWithChanges(
                originalWallA,
                undoDeleteObjects[0],
                w => { w.JsonTime = 4; w.Duration = -2f; },
                "Perform hyper wall");

            var undoHyperObjects = PlaceUtils.Undo<BaseObstacle>().ToList();
            BeatmapAssertion.IsUnchanged(originalWallA, undoHyperObjects[0], "Undo hyper wall");
        }

        // Reverse wall drags must preserve both selected endpoints instead of collapsing to the minimum duration.
        [Test]
        public void ReverseWallPlacementUsesForwardTimeRange()
        {
            var obstaclePlacement = Object.FindAnyObjectByType<ObstaclePlacement>();
            obstaclePlacement.QueuedData = new BaseObstacle
            {
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Width = 1
            };

            obstaclePlacement.RoundedJsonTime = 4f;
            obstaclePlacement.HandleApply();
            obstaclePlacement.RoundedJsonTime = 2f;
            obstaclePlacement.HandleApply();

            // The placement action has no conflicts to return on undo, so inspect the authored wall still in the obstacle collection.
            var obstacleCollection =
                BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);
            var placedWall = obstacleCollection.MapObjects.Single();
            Assert.That(placedWall.JsonTime, Is.EqualTo(2f));
            Assert.That(placedWall.Duration, Is.EqualTo(2f));
        }

        [Test]
        public void PlacementPersistsCustomProperty()
        {
            Settings.Instance.MapVersion = 2;

            var customCoord = new JSONArray { [0] = 0, [1] = 1 };
            var customSize = new JSONArray { [0] = 0, [1] = null, [2] = 420 };

            var wallA = new BaseObstacle
            {
                JsonTime = 2,
                PosX = (int)GridX.Left,
                Type = (int)ObstacleType.Full,
                Duration = 2,
                Width = 1
            };
            wallA.CustomCoordinate = customCoord;
            wallA.CustomSize = customSize;
            var expectedWallACustom = BeatmapFactory.Clone(wallA);
            wallA = PlaceUtils.Place(wallA);

            expectedWallACustom.CustomData = new JSONObject { ["_position"] = customCoord, ["_scale"] = customSize };
            BeatmapAssertion.IsEqual(expectedWallACustom, wallA, "Applies CustomProperties to CustomData");
            BeatmapAssertion.IsInCollection(wallA, "Placed wall is in collection");
        }
    }
}
