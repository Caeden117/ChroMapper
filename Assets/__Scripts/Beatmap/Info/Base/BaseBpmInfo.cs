using System;
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
        public List<BpmInfoLufsRegion> LufsRegions = new();
        
        public BaseBpmInfo InitWithSongContainerInstance()
        {
            Version = BeatSaberSongContainer.Instance.Info.Version[0] == '4' ? "4.0.0" : "2.0.0";
            AudioSamples = BeatSaberSongContainer.Instance.LoadedSongSamples;
            AudioFrequency = BeatSaberSongContainer.Instance.LoadedSongFrequency;

            return this;
        }

        public static string GetOutputFileName(int difficultyMajorVersion, BaseInfo info)
        {
            if (difficultyMajorVersion == 4)
            {
                return !string.IsNullOrWhiteSpace(info.AudioDataFilename)
                    ? info.AudioDataFilename
                    : V4AudioData.FileName;
            }

            return V2BpmInfo.FileName;
        }

        public static List<BaseBpmEvent> GetBpmEvents(List<BpmInfoBpmRegion> bpmRegions, int audioFrequency)
        {
            var bpmEvents = new List<BaseBpmEvent>();

            foreach (var bpmRegion in bpmRegions)
            {
                var samples = bpmRegion.EndSampleIndex - bpmRegion.StartSampleIndex;
                var beats = bpmRegion.EndBeat - bpmRegion.StartBeat;
                var rawBpm = beats / samples * audioFrequency * 60;

                // use rounded bpm if unrounding was caused by numerical error in conversion to BPM regions
                var roundedBpm = (float)Math.Round(beats / samples * audioFrequency * 60);
                var roundedBpmSamples = beats * 60 / roundedBpm * audioFrequency;
                const float threshold = 1.1f; // expect max of 0.5 samples either direction for both start and end, so max of 1 total, plus some margin
                var useRounded = Math.Abs(roundedBpmSamples - samples) < threshold;

                bpmEvents.Add(new BaseBpmEvent
                {
                    Bpm = useRounded ? roundedBpm : rawBpm,
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
                // This SHOULD be 0, since the fist BpmEvent is supposed to be at beat 0
                var previousEndSampleIndex = (int)Math.Round(bpmEvents[0].SongBpmTime * (60f / songBpm) * audioFrequency, MidpointRounding.AwayFromZero);

                for (var i = 0; i < bpmEvents.Count - 1; i++)
                {
                    var currentBpmEvent = bpmEvents[i];
                    var nextBpmEvent = bpmEvents[i + 1];
                    var endSampleIndex = (int)Math.Round(nextBpmEvent.SongBpmTime * (60f / songBpm) * audioFrequency, MidpointRounding.AwayFromZero);

                    regions.Add(new BpmInfoBpmRegion
                    {
                        StartSampleIndex = previousEndSampleIndex,
                        EndSampleIndex = endSampleIndex,
                        StartBeat = currentBpmEvent.JsonTime,
                        EndBeat = nextBpmEvent.JsonTime,
                    });

                    previousEndSampleIndex = endSampleIndex;
                }

                var lastBpmEvent = bpmEvents[^1];
                var secondsDiff = (float)(audioSamples - previousEndSampleIndex) / audioFrequency;
                var jsonBeatsDiff = secondsDiff * (lastBpmEvent.Bpm / 60f);

                regions.Add(new BpmInfoBpmRegion
                {
                    StartSampleIndex = previousEndSampleIndex,
                    EndSampleIndex = audioSamples,
                    StartBeat = lastBpmEvent.JsonTime,
                    EndBeat = lastBpmEvent.JsonTime + jsonBeatsDiff,
                });
            }

            return regions;
        }
    }

    public struct BpmInfoLufsRegion
    {
        public int StartSampleIndex { get; set; }
        public int EndSampleIndex { get; set; }
        public float Loudness { get; set; }
    }

    public struct BpmInfoBpmRegion
    {
        public int StartSampleIndex { get; set; }
        public int EndSampleIndex { get; set; }
        public float StartBeat { get; set; }
        public float EndBeat { get; set; }
    }
}
