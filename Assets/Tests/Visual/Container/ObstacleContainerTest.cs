using System.Collections;
using System.Reflection;
using Beatmap.Base;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Editor;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Placement
{
    public class ObstacleContainerTest : PreviewWorkflowTestBase
    {
        private static readonly PropertyInfo playbackSeconds = typeof(AudioTimeSyncController)
            .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private ObstacleGridContainer _obstaclesCollection;
        private BaseObstacle _placedObstacle;
        private AudioTimeSyncController playbackClock;
        private bool previousClockEnabled;

        [SetUp]
        public void SetUp()
        {
            _obstaclesCollection = BeatmapObjectContainerCollection.GetCollectionForType<ObstacleGridContainer>(ObjectType.Obstacle);

            _placedObstacle = new BaseObstacle
            {
                JsonTime = 0,
                Duration = 2,
                PosX = 0,
                PosY = 0,
                Height = 5
            };
            _placedObstacle = PlaceUtils.Place(_placedObstacle);
        }

        [UnityTest]
        public IEnumerator PlaybackAfterSeekRecyclesWallsWithDifferentDurations()
        {
            var halfJump = Object.FindAnyObjectByType<VariableNJSProvider>().MaxHalfJumpDurationInBeats;
            _placedObstacle.SetSpawnParameters(halfJump, _placedObstacle.HalfJumpDistance);
            var firstLongWall = PlaceWall(1f, 14f, halfJump);
            var secondLongWall = PlaceWall(2f, 16f, halfJump);
            PlaceWall(30f, 1f, halfJump);
            PreparePreview(halfJump + 6f);
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(firstLongWall), Is.True,
                "The paused seek must load the wall before checking its playback despawn.");
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(secondLongWall), Is.True);

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(playbackClock, playbackClock.CurrentSongBpmTime);
            var despawnTime = firstLongWall.SongBpmTime + firstLongWall.DurationSongBpmTime + halfJump;
            playbackSeconds.SetValue(playbackClock, playbackClock.GetSecondsFromBeat(despawnTime - 0.25f));
            yield return null;
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(firstLongWall), Is.True,
                "The wall must remain loaded until its own despawn time.");

            playbackSeconds.SetValue(playbackClock, playbackClock.GetSecondsFromBeat(despawnTime + 0.25f));
            yield return null;
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(firstLongWall), Is.False,
                "Resuming after a seek skipped the first live wall's despawn when wall durations differed.");
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(secondLongWall), Is.True,
                "Recycling the first wall must not remove its longer-lived neighbor.");
        }

        [UnityTest]
        public IEnumerator PlaybackAfterSeekSpawnsWallsWithDifferentJumpOffsets()
        {
            var halfJump = Object.FindAnyObjectByType<VariableNJSProvider>().MaxHalfJumpDurationInBeats;
            _placedObstacle.CustomData = new JSONObject
            {
                [_placedObstacle.CustomKeyNoteJumpStartBeatOffset] = 20f
            };
            var nextWall = PlaceWall(halfJump + 5f, 10f, halfJump);
            PlaceWall(halfJump + 6f, 10f, halfJump);
            PlaceWall(halfJump + 30f, 1f, halfJump);
            PreparePreview(4f);
            Assert.That(_placedObstacle.HalfJumpDuration, Is.GreaterThan(halfJump + 10f),
                "The earlier wall must retain its authored long jump offset throughout preview setup.");
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(nextWall), Is.False,
                "The paused seek must precede the wall's normal spawn window.");

            TestUtils.StartDeterministicPlaybackAtSongBpmTime(playbackClock, 4f);
            yield return null;
            Assert.That(_obstaclesCollection.LoadedContainers.ContainsKey(nextWall), Is.True,
                "Resuming after a seek skipped the next wall when an earlier wall had a longer jump offset.");
        }

        private BaseObstacle PlaceWall(float time, float duration, float halfJump)
        {
            var wall = PlaceUtils.Place(new BaseObstacle
            {
                JsonTime = time,
                Duration = duration,
                PosX = 0,
                PosY = 0,
                Width = 1,
                Height = 5
            });
            wall.SetSpawnParameters(halfJump, wall.HalfJumpDistance);
            return wall;
        }

        private void PreparePreview(float songBpmTime)
        {
            Settings.Instance.Animations = true;
            playbackClock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = playbackClock.enabled;
            playbackClock.enabled = false;
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            playbackClock.MoveToSongBpmTime(songBpmTime);
            _obstaclesCollection.RefreshPool(true);
        }

        protected override void BeforeCleanup() => RestorePlaybackClock();

        [UnityTearDown]
        public IEnumerator RestorePlayback()
        {
            RestorePlaybackClock();
            yield break;
        }

        private void RestorePlaybackClock()
        {
            if (playbackClock == null) return;
            if (playbackClock.IsPlaying) TestUtils.PauseDeterministicPlayback(playbackClock);
            playbackClock.enabled = previousClockEnabled;
            playbackClock = null;
        }

        private MeshRenderer GetObstacleRenderer() =>
            _obstaclesCollection.LoadedContainers[_placedObstacle].GetComponentInChildren<MeshRenderer>();

        [Test]
        public void UpdatesWhenEditorScaleUpdates()
        {
            Assert.IsTrue(
                _obstaclesCollection.LoadedContainers.TryGetValue(_placedObstacle, out var obstacleContainer),
                "Obstacle container not found");

            var obstacleRenderer = GetObstacleRenderer();

            // Increase scale
            const float EditorScaleMultiplier = 2;
            var originalEditorScale = Settings.Instance.EditorScale;
            var originalObstacleScale = obstacleRenderer.bounds.size;
            try
            {
                Settings.Instance.EditorScale *= EditorScaleMultiplier;
                Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
                var modifiedObstacleScale = obstacleRenderer.bounds.size;

                Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
                Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
                Assert.AreEqual(
                    EditorScaleMultiplier * originalObstacleScale.z,
                    modifiedObstacleScale.z,
                    0.02); // because 0.001 was too strict
            }
            finally
            {
                Settings.Instance.EditorScale = originalEditorScale;
                Settings.ManuallyNotifySettingUpdatedEvent("EditorScale", Settings.Instance.EditorScale);
            }
        }

        [Test]
        public void ScalesWithBpmEventsCorrectly()
        {
            Assert.IsTrue(
                _obstaclesCollection.LoadedContainers.TryGetValue(_placedObstacle, out var obstacleContainer),
                "Obstacle container not found");

            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 0, Bpm = 100 });
            var originalObstacleScale = GetObstacleRenderer().bounds.size;

            // Obstacle should now be 3/4 of its original length
            PlaceUtils.Place(new BaseBpmEvent { JsonTime = 1, Bpm = 200 });
            var modifiedObstacleScale = GetObstacleRenderer().bounds.size;

            Assert.AreEqual(originalObstacleScale.x, modifiedObstacleScale.x, 0.001);
            Assert.AreEqual(originalObstacleScale.y, modifiedObstacleScale.y, 0.001);
            Assert.AreEqual(3f / 4f * originalObstacleScale.z, modifiedObstacleScale.z, 0.02);
        }
    }
}
