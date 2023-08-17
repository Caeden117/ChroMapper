using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * A BPM and offset estimation algorithm by Bram van de Wetering, aka Fietsemaker, the creator of ArrowVortex
 * Adapted by mattmora from C++ to C# for use in Unity for Chromapper.
 * Depends on Aubio.NET for onset detection (hard to replace)
 * and MathNet.Numerics for polynomial fitting and evaluation (easier to replace)
 * Partial source as well as van de Wetering's paper describing the algorithm, 
 * and a modified C++ implementation by Nathan Stephenson can be found at
 * https://github.com/nathanstep55/bpm-offset-detector
 * Revisions (excluding translation / formatting):
 * 2023 June (initial port to Chromapper)
 * - Removed threading during interval testing (bc I don't know how to do that kind of parallel calculation this context)
 * - Increased default block size to 2048 from 1024 (thinking is that this limits the low end of spectral info, 
 *   and while 1024 is good, 2048 fully encapsulates the audible range at 44100 sample rate
 * - Fix: apply new fitness when accepting rounded BPMs
 * - Attempt rounding for more BPMs (within 0.3 diff instead of 0.05)
 * - Reduced roundingThreshold to 0.95 from 0.99 (what's good for this highly depends on strength calculation)
 * - removed unchecked "close enough" rounding
 * - ported relevant aubio code, but using existing FFT in Chromapper (might have slightly change results, but still good)
 * - increased default minimum BPM to save on calculation time and duplicates - rely on user discretion to half/double results
 * - set intervalDelta to 1 - better guarantees accuracy and doesn't actually slow things down much surprisingly
 *           
 * Variables that might be worth exposing to the user:
 * - blockSize - 2048 seems way better, but 1024 is serviceable
 * - hopSize - 256 is good, sometimes 128 gives slightly better results but is slower
 * - Method - SpectralFlux or ComplexDomain, both perform very well; papers suggest SF is best, but the "original" code appears to uses CD and it seems slightly better in testing
 *   in further testing, I think AV might actually use HFC? though it's not necessarily best. Also tested, Kulback-Liebler but it doesn't seem great
 *   (guessing the issue w/ SpectralFlux is that it highly depends on the pitch content of the main onset of the track, which can be very inconsistent across genres)
 * - Threshold (onsets) - 0.1-0.18 or ?
 * - Onset strength calculation
 * 
 * Potential TODOs, might be over-complicating
 * - UI
 * - optimization (increasing intervalDownsample significantly shortens the onset analysis process, but actual onset detection is currently pretty slow with the ported aubio code)
 * - song time range selection for use with variable BPM songs
 * - easy multiples?
 * - advanced settings tweaking (like choose Method of Threshold)
 */
public class SyncAnalysis
{
    private readonly double minimumBPM;
    private readonly double maximumBPM;
    private const int intervalDelta = 1;
    private const int intervalDownsample = 5;

    // Size of window around onset sample used to calculate onset strength, can significantly affect results
    // 200 is from the original code, not sure where it comes from, but it seems to work well
    private const int strengthWindowSize = 200;
    // Fitness ratio of rounded to un-rounded required to accept a rounded BPM
    private const double roundingThreshold = 0.95;

    public float[] Samples;
    public int SampleRate;
    public int NumFrames;
    public List<TempoResult> Results = new List<TempoResult>();
    public float Progress { get; private set; }

    public SyncAnalysis(double minBPM = 89.0, double maxBPM = 205.0)
    //public SyncAnalysis(double minBPM, double maxBPM)
    {
        minimumBPM = minBPM;
        maximumBPM = maxBPM;
    }

    public void Run(float[] audioData, int channels, int sampleRate, int blockSize = 2048, int hopSize = 256)
    {
        SampleRate = sampleRate;

        // Run the aubio onset tracker to find note onsets.
        var onsetDetection = new Aubio.OnsetDetector(Aubio.SpectralDescription.OnsetType.ComplexDomain, blockSize, hopSize, sampleRate)
        //var onsetDetection = new Aubio.OnsetDetector(Aubio.SpectralDescription.OnsetType.SpectralFlux, blockSize, hopSize, sampleRate)
        //var onsetDetection = new Aubio.OnsetDetector(Aubio.SpectralDescription.OnsetType.HFC, blockSize, hopSize, sampleRate)
        {
            // 0.1 Threshold is from van de Wetering's paper, though also the paper says it uses SpecFlux so idk
            // I'm getting best result with ComplexDomain and these settings
            Threshold = 0.1,
        };

        NumFrames = audioData.Length / channels;
        // Mix down to mono for processing
        Samples = new float[NumFrames];
        var hopData = new double[hopSize];
        var onsetOut = new double[1];
        var onsets = new List<Onset>();
        for (var i = 0; i < NumFrames; i++)
        {
            for (var c = 0; c < channels; c++)
            {
                Samples[i] += audioData[(i * channels) + c];
            }
            Samples[i] /= channels;
            hopData[i % hopSize] = Samples[i];
            if (i % hopSize == hopSize - 1)
            {
                onsetDetection.Do(hopData, onsetOut);
                if (onsetOut[0] >= 1) onsets.Add(new Onset(onsetDetection.Last, 0f));
            }
        }
        //MarkProgress(1, "Find onsets");

        for (var i = 0; i < onsets.Count; ++i)
        {
            var onset = onsets[i];
            var a = Math.Max(0, onset.Position - (strengthWindowSize / 2));
            var b = Math.Min(Samples.Length, onset.Position + (strengthWindowSize / 2));
            var v = 0.0;
            for (var j = a; j < b; ++j)
            {
                v += Math.Abs(Samples[j]);
            }
            v /= Math.Max(1, b - a);
            onsets[i].Strength = (float)v;
        }

        Debug.Log(onsets.Count);

        // Find BPM values.
        CalculateBPM(onsets);
        //MarkProgress(4, "Find BPM");

        // Find offset values.
        CalculateOffset(onsets);
        //MarkProgress(5, "Find offsets");

    }

    // ================================================================================================
    // Helper structs.
    private class Onset
    {
        public int Position;
        public float Strength;

        public Onset(int pos, float strength) { Position = pos; Strength = strength; }

        public override string ToString() => $"Onset({Position}, {Strength})";
    }

    public class TempoResult
    {
        public float Fitness;
        public float BPM;
        public float Beat { get => 60f / BPM; private set { } }
        public float Offset;

        public TempoResult(float bpm, float offset, float fitness)
        {
            Debug.Log($"TempoResult: {bpm} {offset} {fitness}");
            BPM = bpm; Offset = offset; Fitness = fitness;
        }
    };

    // ================================================================================================
    // Gap confidence evaluation
    private class GapData
    {
        // TODO figure out which of these should be public
        public List<Onset> Onsets;
        public int[] WrappedPos;
        public double[] WrappedOnsets;
        public double[] Window;
        public int BufferSize, NumOnsets, WindowSize, Downsample;

        public GapData(int maxInterval, int downsample, List<Onset> onsets)
        {
            NumOnsets = onsets.Count;
            Onsets = onsets;
            Downsample = downsample;
            WindowSize = 2048 >> downsample;
            BufferSize = maxInterval;

            Window = CreateHammingWindow(WindowSize);
            WrappedPos = new int[NumOnsets];
            WrappedOnsets = new double[BufferSize];
        }

        // This is slightly different from the DSPLib Hamming Window, so keeping it for consistency
        // Creates weights for a hamming window of length n.
        private double[] CreateHammingWindow(int n)
        {
            var output = new double[n];
            var t = 6.2831853071795864 / (n - 1);
            for (var i = 0; i < n; ++i) output[i] = 0.54 - (0.46 * Math.Cos(i * t));
            return output;
        }

        // Returns the confidence value that indicates how many onsets are close to the given gap position.
        public double GapConfidence(int gapPos, int interval)
        {
            var halfWindowSize = WindowSize / 2;
            var area = 0.0;

            var beginOnset = gapPos - halfWindowSize;
            var endOnset = gapPos + halfWindowSize;

            if (beginOnset < 0)
            {
                var wrappedBegin = beginOnset + interval;
                for (var i = wrappedBegin; i < interval; ++i)
                {
                    var windowIndex = i - wrappedBegin;
                    area += WrappedOnsets[i] * Window[windowIndex];
                }
                beginOnset = 0;
            }
            if (endOnset > interval)
            {
                var wrappedEnd = endOnset - interval;
                var indexOffset = WindowSize - wrappedEnd;
                for (var i = 0; i < wrappedEnd; ++i)
                {
                    var windowIndex = i + indexOffset;
                    area += WrappedOnsets[i] * Window[windowIndex];
                }
                endOnset = interval;
            }
            for (var i = beginOnset; i < endOnset; ++i)
            {
                var windowIndex = i - beginOnset;
                area += WrappedOnsets[i] * Window[windowIndex];
            }

            return area;
        }

        // Returns the confidence of the best gap value for the given interval.
        public double GetConfidenceForInterval(int interval)
        {
            Array.Clear(WrappedOnsets, 0, WrappedOnsets.Length);

            // Make a histogram of onset strengths for every position in the interval.
            var reducedInterval = interval >> Downsample;
            for (var i = 0; i < NumOnsets; ++i)
            {
                var pos = (Onsets[i].Position % interval) >> Downsample;
                WrappedPos[i] = pos;
                WrappedOnsets[pos] += Onsets[i].Strength;
            }

            // Record the amount of support for each gap value.
            var highestConfidence = 0.0;
            for (var i = 0; i < NumOnsets; ++i)
            {
                var pos = WrappedPos[i];
                var confidence = GapConfidence(pos, reducedInterval);
                var offbeatPos = (pos + (reducedInterval / 2)) % reducedInterval;
                confidence += GapConfidence(offbeatPos, reducedInterval) * 0.5;

                if (confidence > highestConfidence)
                {
                    highestConfidence = confidence;
                }
            }

            return highestConfidence;
        }

        // Returns the confidence of the best gap value for the given BPM value.
        // Specifically useful for testing BPMs that correspond to fractional intervals
        public double GetConfidenceForBPM(IntervalTester test, double bpm)
        {
            Array.Clear(WrappedOnsets, 0, WrappedOnsets.Length);

            var intervalf = test.SampleRate * 60.0 / bpm;
            var interval = (int)(intervalf + 0.5);
            for (var i = 0; i < NumOnsets; ++i)
            {
                var pos = (int)(Onsets[i].Position % intervalf);
                WrappedPos[i] = pos;
                WrappedOnsets[pos] += Onsets[i].Strength;
            }

            // Record the amount of support for each gap value.
            var highestConfidence = 0.0;
            for (var i = 0; i < NumOnsets; ++i)
            {
                var pos = WrappedPos[i];
                var confidence = GapConfidence(pos, interval);
                var offbeatPos = (pos + (interval / 2)) % interval;
                confidence += GapConfidence(offbeatPos, interval) * 0.5;

                if (confidence > highestConfidence)
                {
                    highestConfidence = confidence;
                }
            }

            // Normalize the confidence value.
            highestConfidence -= test.Poly.Evaluate(intervalf);

            return highestConfidence;
        }
    };

    // ================================================================================================
    // Interval testing

    private class IntervalTester
    {
        public int MinInterval;
        public int MaxInterval;
        public int NumIntervals;
        public int SampleRate;
        public int GapWindowSize;
        public int NumOnsets;
        public List<Onset> Onsets;
        public double[] Fitness;
        public MathNet.Numerics.Polynomial Poly;

        public IntervalTester(int sampleRate, int numOnsets, List<Onset> onsets, double minBPM, double maxBPM)
        {
            SampleRate = sampleRate;
            NumOnsets = numOnsets;
            Onsets = onsets;
            MinInterval = (int)((sampleRate * 60.0 / maxBPM) + 0.5);
            MaxInterval = (int)((sampleRate * 60.0 / minBPM) + 0.5);
            NumIntervals = MaxInterval - MinInterval;
            Fitness = new double[NumIntervals];
        }

        public double IntervalToBPM(int i) => SampleRate * 60.0 / (i + MinInterval);

        public void FillCoarseIntervals(GapData gapData)
        {
            //Debug.Log("Coarse start.");
            var numCoarseIntervals = (NumIntervals + intervalDelta - 1) / intervalDelta;
            //Parallel.For(0, numCoarseIntervals, (i) =>
            for (var i = 0; i < numCoarseIntervals; i++)
            {
                var index = i * intervalDelta;
                var interval = MinInterval + index;
                Fitness[index] = Math.Max(0.001, gapData.GetConfidenceForInterval(interval));
                //Debug.Log(Fitness[index]);
            }
            //Debug.Log("Coarse done.");
        }

        public (int, int) FillIntervalRange(GapData gapData, int begin, int end)
        {
            begin = Math.Max(begin, 0);
            end = Math.Min(end, NumIntervals);
            var fitIndex = begin;
            for (int i = begin, interval = MinInterval + begin; i < end; ++i, ++interval, ++fitIndex)
            {
                if (Fitness[fitIndex] == 0)
                {
                    Fitness[fitIndex] = gapData.GetConfidenceForInterval(interval) - Poly.Evaluate(interval);
                    //NormalizeFitness(Fitness[fitIndex], Poly, interval);
                    Fitness[fitIndex] = Math.Max(Fitness[fitIndex], 0.1);
                }
            }
            return (begin, end);
        }

        public int FindBestInterval(double[] fitness, int begin, int end)
        {
            var bestInterval = 0;
            var highestFitness = 0.0;
            for (var i = begin; i < end; ++i)
            {
                if (fitness[i] > highestFitness)
                {
                    highestFitness = fitness[i];
                    bestInterval = i;
                }
            }
            return bestInterval;
        }
    };

    // ================================================================================================
    // BPM testing

    // Removes BPM values that are near-duplicates or multiples of a better BPM value.
    private void RemoveDuplicates(List<TempoResult> results, double precision = 0.1)
    {
        for (var i = 0; i < results.Count; ++i)
        {
            double bpm = results[i].BPM, doubled = bpm * 2.0, halved = bpm * 0.5;
            for (var j = results.Count - 1; j > i; --j)
            {
                var v = results[j].BPM;
                if (Math.Min(Math.Min(Math.Abs(v - bpm), Math.Abs(v - doubled)), Math.Abs(v - halved)) <= precision)
                {
                    //Debug.Log($"Removing {results[j].BPM} ({results[j].Fitness}), keeping {results[i].BPM} ({results[i].Fitness})");
                    results.RemoveAt(j);
                }
            }
        }
    }

    // Rounds BPM values that are close to integer values.
    private void RoundBPMValues(IntervalTester test, GapData gapData, List<TempoResult> results, double range, double threshold)
    {
        foreach (var result in results)
        {
            var roundBPM = (float)Math.Round(result.BPM);
            var diff = Math.Abs(result.BPM - roundBPM);
            // mattmora - better not to do this imo, as the roundingThreshold will already accommodate for "close enough" rounding cases
            //if (diff < 0.01)
            //{
            //    result.BPM = roundBPM;
            //    result.Fitness = (float)gapData.GetConfidenceForBPM(test, roundBPM);
            //}
            //else
            if (diff < range)
            {
                var old = gapData.GetConfidenceForBPM(test, result.BPM);
                var cur = gapData.GetConfidenceForBPM(test, roundBPM);
                Debug.Log($"round {roundBPM}<{cur}> v {result.BPM}<{old * roundingThreshold}({old})>");
                if (cur > old * threshold)
                {
                    result.BPM = roundBPM;
                    result.Fitness = (float)cur;
                }
            }
        }
    }

    // Finds likely BPM candidates based on the given note onset values.
    private void CalculateBPM(List<Onset> onsets)
    {
        var numOnsets = onsets.Count;
        // In order to determine the BPM, we need at least two onsets.
        if (numOnsets < 2)
        {
            Results.Add(new TempoResult(100f, 0f, 1f));
            return;
        }

        var test = new IntervalTester(SampleRate, numOnsets, onsets, minimumBPM, maximumBPM);
        var gapData = new GapData(test.MaxInterval, intervalDownsample, onsets);

        // Loop through every 10th possible BPM, later we will fill in those that look interesting.
        //for (var i = 0; i < test.NumIntervals; i++) test.Fitness[i] = 0.0;
        test.FillCoarseIntervals(gapData);
        var numCoarseIntervals = (test.NumIntervals + intervalDelta - 1) / intervalDelta;

        //MarkProgress(2, "Fill course intervals");

        // Determine the polynomial coefficients to approximate the fitness curve and normalize the current fitness values.
        var polyX = new double[numCoarseIntervals];
        var polyY = new double[numCoarseIntervals];
        for (var i = 0; i < numCoarseIntervals; i++)
        {
            var index = i * intervalDelta;
            var interval = test.MinInterval + index;
            polyX[i] = interval;
            polyY[i] = test.Fitness[index];
        }
        test.Poly = MathNet.Numerics.Polynomial.Fit(polyX, polyY, 3);
        var maxFitness = 0.001;
        for (var i = 0; i < test.NumIntervals; i += intervalDelta)
        {
            //test.Fitness[i] -= NormalizeFitness(test.Fitness[i], test.Poly, i);
            test.Fitness[i] -= test.Poly.Evaluate(i + test.MinInterval);
            //Debug.Log($"Fitness: {test.Fitness[i]}");
            maxFitness = Math.Max(maxFitness, test.Fitness[i]);
        }

        // Refine the intervals around the best intervals.
        var fitnessThreshold = maxFitness * 0.4;
        for (var i = 0; i < test.NumIntervals; i += intervalDelta)
        {
            if (test.Fitness[i] > fitnessThreshold)
            {
                if (intervalDelta > 1)
                {
                    (var begin, var end) = test.FillIntervalRange(gapData, i - (intervalDelta - 1), i + intervalDelta);
                    //Debug.Log($"begin {begin} end {end}");
                    var best = test.FindBestInterval(test.Fitness, begin, end);
                    Results.Add(new TempoResult((float)test.IntervalToBPM(best), 0f, (float)test.Fitness[best]));
                }
                else
                {
                    Results.Add(new TempoResult((float)test.IntervalToBPM(i), 0f, (float)test.Fitness[i]));
                }
            }
        }

        //MarkProgress(3, "Refine intervals");

        // At this point we stop the downsampling and upgrade to a more precise gap window.
        gapData = new GapData(test.MaxInterval, 0, onsets);

        // Round BPM values to integers when possible, and remove weaker duplicates.
        Results.Sort((a, b) => a.Fitness > b.Fitness ? -1 : 1);

        // Strict rounding to avoid throwing out best duplicates
        RoundBPMValues(test, gapData, Results, 0.1, 1);
        // Remove duplicates, keep the best
        RemoveDuplicates(Results);
        // General rounding, accept rounded BPMs that are slightly worse bc they're probably right actually
        RoundBPMValues(test, gapData, Results, 0.3, roundingThreshold);
        // Remove duplicates produced by rounding
        RemoveDuplicates(Results, 0);

        Results.Sort((a, b) => a.Fitness > b.Fitness ? -1 : 1);

        // If the fitness of the first and second option is very close, we ask for a second opinion now that we've stopped downsampling
        if (Results.Count >= 2 && Results[0].Fitness / Results[1].Fitness < 1.05)
        {
            Debug.Log("Double Check");
            for (var i = 0; i < Results.Count; i++)
            {
                Results[i].Fitness = (float)gapData.GetConfidenceForBPM(test, Results[i].BPM);
            }
            Results.Sort((a, b) => a.Fitness > b.Fitness ? -1 : 1);
        }

        // In all 300 test cases the correct BPM value was part of the top 3 choices,
        // so it seems reasonable to discard anything below the top 3 as irrelevant.
        // mattmora - will take a few more just in case
        Debug.Log("Results");
        if (Results.Count > 5) Results = Results.Take(5).ToList();
    }


    // ================================================================================================
    // Offset testing

    private void ComputeSlopes(float[] samples, double[] output, int numFrames, int samplerate)
    {
        //memset(out, 0, sizeof(real)* numFrames);

        var wh = samplerate / 20;
        if (numFrames < wh * 2) return;

        // Initial sums of the left/right side of the window.
        double sumL = 0, sumR = 0;
        for (int i = 0, j = wh; i < wh; ++i, ++j)
        {
            sumL += Math.Abs(samples[i]);
            sumR += Math.Abs(samples[j]);
        }

        // Slide window over the samples.
        var scalar = 1.0 / wh;
        for (int i = wh, end = numFrames - wh; i < end; ++i)
        {
            // Determine slope value.
            output[i] = Math.Max(0.0, (sumR - sumL) * scalar);

            // Move window.
            var cur = Math.Abs(samples[i]);
            sumL -= Math.Abs(samples[i - wh]);
            sumL += cur;
            sumR -= cur;
            sumR += Math.Abs(samples[i + wh]);
        }
    }

    // Returns the most promising offset for the given BPM value.
    private double GetBaseOffsetValue(GapData gapData, int sampleRate, double bpm)
    {
        var numOnsets = gapData.NumOnsets;
        var onsets = gapData.Onsets;

        var wrappedPos = gapData.WrappedPos;
        var wrappedOnsets = gapData.WrappedOnsets;
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        // Make a histogram of onset strengths for every position in the interval.
        var intervalf = sampleRate * 60.0 / bpm;
        var interval = (int)(intervalf + 0.5);
        //memset(wrappedOnsets, 0, sizeof(real_t) * interval);
        for (var i = 0; i < numOnsets; ++i)
        {
            var pos = (int)(onsets[i].Position % intervalf);
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += 1.0;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0;
        var offsetPos = 0;
        for (var i = 0; i < numOnsets; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = gapData.GapConfidence(pos, interval);
            var offbeatPos = (pos + (interval / 2)) % interval;
            confidence += gapData.GapConfidence(offbeatPos, interval) * 0.5;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
                offsetPos = pos;
            }
        }

        return (double)offsetPos / sampleRate;
    }

    // Compares each offset to its corresponding offbeat value, and selects the most promising one.
    private double AdjustForOffbeats(double offset, double bpm)
    {
        // Create a slope representation of the waveform.
        var slopes = new double[NumFrames];
        ComputeSlopes(Samples, slopes, NumFrames, SampleRate);

        // Determine the offbeat sample position.
        var secondsPerBeat = 60.0 / bpm;
        var offbeat = offset + (secondsPerBeat * 0.5);
        if (offbeat > secondsPerBeat) offbeat -= secondsPerBeat;

        // Calculate the support for both sample positions.
        var end = (double)NumFrames;
        var interval = secondsPerBeat * SampleRate;
        var posA = offset * SampleRate;
        var posB = offbeat * SampleRate;
        var sumA = 0.0;
        var sumB = 0.0;
        for (; posA < end && posB < end; posA += interval, posB += interval)
        {
            sumA += slopes[(int)posA];
            sumB += slopes[(int)posB];
        }

        // Return the offset with the highest support.
        return (sumA >= sumB) ? offset : offbeat;
    }

    // Selects the best offset value for each of the BPM candidates.
    private void CalculateOffset(List<Onset> onsets)
    {
        // Create gapdata buffers for testing.
        var maxInterval = 0.0;
        foreach (var result in Results) maxInterval = Math.Max(maxInterval, SampleRate * 60.0 / result.BPM);
        var gapData = new GapData((int)(maxInterval + 1.0), 1, onsets);

        // Fill in onset values for each BPM.
        foreach (var result in Results)
            result.Offset = (float)GetBaseOffsetValue(gapData, SampleRate, result.BPM);

        // Test all onsets against their offbeat values, pick the best one.
        foreach (var result in Results)
        {
            result.Offset = (float)AdjustForOffbeats(result.Offset, result.BPM);
            if (result.Offset > result.Beat / 2f) result.Offset -= result.Beat; 
        }
    }
}
