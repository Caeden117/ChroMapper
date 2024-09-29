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

        // This is a 30 second file with:
        //  - 60 bpm at beat 10
        //  - 120 bpm at beat 20
        // Lufs is a single region
        private const string audioDataJson = @"
{
    ""version"": ""4.0.0"",
    ""songChecksum"": """",
    ""songSampleCount"": 882000,
    ""songFrequency"": 44100,
    ""bpmData"": [
        {
            ""si"": 0,
            ""ei"": 441000,
            ""sb"": 0,
            ""eb"": 10
        }
        {
            ""si"": 441000,
            ""ei"": 882000,
            ""sb"": 10,
            ""eb"": 30
        }
    ],
    ""lufsData"": [
        {
            ""si"": 0,
            ""ei"": 882000,
            ""l"": 3.1
        }
    ]
}
";
        
        [Test]
        public void GetFromJson_V2BpmInfo()
        {
            var bpmInfo = V2BpmInfo.GetFromJson(JSON.Parse(bpmInfoJson));
            
            Assert.AreEqual("2.0.0", bpmInfo.Version);

            AssertCommonGetFromJson(bpmInfo);

            Assert.AreEqual(0, bpmInfo.LufsRegions.Count);
        }
        
        [Test]
        public void GetFromJson_V4AudioData()
        {
          var bpmInfo = V4AudioData.GetFromJson(JSON.Parse(audioDataJson));

          AssertCommonGetFromJson(bpmInfo);
          
          Assert.AreEqual("4.0.0", bpmInfo.Version);

          Assert.AreEqual(1, bpmInfo.LufsRegions.Count);
          Assert.AreEqual(0, bpmInfo.LufsRegions[0].StartSampleIndex);
          Assert.AreEqual(882000, bpmInfo.LufsRegions[0].EndSampleIndex);
          Assert.AreEqual(3.1f, bpmInfo.LufsRegions[0].Loudness);
        }

        private void AssertCommonGetFromJson(BaseBpmInfo bpmInfo)
        {
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
        public void GetBpmEvents()
        {
            var audioFrequency = 44100;
            var bpmRegions = new List<BpmInfoBpmRegion>
            {
                new()
                {
                    StartBeat = 0f,
                    EndBeat = 10f,
                    StartSampleIndex = 0,
                    EndSampleIndex = audioFrequency * 10
                },
                new()
                {
                    StartBeat = 10f,
                    EndBeat = 30f,
                    StartSampleIndex = audioFrequency * 10,
                    EndSampleIndex = audioFrequency * 20
                }
            };

            var bpmEvents = BaseBpmInfo.GetBpmEvents(bpmRegions, audioFrequency);

            Assert.AreEqual(2, bpmEvents.Count);

            Assert.AreEqual(0, bpmEvents[0].JsonTime);
            Assert.AreEqual(60, bpmEvents[0].Bpm);

            Assert.AreEqual(10, bpmEvents[1].JsonTime);
            Assert.AreEqual(120, bpmEvents[1].Bpm);
        }
        
        [Test]
        public void GetBpmEvents_Empty()
        {
            var audioFrequency = 44100;
            var bpmRegions = new List<BpmInfoBpmRegion>();

            var bpmEvents = BaseBpmInfo.GetBpmEvents(bpmRegions, audioFrequency);

            Assert.AreEqual(0, bpmEvents.Count); // lol
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

        [Test]
        public void ConversionDoesNotIntroduceDriftOverTime()
        {
            var songBpm = 60f;
            var audioFrequency = 44100;
            var audiosamples = 44100 * 20;
            
            var initialBpmEvents = new List<BaseBpmEvent>
            {
                new() { JsonTime = 0, Bpm = 60 },
                new() { JsonTime = 10, Bpm = 120 }
            };
            var initialRegions = BaseBpmInfo.GetBpmInfoRegions(initialBpmEvents, songBpm, audiosamples, audioFrequency);
            
            // Loop conversion to and from a bunch of times
            List<BaseBpmEvent> bpmEvents = new List<BaseBpmEvent>
            {
                new() { JsonTime = 0, Bpm = 60 },
                new() { JsonTime = 10, Bpm = 120 }
            };
            List<BpmInfoBpmRegion> regions = new List<BpmInfoBpmRegion>();
            for (var i = 0; i < 100; i++)
            {
                regions = BaseBpmInfo.GetBpmInfoRegions(bpmEvents, songBpm, audiosamples, audioFrequency);
                bpmEvents = BaseBpmInfo.GetBpmEvents(regions, audioFrequency);
            }
            
            // Compare bpm events
            Assert.AreEqual(initialBpmEvents.Count, bpmEvents.Count);
            
            Assert.AreEqual(initialBpmEvents[0].JsonTime, bpmEvents[0].JsonTime);
            Assert.AreEqual(initialBpmEvents[0].Bpm, bpmEvents[0].Bpm);
            
            Assert.AreEqual(initialBpmEvents[1].JsonTime, bpmEvents[1].JsonTime);
            Assert.AreEqual(initialBpmEvents[1].Bpm, bpmEvents[1].Bpm);
            
            // Compare regions
            Assert.AreEqual(initialRegions.Count, regions.Count);
            
            Assert.AreEqual(initialRegions[0].StartBeat, regions[0].StartBeat);
            Assert.AreEqual(initialRegions[0].EndBeat, regions[0].EndBeat);
            Assert.AreEqual(initialRegions[0].StartSampleIndex, regions[0].StartSampleIndex);
            Assert.AreEqual(initialRegions[0].EndSampleIndex, regions[0].EndSampleIndex);
            
            Assert.AreEqual(initialRegions[1].StartBeat, regions[1].StartBeat);
            Assert.AreEqual(initialRegions[1].EndBeat, regions[1].EndBeat);
            Assert.AreEqual(initialRegions[1].StartSampleIndex, regions[1].StartSampleIndex);
            Assert.AreEqual(initialRegions[1].EndSampleIndex, regions[1].EndSampleIndex);
        }
    }
}