using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DSPLib;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    public static int MAXThreads = 4;
    public int ColumnsPerChunk = 300;
    public bool FillEmptySpaceWithTransparency = true;
    [FormerlySerializedAs("chunksComplete")] public ConcurrentQueue<int> ChunksComplete = new ConcurrentQueue<int>();

    private readonly List<Thread> backgroundThreads = new List<Thread>();
    private readonly List<int> chunkQueue = new List<int>();
    private readonly float[] emptyArr = new float[Bands.Length - 1];

    private AudioTimeSyncController atsc;
    private int chunkSize;
    private float clipLength;
    private float hzStep;
    private bool is3d;

    // Information about the audio track to be used in threads
    private float[] multiChannelSamples;
    private int numChannels;
    private int numTotalSamples;

    // Cached FFT data
    private double[] preProcessedSamples;
    private float sampleOffset;
    private int sampleRate;
    private double scaleFactor;

    private float secondPerChunk = float.NaN;
    private Gradient spectrogramHeightGradient;

    private WaveformData waveformData;

    private double[] windowCoefs;

    // first value is a "sample colums" and each of column have sample data
    // it's used for making nice geometric stuff
    public static float[] Bands { get; } =
    {
        0, 100, 200, 300, 400, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000,
        7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11500, 12000, 12500, 13000, 13500, 14000, 14500, 15000,
        15500, 16000, 16500, 17000, 17500, 18000, 18500, 19000, 19500, 20000
    };

    // This will be triggered if the game wants to close or we're exiting to the main menu
    // Remove all chunks from the queue so the background threads quit
    internal void OnDestroy()
    {
        lock (chunkQueue)
        {
            chunkQueue.Clear();
        }
    }

    public uint GetSampleCount() => is3d ? 4096u : 512u;

    public void SetSecondPerChunk(float secondPerChunk) => this.secondPerChunk = secondPerChunk;

    public bool IsAlive()
    {
        // return backgroundThreads.Any(it => it.IsAlive); // Silly TopCat, this can be modified from other threads while running.
        for (var i = 0; i < backgroundThreads.Count; i++)
        {
            if (backgroundThreads[i].IsAlive)
                return true;
        }

        return false;
    }

    // We use the unity api on the main thread to pull data to be processed in the background
    public void Begin(bool is3d, Gradient spectrogramHeightGradient, AudioClip audioClip, WaveformData waveformData,
        AudioTimeSyncController atsc, int chunkSize)
    {
        this.spectrogramHeightGradient = spectrogramHeightGradient;
        this.is3d = is3d;
        this.waveformData = waveformData;
        this.atsc = atsc;
        this.chunkSize = chunkSize;

        numChannels = audioClip.channels;
        numTotalSamples = audioClip.samples;
        clipLength = audioClip.length;
        sampleRate = audioClip.frequency;

        multiChannelSamples = new float[numTotalSamples * numChannels];
        audioClip.GetData(multiChannelSamples, 0);

        waveformData.Chunks = (int)Math.Ceiling(clipLength / secondPerChunk);

        // How many audio samples wide a column is, we will likely use more samples
        // than the width to perform the FFT which has a smoothing effect
        if (is3d)
        {
            sampleOffset = secondPerChunk / ColumnsPerChunk * sampleRate;
            waveformData.InitBandVolumes(ColumnsPerChunk, Bands.Length - 1);
        }
        else
        {
            var samples = (int)GetSampleCount() / 2;
            var samplesPerChunk = sampleRate * secondPerChunk;
            ColumnsPerChunk = (int)samplesPerChunk / samples;
            sampleOffset = samplesPerChunk / ColumnsPerChunk;
            waveformData.InitBandVolumes(ColumnsPerChunk, samples + 1);
        }

        Debug.Log("WaveformGenerator: Starting Background Thread");

        var bgThread = new Thread(PreProcessData);
        backgroundThreads.Add(bgThread);
        bgThread.Start();
    }

    // Render 4 times more chunks in the future than the past
    private int GetPriority(int val)
    {
        if (val == int.MaxValue) return int.MaxValue;

        if (atsc != null) val -= (int)atsc.CurrentBeat / chunkSize;

        if (val < 0) return -val * 4;
        return val;
    }

    private bool GetNextChunkToRender(out int result)
    {
        lock (chunkQueue)
        {
            if (chunkQueue.Count == 0)
            {
                result = 0;
                return false;
            }

            result = chunkQueue.Aggregate(int.MaxValue,
                (curMin, x) => GetPriority(x) < GetPriority(curMin) ? x : curMin);
            chunkQueue.Remove(result);
            return true;
        }
    }

    public void PreProcessData()
    {
        preProcessedSamples = new double[numTotalSamples];

        // Average all audio channels together
        // divide by 1.5 to roughly match amplitude to previous broken behavior (not just 2 because of some destructive interference)
        for (var i = 0; i < multiChannelSamples.Length; i++)
            preProcessedSamples[i / numChannels] += multiChannelSamples[i] / numChannels / 1.5;

        Debug.Log("WaveformGenerator: Combine Channels done " + preProcessedSamples.Length);

        for (var i = 0; i < waveformData.Chunks; i++) chunkQueue.Add(i);

        // Precalculate the window function for our sample size
        windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.BH92, GetSampleCount());
        scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

        hzStep = sampleRate / (float)GetSampleCount();

        // Try and leave at least one cpu core for the main thread, limited by MAX_THREADS
        var threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, MAXThreads));
        for (var i = 0; i < threadCount; i++)
        {
            var fftThread = new Thread(PerformFFTThreaded);
            // Calling this outside of the main Unity thread can cause a InvalidOperationException in IsAlive().
            backgroundThreads.Add(fftThread);
            fftThread.Start();
        }

        Debug.Log("Background Thread Completed");
    }

    public void PerformFFTThreaded()
    {
        var fftSize = GetSampleCount();
        var fft = new FFT();
        fft.Initialize(fftSize);
        var sampleChunk = new double[fftSize];

        var bins = fftSize / 2;
        var compFactors = new double[bins];

        if (!is3d)
        {
            // Precompute factors for spectrogram frequency compensation
            var scalingConstant = 8d / fftSize;
            for (var y = 0; y < bins; y++) compFactors[y] = Math.Sqrt((y + 0.25) * scalingConstant);
        }

        while (GetNextChunkToRender(out var chunkId))
        {
            var bandColors = new Color[ColumnsPerChunk][];
            for (var k = 0; k < ColumnsPerChunk; k++)
            {
                var i = (chunkId * ColumnsPerChunk) + k;
                // Grab the current chunk of audio sample data
                var curSampleSize = (int)fftSize;
                if ((i * sampleOffset) + fftSize > preProcessedSamples.Length)
                {
                    // We've reached the end of the track, pad with empty data
                    if (is3d)
                    {
                        waveformData.BandVolumes[i] = emptyArr;
                    }
                    else
                    {
                        waveformData.BandVolumes[i] = new float[bins + 1];
                        if (FillEmptySpaceWithTransparency)
                        {
                            bandColors[k] = new Color[bins + 1];
                        }
                        else
                        {
                            bandColors[k] = Enumerable.Repeat(spectrogramHeightGradient.Evaluate(0f), (int)bins + 1)
                                .ToArray();
                        }
                    }

                    continue;
                }

                Buffer.BlockCopy(preProcessedSamples, (int)(i * sampleOffset) * sizeof(double), sampleChunk, 0,
                    curSampleSize * sizeof(double));

                // Apply our chosen FFT Window
                var scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);

                // Perform the FFT and convert output (complex numbers) to Magnitude
                var fftSpectrum = fft.Execute(scaledSpectrumChunk);
                var scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
                scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

                if (is3d)
                {
                    var bandVolumes = new float[Bands.Length - 1];
                    for (var j = 1; j < Bands.Length; j++)
                        bandVolumes[j - 1] = BandVol(Bands[j - 1], Bands[j], scaledFFTSpectrum, hzStep);
                    waveformData.BandVolumes[i] = bandVolumes;
                }
                else
                {
                    // Compensate for frequency bin
                    for (var y = 0; y < bins; y++) scaledFFTSpectrum[y] *= compFactors[y];

                    var gradientFactor = 25;
                    waveformData.BandVolumes[i] = scaledFFTSpectrum.Select(it =>
                    {
                        if (it >= Math.Pow(Math.E, -255d / gradientFactor))
                            return (float)((Math.Log(it) + (255d / gradientFactor)) * gradientFactor) / 128f;
                        return 0f;
                    }).ToArray();
                    bandColors[k] = waveformData.BandVolumes[i].Select(it =>
                    {
                        var lerp = Mathf.InverseLerp(0, 2, it);
                        return spectrogramHeightGradient.Evaluate(lerp);
                    }).ToArray();
                }
            }

            if (!is3d)
            {
                // Render 2d texture with spectogram for entire chunk
                var data = waveformData.BandCData[chunkId];

                try
                {
                    var index = 0;
                    if (bandColors == null) return;
                    for (var y = 0; y < bandColors[0].Length; y++)
                    {
                        for (var x = 0; x < bandColors.Length; x++)
                            data[index++] = bandColors[x][y];
                    }
                }
                catch (NullReferenceException)
                {
                    // Cancelled some other way
                }
                catch (InvalidOperationException)
                {
                    // NativeArray has been deallocated :(
                }
            }

            ChunksComplete.Enqueue(chunkId);
            waveformData.ProcessedChunks++;
        }

        Debug.Log("FFT Thread Completed");
    }

    // Groups FFT samples to form the spectogram rendering, output values from 0-220ish
    public static float BandVol(float fLow, float fHigh, double[] samples, float hzStep)
    {
        var samplesCount = Mathf.RoundToInt((fHigh - fLow) / hzStep);
        var firtSample = Mathf.RoundToInt(fLow / hzStep);
        var lastSample = Mathf.Min(firtSample + samplesCount, (samples.Length * 2) - 3);

        double sum = 0;
        // This isn't an average but it appears to work fairly well
        for (var i = firtSample; i <= lastSample; i++) sum += samples[i];
        return Math.Max(0, (float)sum * Mathf.Sqrt(fLow + fHigh) * 2);
    }
}
