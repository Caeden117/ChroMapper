using System;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class TempoMapTest
    {
        private static TempoMap Map() => new(new[]
        {
            new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 480000, StartBeat = 0, EndBeat = 20 }
        }, 48000);

        [Test]
        public void SplitAndMergePreserveAudioAndBeatCoordinates()
        {
            var map = Map();
            Assert.IsTrue(map.Split(120000));
            Assert.AreEqual(5, map.Regions[1].StartBeat);
            Assert.AreEqual(120, map.Bpm(0), 0.001);
            Assert.AreEqual(120, map.Bpm(1), 0.001);
            Assert.IsTrue(map.MergeNext(0));
            Assert.AreEqual(480000, map.EndSample);
            Assert.AreEqual(20, map.BeatAt(480000));
            map.Validate();
        }

        [Test]
        public void MovingDividerPreservesBeatSpansAndChangesBothTempos()
        {
            var map = Map();
            map.Split(240000);
            Assert.IsTrue(map.MoveBoundary(1, 120000));
            Assert.AreEqual(240, map.Bpm(0), 0.001);
            Assert.AreEqual(80, map.Bpm(1), 0.001);
            Assert.AreEqual(10, map.Regions[1].StartBeat);
            Assert.AreEqual(20, map.Regions[1].EndBeat);
            map.Validate();
        }

        [Test]
        public void BpmEditRebasesDownstreamBeatsWithoutMovingSamples()
        {
            var map = Map();
            map.Split(240000);
            Assert.IsTrue(map.SetBpm(0, 60));
            Assert.AreEqual(5, map.Regions[1].StartBeat);
            Assert.AreEqual(15, map.Regions[1].EndBeat);
            Assert.AreEqual(240000, map.Regions[1].StartSampleIndex);
            Assert.AreEqual(120, map.Bpm(1), 0.001);
        }

        [Test]
        public void MovingBeatStretchesRegionRatherThanMovingItsAudioBoundary()
        {
            var map = Map();
            Assert.IsTrue(map.MoveBeat(0, 4, 48000));
            Assert.AreEqual(240, map.Bpm(0), 0.001);
            Assert.AreEqual(480000, map.EndSample);
            Assert.AreEqual(48000, map.SampleAt(0, 4));
        }

        [Test]
        public void SingleBeatSplitPreservesRightRemainder()
        {
            var map = Map();
            map.Split(120000, true);
            Assert.AreEqual(1, map.Regions[0].EndBeat);
            Assert.AreEqual(16, map.Regions[1].EndBeat);
            map.Validate();
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1001)]
        public void InvalidBpmDoesNotMutateMap(double bpm)
        {
            var map = Map();
            Assert.IsFalse(map.SetBpm(0, bpm));
            Assert.AreEqual(120, map.Bpm(0), 0.001);
        }

        [Test]
        public void EndpointEditsCannotCreateEmptyRegions()
        {
            var map = Map();
            Assert.IsFalse(map.Split(0));
            Assert.IsFalse(map.Split(480000));
            map.Split(240000);
            Assert.IsFalse(map.MoveBoundary(1, 0));
            Assert.IsFalse(map.MoveBoundary(1, 480000));
        }

        [Test]
        public void GestureSnapshotIsIndependent()
        {
            var original = Map();
            var next = original.Copy();
            next.Split(240000);
            next.SetBpm(0, 100);
            Assert.AreEqual(1, original.Regions.Count);
            Assert.AreEqual(120, original.Bpm(0), 0.001);
        }

        [Test]
        public void DiscontinuousImportedRegionsAreRejected()
        {
            var map = Map();
            map.Split(240000);
            var region = map.Regions[1];
            region.StartSampleIndex++;
            map.Regions[1] = region;
            Assert.Throws<ArgumentException>(map.Validate);
        }

        [Test]
        public void TempoPatchPreservesUnloadedObjectsAndCustomData()
        {
            var node = JSON.Parse("{\"version\":\"3.3.0\",\"colorNotes\":[{\"b\":7}],\"customData\":{\"futureField\":42},\"bpmEvents\":[{\"b\":0,\"m\":99}]}");
            var output = TempoMapFileStore.WithTempo(node, Map());
            Assert.AreEqual(7, output["colorNotes"][0]["b"].AsInt);
            Assert.AreEqual(42, output["customData"]["futureField"].AsInt);
            Assert.AreEqual(120, output["bpmEvents"][0]["m"].AsFloat);
        }

        [Test]
        public void V2PatchReplacesOnlyBpmEventsAndKeepsLightEvents()
        {
            var node = JSON.Parse("{\"_version\":\"2.0.0\",\"_events\":[{\"_time\":1,\"_type\":0,\"_value\":3},{\"_time\":0,\"_type\":100,\"_floatValue\":99}]}");
            var output = TempoMapFileStore.WithTempo(node, Map());
            Assert.AreEqual(2, output["_events"].Count);
            Assert.AreEqual(120, output["_events"][0]["_floatValue"].AsFloat);
            Assert.AreEqual(3, output["_events"][1]["_value"].AsInt);
        }
        [Test]
        public void V1TempoPatchIsRejected()
        {
            var node = JSON.Parse("{\"_version\":\"1.5.0\",\"_events\":[]}");
            Assert.Throws<System.IO.InvalidDataException>(() => TempoMapFileStore.WithTempo(node, Map()));
        }

        [Test]
        public void V1DifficultyIsNotAcceptedByFactory()
        {
            var node = JSON.Parse("{\"_version\":\"1.5.0\",\"_events\":[],\"_notes\":[]}");
            Assert.IsNull(Beatmap.Helper.BeatmapFactory.GetDifficultyFromJson(node, "unused.json", new BaseInfo(), null));
        }
    }
}
