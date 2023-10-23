using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.V3;
using NUnit.Framework;
using Tests.Util;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class BPMTest
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
            CleanupUtils.CleanupBPMChanges();
        }

        private static void CheckBPM(string msg, BeatmapObjectContainerCollection container, int idx, float jsonTime, float bpm, float? songBpmTime = null)
        {
            var decimalPrecision = Settings.Instance.TimeValueDecimalPrecision;
            var delta = 1.5 * Mathf.Pow(10, -decimalPrecision);
            var obj = container.LoadedObjects.ElementAt(idx);
            Assert.IsInstanceOf<BaseBpmEvent>(obj);
            if (obj is BaseBpmEvent bpmEvent)
            {
                Assert.AreEqual(jsonTime, bpmEvent.JsonTime, delta, $"{msg}: Mismatched JsonTime");
                Assert.AreEqual(bpm, bpmEvent.Bpm, delta, $"{msg}: Mismatched BPM");
                if (songBpmTime != null) Assert.AreEqual(songBpmTime.Value, bpmEvent.SongBpmTime, delta, $"{msg}: Mismatched SongBpmTime");
            }
        }

        [Test]
        public void SongBpmTimes()
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
            if (collection is BPMChangeGridContainer bpmCollection)
            {
                var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                BaseBpmEvent baseBpmEvent = new V3BpmEvent(0, 111);
                bpmCollection.SpawnObject(baseBpmEvent);

                baseBpmEvent = new V3BpmEvent(1, 222);
                bpmCollection.SpawnObject(baseBpmEvent);

                baseBpmEvent = new V3BpmEvent(2, 333);
                bpmCollection.SpawnObject(baseBpmEvent);

                baseBpmEvent = new V3BpmEvent(3, 444);
                bpmCollection.SpawnObject(baseBpmEvent);

                Assert.AreEqual(4, bpmCollection.LoadedObjects.Count);
                CheckBPM("1st BPM values", bpmCollection, 0, 0, 111, 0);
                CheckBPM("2nd BPM values", bpmCollection, 1, 1, 222, songBpm / 111);
                CheckBPM("3rd BPM values", bpmCollection, 2, 2, 333, songBpm / 111 + songBpm / 222);
                CheckBPM("4th BPM values", bpmCollection, 3, 3, 444, songBpm / 111 + songBpm / 222 + songBpm / 333);

                baseBpmEvent = new V3BpmEvent(0, 1);
                bpmCollection.SpawnObject(baseBpmEvent);

                Assert.AreEqual(4, bpmCollection.LoadedObjects.Count);
                CheckBPM("1st BPM values after modified", bpmCollection, 0, 0, 1, 0);
                CheckBPM("2nd BPM values after modified", bpmCollection, 1, 1, 222, songBpm / 1);
                CheckBPM("3rd BPM values after modified", bpmCollection, 2, 2, 333, songBpm / 1 + songBpm / 222);
                CheckBPM("4th BPM values after modified", bpmCollection, 3, 3, 444, songBpm / 1 + songBpm / 222 + songBpm / 333);

                bpmCollection.DeleteObject(baseBpmEvent);

                Assert.AreEqual(3, bpmCollection.LoadedObjects.Count);
                CheckBPM("1st BPM values after delete", bpmCollection, 0, 1, 222, 1);
                CheckBPM("2nd BPM values after delete", bpmCollection, 1, 2, 333, 1 + songBpm / 222);
                CheckBPM("3rd BPM values after delete", bpmCollection, 2, 3, 444, 1 + songBpm / 222 + songBpm / 333);
            }
        }

        [Test]
        public void ModifyEvent()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
            if (collection is BPMChangeGridContainer bpmCollection)
            {
                BaseBpmEvent baseBpmEvent = new V3BpmEvent(20, 20);
                bpmCollection.SpawnObject(baseBpmEvent);

                baseBpmEvent = new V3BpmEvent(10, 10);
                bpmCollection.SpawnObject(baseBpmEvent);

                if (bpmCollection.LoadedContainers[baseBpmEvent] is BpmEventContainer container)
                    BeatmapBPMChangeInputController.ChangeBpm(container, "60");

                Assert.AreEqual(2, bpmCollection.LoadedObjects.Count);
                CheckBPM("Update BPM event", bpmCollection, 0, 10, 60);
                CheckBPM("Update future BPM event SongTime", bpmCollection, 1, 20, 20, 10 + 10 * (100f / 60));

                actionContainer.Undo();

                Assert.AreEqual(2, bpmCollection.LoadedObjects.Count);
                CheckBPM("Undo BPM event", bpmCollection, 0, 10, 10);
                CheckBPM("Undo future BPM event SongTime", bpmCollection, 1, 20, 20, 10 + 10 * (100f / 10));

                actionContainer.Redo();

                Assert.AreEqual(2, bpmCollection.LoadedObjects.Count);
                CheckBPM("Redo BPM event", bpmCollection, 0, 10, 60);
                CheckBPM("Redo future BPM event SongTime", bpmCollection, 1, 20, 20, 10 + 10 * (100f / 60));
            }
        }

        [Test]
        public void GoToBeat()
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.BpmChange);
            if (collection is BPMChangeGridContainer bpmCollection)
            {
                var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                BaseBpmEvent baseBpmEvent = new V3BpmEvent(0, 111);
                bpmCollection.SpawnObject(baseBpmEvent);

                baseBpmEvent = new V3BpmEvent(1, 222);
                bpmCollection.SpawnObject(baseBpmEvent);

                Assert.AreEqual(2, bpmCollection.LoadedObjects.Count);

                var atsc = Object.FindObjectOfType<AudioTimeSyncController>();

                atsc.GoToBeat("0");
                Assert.AreEqual(0, atsc.CurrentJsonTime, 0.001f);
                Assert.AreEqual(0, atsc.CurrentSongBpmTime, 0.001f);

                atsc.GoToBeat("0.5");
                Assert.AreEqual(0.5, atsc.CurrentJsonTime, 0.001f);
                Assert.AreEqual(0.5 * (songBpm / 111), atsc.CurrentSongBpmTime, 0.001f);

                atsc.GoToBeat("1.0");
                Assert.AreEqual(1.0, atsc.CurrentJsonTime, 0.001f);
                Assert.AreEqual(1.0 * (songBpm / 111), atsc.CurrentSongBpmTime, 0.001f);

                atsc.GoToBeat("1.5");
                Assert.AreEqual(1.5, atsc.CurrentJsonTime, 0.001f);
                Assert.AreEqual(1.0 * (songBpm / 111) + 0.5 * (songBpm / 222), atsc.CurrentSongBpmTime, 0.001f);

                atsc.GoToBeat("Invalid number");
                Assert.AreEqual(1.5, atsc.CurrentJsonTime, 0.001f);
                Assert.AreEqual(1.0 * (songBpm / 111) + 0.5 * (songBpm / 222), atsc.CurrentSongBpmTime, 0.001f);
            }
        }

        [Test]
        public void UndoActionCollection()
        {
            var actionContainer = Object.FindObjectOfType<BeatmapActionContainer>();
            var bpmCollection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType.BpmChange);

            var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            var baseBpmEvent0 = new V3BpmEvent(0, 111);
            bpmCollection.SpawnObject(baseBpmEvent0, out var conflicting0);

            var baseBpmEvent1 = new V3BpmEvent(1, 222);
            bpmCollection.SpawnObject(baseBpmEvent1, out var conflicting1);

            var baseBpmEvent2 = new V3BpmEvent(2, 333);
            bpmCollection.SpawnObject(baseBpmEvent2, out var conflicting2);

            BeatmapActionContainer.AddAction(new ActionCollectionAction(new List<BeatmapAction>{
                new BeatmapObjectPlacementAction(baseBpmEvent0, conflicting0, ""),
                new BeatmapObjectPlacementAction(baseBpmEvent1, conflicting1, ""),
                new BeatmapObjectPlacementAction(baseBpmEvent2, conflicting2, ""),
            }));

            // Check songBpm after placing
            Assert.AreEqual(3, bpmCollection.LoadedObjects.Count);

            Assert.AreEqual(0, bpmCollection.LoadedObjects.ElementAt(0).JsonTime, 0.001f);
            Assert.AreEqual(1, bpmCollection.LoadedObjects.ElementAt(1).JsonTime, 0.001f);
            Assert.AreEqual(2, bpmCollection.LoadedObjects.ElementAt(2).JsonTime, 0.001f);

            Assert.AreEqual(0, bpmCollection.LoadedObjects.ElementAt(0).SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111, bpmCollection.LoadedObjects.ElementAt(1).SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111 + songBpm / 222, bpmCollection.LoadedObjects.ElementAt(2).SongBpmTime, 0.001f);

            // Undo should remove everyhting
            actionContainer.Undo();
            Assert.AreEqual(0, bpmCollection.LoadedObjects.Count);

            // Redo should replace objects in the same positions
            actionContainer.Redo();
            Assert.AreEqual(3, bpmCollection.LoadedObjects.Count);

            Assert.AreEqual(0, bpmCollection.LoadedObjects.ElementAt(0).JsonTime, 0.001f);
            Assert.AreEqual(1, bpmCollection.LoadedObjects.ElementAt(1).JsonTime, 0.001f);
            Assert.AreEqual(2, bpmCollection.LoadedObjects.ElementAt(2).JsonTime, 0.001f);

            Assert.AreEqual(0, bpmCollection.LoadedObjects.ElementAt(0).SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111, bpmCollection.LoadedObjects.ElementAt(1).SongBpmTime, 0.001f);
            Assert.AreEqual(songBpm / 111 + songBpm / 222, bpmCollection.LoadedObjects.ElementAt(2).SongBpmTime, 0.001f);
        }
    }
}