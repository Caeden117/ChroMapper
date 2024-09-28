using System.Collections.Generic;
using Beatmap.Base;

namespace Beatmap.Info
{
    public class BaseBpmInfo
    {
        public string Version { get; set; }
        public int AudioSamples { get; set; }
        public int AudioFrequency { get; set; }
        public List<BpmInfoBpmRegion> BpmRegions = new();
        
        public BaseBpmInfo InitWithSongContainerInstance()
        {
            Version = "2.0.0";
            AudioSamples = BeatSaberSongContainer.Instance.LoadedSongSamples;
            AudioFrequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;

            return this;
        }
        
        public static string GetOutputFileName(BeatSaberSong info) => V2BpmInfo.FileName;
        
        public static List<BaseBpmEvent> GetBpmEvents(List<BpmInfoBpmRegion> bpmRegions, int audioFrequency)
        {
            var bpmEvents = new List<BaseBpmEvent>();

            foreach (var bpmRegion in bpmRegions)
            {
                var samples = bpmRegion.EndSampleIndex - bpmRegion.StartSampleIndex;
                var beats = bpmRegion.EndBeat - bpmRegion.StartBeat;
                
                bpmEvents.Add(new BaseBpmEvent
                {
                    Bpm = beats / samples * audioFrequency * 60,
                    JsonTime = bpmRegion.StartBeat
                });
            }
            
            return bpmEvents;
        }

        public static List<BpmInfoBpmRegion> GetBpmInfoRegions(List<BaseBpmEvent> bpmEvents, float songBpm, int audioSamples, int audioFrequency)
        {
            var regions = new List<BpmInfoBpmRegion>();
            
            if (bpmEvents.Count == 0)
            {
                regions.Add(new BpmInfoBpmRegion
                {
                    StartSampleIndex = 0,
                    EndSampleIndex = audioSamples,
                    StartBeat = 0f,
                    EndBeat = (songBpm / 60f) * ((float)audioSamples / audioFrequency),
                });
            }
            else
            {
                for (var i = 0; i < bpmEvents.Count - 1; i++)
                {
                    var currentBpmEvent = bpmEvents[i];
                    var nextBpmEvent = bpmEvents[i + 1];

                    regions.Add(new BpmInfoBpmRegion
                    {
                        StartSampleIndex = (int)(currentBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        EndSampleIndex = (int)(nextBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency),
                        StartBeat = currentBpmEvent.JsonTime,
                        EndBeat = nextBpmEvent.JsonTime,
                    });
                }

                var lastBpmEvent = bpmEvents[^1];
                var lastStartSampleIndex = lastBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency;
                var secondsDiff = (audioSamples - lastStartSampleIndex) / audioFrequency;
                var jsonBeatsDiff = secondsDiff * (lastBpmEvent.Bpm / 60f);

                regions.Add(new BpmInfoBpmRegion
                {
                    StartSampleIndex = (int)lastStartSampleIndex,
                    EndSampleIndex = audioSamples,
                    StartBeat = lastBpmEvent.JsonTime,
                    EndBeat = lastBpmEvent.JsonTime + jsonBeatsDiff,
                });
            }

            return regions;
        }
    }

    public struct BpmInfoBpmRegion
    {
        public int StartSampleIndex { get; set; }
        public int EndSampleIndex { get; set; }
        public float StartBeat { get; set; }
        public float EndBeat { get; set; }
    }
}
