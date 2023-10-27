using System.Collections;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class ObstacleContainerTest
    {
        float originalEditorScale;
        ObstacleGridContainer obstaclesCollection;
        BaseObstacle placedObstacle;

        [UnityOneTimeSetUp]
        public IEnumerator LoadMap()
        {
            return TestUtils.LoadMap(3);
        }

        [OneTimeSetUp]
        public void SaveEditorScale()
        {
            originalEditorScale = Settings.Instance.EditorScale;
        }

        [OneTimeTearDown]
        public void FinalTearDown()
        {
            Settings.Instance.EditorScale = originalEditorScale;
            TestUtils.ReturnSettings();
        }

        [SetUp]
        public void PlaceWall()
        {
            obstaclesCollection = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();

            var root = obstaclesCollection.transform.root;
            var obstaclePlacement = root.GetComponentInChildren<ObstaclePlacement>();
            var inputController = root.GetComponentInChildren<BeatmapObstacleInputController>();
            obstaclePlacement.RefreshVisuals();

            placedObstacle = new V3Obstacle
            {
                JsonTime = 0,
                Duration = 2,
                PosX = 0,
                PosY = 0,
                Height = 5
            };
            PlaceUtils.PlaceWall(obstaclePlacement, placedObstacle);
        }

        [TearDown]
        public void ContainerCleanup()
        {
            BeatmapActionContainer.RemoveAllActionsOfType<BeatmapAction>();
            CleanupUtils.CleanupObstacles();
            CleanupUtils.CleanupBPMChanges();
        }


        [Test]
        public void UpdatesWhenEditorScaleUpdates()
        {
            if (!obstaclesCollection.LoadedContainers.TryGetValue(placedObstacle, out var obstacleContainer))
            {
                Assert.Fail("Obstacle container not found");
            }

            var obstacleRenderer = obstacleContainer.GetComponentInChildren<MeshRenderer>();

            // Increase scale
            const float EditorScaleMultiplier = 2;
            var originalObstacleScale = obstacleRenderer.bounds.size;
            Settings.Instance.EditorScale *= EditorScaleMultiplier;
            Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
            var modifiedObstacleScale = obstacleRenderer.bounds.size;

            Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
            Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
            Assert.AreEqual(EditorScaleMultiplier * originalObstacleScale.z, modifiedObstacleScale.z, 0.001);
        }

        [Test]
        public void ScalesWithBpmEventsCorrectly()
        {
            if (!obstaclesCollection.LoadedContainers.TryGetValue(placedObstacle, out var obstacleContainer))
            {
                Assert.Fail("Obstacle container not found");
            }

            var bpmCollection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);
            bpmCollection.SpawnObject(new V3BpmEvent { JsonTime = 0, Bpm = 100 });
            var obstacleRenderer = obstacleContainer.GetComponentInChildren<MeshRenderer>();
            var originalObstacleScale = obstacleRenderer.bounds.size;

            // Obstacle should now be 3/4 of its original length
            bpmCollection.SpawnObject(new V3BpmEvent { JsonTime = 1, Bpm = 200 });
            var modifiedObstacleScale = obstacleRenderer.bounds.size;

            Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
            Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
            Assert.AreEqual(3f / 4f * originalObstacleScale.z, modifiedObstacleScale.z, 0.001);
        }
    }
}