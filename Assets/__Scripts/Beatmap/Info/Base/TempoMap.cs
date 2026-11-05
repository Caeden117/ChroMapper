using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

namespace Beatmap.Info
{
    /// <summary>Editable sample anchors. Adjacent regions share their endpoint; beats remain continuous.</summary>
    public class TempoMap
    {
        public List<BpmInfoBpmRegion> Regions { get; private set; }
        public int Frequency { get; }
        public int EndSample => Regions[^1].EndSampleIndex;

        public TempoMap(IEnumerable<BpmInfoBpmRegion> regions, int frequency)
        {
            Frequency = frequency;
            Regions = regions.ToList();
            Validate();
        }

        public TempoMap Copy() => new(Regions, Frequency);

        public int RegionAt(int sample)
        {
            for (var i = Regions.Count - 1; i > 0; i--)
                if (sample >= Regions[i].StartSampleIndex) return i;
            return 0;
        }

        public double Bpm(int index)
        {
            var r = Regions[index];
            return (double)(r.EndBeat - r.StartBeat) * Frequency * 60 / (r.EndSampleIndex - r.StartSampleIndex);
        }

        public double BeatAt(int sample)
        {
            return BeatAt(RegionAt(sample), sample);
        }

        public double BeatAt(int index, double sample)
        {
            var r = Regions[index];
            return r.StartBeat + (double)(sample - r.StartSampleIndex) / (r.EndSampleIndex - r.StartSampleIndex)
                * (r.EndBeat - r.StartBeat);
        }

        public int SampleAt(int index, double beat)
        {
            var r = Regions[index];
            return r.StartSampleIndex + (int)Math.Round((beat - r.StartBeat) / (r.EndBeat - r.StartBeat)
                * (r.EndSampleIndex - r.StartSampleIndex));
        }

        public bool Split(int sample, bool singleBeat = false)
        {
            var index = RegionAt(sample);
            var r = Regions[index];
            if (sample <= r.StartSampleIndex || sample >= r.EndSampleIndex) return false;
            var beat = (float)BeatAt(sample);
            var rightBeats = r.EndBeat - beat;
            if (singleBeat) beat = r.StartBeat + 1;
            Regions[index] = new BpmInfoBpmRegion
            {
                StartSampleIndex = r.StartSampleIndex, EndSampleIndex = sample,
                StartBeat = r.StartBeat, EndBeat = beat
            };
            Regions.Insert(index + 1, new BpmInfoBpmRegion
            {
                StartSampleIndex = sample, EndSampleIndex = r.EndSampleIndex,
                StartBeat = beat, EndBeat = beat + rightBeats
            });
            Rebase();
            return true;
        }

        public bool MergeNext(int index)
        {
            if (index < 0 || index >= Regions.Count - 1) return false;
            var left = Regions[index];
            var right = Regions[index + 1];
            left.EndSampleIndex = right.EndSampleIndex;
            left.EndBeat += right.EndBeat - right.StartBeat;
            Regions[index] = left;
            Regions.RemoveAt(index + 1);
            Rebase();
            return true;
        }

        public bool MoveBoundary(int rightIndex, int sample)
        {
            if (rightIndex <= 0 || rightIndex >= Regions.Count) return false;
            var left = Regions[rightIndex - 1];
            var right = Regions[rightIndex];
            if (sample <= left.StartSampleIndex || sample >= right.EndSampleIndex) return false;
            left.EndSampleIndex = right.StartSampleIndex = sample;
            Regions[rightIndex - 1] = left;
            Regions[rightIndex] = right;
            return true;
        }

        public bool SetBeats(int index, double beats)
        {
            if (!Finite(beats) || beats <= 0) return false;
            var r = Regions[index];
            var bpm = beats * Frequency * 60 / (r.EndSampleIndex - r.StartSampleIndex);
            if (bpm < 1 || bpm > 1000) return false;
            r.EndBeat = r.StartBeat + (float)beats;
            if (r.EndBeat <= r.StartBeat) return false;
            Regions[index] = r;
            Rebase();
            return true;
        }

        public bool SetBpm(int index, double bpm)
        {
            var r = Regions[index];
            return SetBeats(index, bpm * (r.EndSampleIndex - r.StartSampleIndex) / (Frequency * 60d));
        }

        public bool MoveBeat(int index, double beat, int sample)
        {
            var r = Regions[index];
            if (sample <= r.StartSampleIndex || beat <= r.StartBeat) return false;
            return SetBeats(index, (beat - r.StartBeat) * (r.EndSampleIndex - r.StartSampleIndex)
                / (sample - r.StartSampleIndex));
        }

        /// <summary>Shift every editable tempo anchor together while keeping the song origin fixed.</summary>
        public bool ShiftTimeline(int delta)
        {
            if (delta == 0) return false;
            if (Regions.Count == 1)
            {
                var anchorBeat = Math.Min(1, Regions[0].EndBeat / 2d);
                return MoveBeat(0, anchorBeat, SampleAt(0, anchorBeat) + delta);
            }
            if (Regions[1].StartSampleIndex + delta <= 0 || Regions[^1].StartSampleIndex + delta >= EndSample) return false;
            for (var i = 0; i < Regions.Count; i++)
            {
                var r = Regions[i];
                if (i > 0) r.StartSampleIndex += delta;
                if (i < Regions.Count - 1) r.EndSampleIndex += delta;
                var bpm = (r.EndBeat - r.StartBeat) * Frequency * 60d / (r.EndSampleIndex - r.StartSampleIndex);
                if (bpm < 1 || bpm > 1000) return false;
            }
            for (var i = 0; i < Regions.Count; i++)
            {
                var r = Regions[i];
                if (i > 0) r.StartSampleIndex += delta;
                if (i < Regions.Count - 1) r.EndSampleIndex += delta;
                Regions[i] = r;
            }
            return true;
        }

        public List<BaseBpmEvent> ToEvents() => Regions.Select((r, i) => new BaseBpmEvent(r.StartBeat, (float)Bpm(i))).ToList();

        private void Rebase()
        {
            var beat = 0f;
            for (var i = 0; i < Regions.Count; i++)
            {
                var r = Regions[i];
                var length = r.EndBeat - r.StartBeat;
                r.StartBeat = beat;
                r.EndBeat = beat += length;
                Regions[i] = r;
            }
        }

        public void Validate()
        {
            if (Frequency <= 0 || Regions.Count == 0) throw new ArgumentException("Invalid tempo map.");
            var sample = 0;
            var beat = 0f;
            foreach (var r in Regions)
            {
                if (r.StartSampleIndex != sample || r.EndSampleIndex <= sample || r.StartBeat != beat
                    || !Finite(r.EndBeat) || r.EndBeat <= beat)
                    throw new ArgumentException("Tempo regions must be ordered, continuous and have positive spans.");
                sample = r.EndSampleIndex;
                beat = r.EndBeat;
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
