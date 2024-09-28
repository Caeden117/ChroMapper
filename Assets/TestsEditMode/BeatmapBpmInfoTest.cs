using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;

namespace TestsEditMode
{
    public class BeatmapBpmInfoTest
    {
        // This is a 30 second file with:
        //  - 60 bpm at beat 10
        //  - 120 bpm at beat 20
        private const string bpmInfoJson = @"
{
    ""_version"": ""2.0.0"",
    ""_songSampleCount"": 882000,
    ""_songFrequency"": 44100,
    ""_regions"": [
        {
            ""_startSampleIndex"": 0,
            ""_endSampleIndex"": 441000,
            ""_startBeat"": 0,
            ""_endBeat"": 10
        },
        {
            ""_startSampleIndex"": 441000,
            ""_endSampleIndex"": 882000,
            ""_startBeat"": 10,
            ""_endBeat"": 30
        }
    ]
}
";
        
        [Test]
        public void GetFromJson_V2BpmInfo()
        {
            var bpmInfo = V2BpmInfo.GetFromJson(JSON.Parse(bpmInfoJson));
            
            Assert.AreEqual("2.0.0", bpmInfo.Version);
            
            Assert.AreEqual(882000, bpmInfo.AudioSamples);
            Assert.AreEqual(44100, bpmInfo.AudioFrequency);

            Assert.AreEqual(2, bpmInfo.BpmRegions.Count);

            Assert.AreEqual(0, bpmInfo.BpmRegions[0].StartSampleIndex);
            Assert.AreEqual(441000, bpmInfo.BpmRegions[0].EndSampleIndex);
            Assert.AreEqual(0, bpmInfo.BpmRegions[0].StartBeat);
            Assert.AreEqual(10, bpmInfo.BpmRegions[0].EndBeat);
            
            Assert.AreEqual(441000, bpmInfo.BpmRegions[1].StartSampleIndex);
            Assert.AreEqual(882000, bpmInfo.BpmRegions[1].EndSampleIndex);
            Assert.AreEqual(10, bpmInfo.BpmRegions[1].StartBeat);
            Assert.AreEqual(30, bpmInfo.BpmRegions[1].EndBeat);
        }

        [Test]
        public void GetBpmInfoRegions()
        {
            var songBpm = 60f;
            var audioFrequency = 44100;
            var audiosamples = 44100 * 20;
            var bpmEvents = new List<BaseBpmEvent>
            {
                new() { JsonTime = 0, Bpm = 60 },
                new() { JsonTime = 10, Bpm = 120 }
            };

            var regions = BaseBpmInfo.GetBpmInfoRegions(bpmEvents, songBpm, audiosamples, audioFrequency);
            Assert.AreEqual(2, regions.Count);
            
            Assert.AreEqual(0f, regions[0].StartBeat);
            Assert.AreEqual(10f, regions[0].EndBeat);
            Assert.AreEqual(0, regions[0].StartSampleIndex);
            Assert.AreEqual(audioFrequency * 10, regions[0].EndSampleIndex);
            
            Assert.AreEqual(10f, regions[1].StartBeat);
            Assert.AreEqual(30f, regions[1].EndBeat);
            Assert.AreEqual(audioFrequency * 10, regions[1].StartSampleIndex);
            Assert.AreEqual(audiosamples, regions[1].EndSampleIndex);
        }
        
        [Test]
        public void GetBpmInfoRegions_Empty()
        {
            var songBpm = 60f;
            var audioFrequency = 44100;
            var audiosamples = 44100 * 20;
            var bpmEvents = new List<BaseBpmEvent>();

            var regions = BaseBpmInfo.GetBpmInfoRegions(bpmEvents, songBpm, audiosamples, audioFrequency);
            Assert.AreEqual(1, regions.Count);
            
            Assert.AreEqual(0f, regions[0].StartBeat);
            Assert.AreEqual(20f, regions[0].EndBeat);
            Assert.AreEqual(0, regions[0].StartSampleIndex);
            Assert.AreEqual(audiosamples, regions[0].EndSampleIndex);
        }
    }
}