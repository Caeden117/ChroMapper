using DSPLib;
using System;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    // first value is a "sample colums" and each of column have sample data
    // it's used for making nice geometric stuff
    public static float[] _bands { get; private set; } = new float[] { 0, 100, 200, 300, 400, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11500, 12000, 12500, 13000, 13500, 14000, 14500, 15000, 15500, 16000, 16500, 17000, 17500, 18000, 18500, 19000, 19500, 20000, };

    public static int MAX_THREADS = 4;
    public int ColumnsPerChunk = 300;
    public bool FillEmptySpaceWithTransparency = true;

    private float secondPerChunk = float.NaN;

    private WaveformData waveformData;
    private readonly List<int> chunkQueue = new List<int>();
    public ConcurrentQueue<int> chunksComplete = new ConcurrentQueue<int>();

    private readonly List<Thread> backgroundThreads = new List<Thread>();
    private readonly float[] emptyArr = new float[_bands.Length - 1];

    // Information about the audio track to be used in threads
    private float[] multiChannelSamples;
    private int numChannels;
    private int numTotalSamples;
    private int sampleRate;
    private float clipLength;

    // Cached FFT data
    private double[] preProcessedSamples;
    private double[] windowCoefs;
    private double scaleFactor;
    private float hzStep;
    private float sampleOffset;

    private AudioTimeSyncController atsc;
    private int chunkSize;
    private bool is3d = false;
    private Gradient spectrogramHeightGradient;

    public uint GetSampleCount()
    {
        return is3d ? 4096u : 512u;
    }

    public void SetSecondPerChunk(float secondPerChunk)
    {
        this.secondPerChunk = secondPerChunk;
    }

    public bool IsAlive()
    {
        // return backgroundThreads.Any(it => it.IsAlive); // Silly TopCat, this can be modified from other threads while running.
        for (int i = 0; i < backgroundThreads.Count; i++)
        {
            if (backgroundThreads[i].IsAlive) return true;
        }
        return false;
    }

    // We use the unity api on the main thread to pull data to be processed in the background
    public void Begin(bool is3d, Gradient spectrogramHeightGradient, AudioClip audioClip, WaveformData waveformData, AudioTimeSyncController atsc, int chunkSize)
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
            waveformData.InitBandVolumes(ColumnsPerChunk, _bands.Length - 1);
        }
        else
        {
            int samples = (int) GetSampleCount() / 2;
            float samplesPerChunk = sampleRate * secondPerChunk;
            ColumnsPerChunk = (int)samplesPerChunk / samples;
            sampleOffset = samplesPerChunk / ColumnsPerChunk;
            waveformData.InitBandVolumes(ColumnsPerChunk, samples + 1);
        }

        Debug.Log("WaveformGenerator: Starting Background Thread");

        Thread bgThread = new Thread(PreProcessData);
        backgroundThreads.Add(bgThread);
        bgThread.Start();
    }

    // This will be triggered if the game wants to close or we're exiting to the main menu
    // Remove all chunks from the queue so the background threads quit
    internal void OnDestroy()
    {
        lock (chunkQueue)
        {
            chunkQueue.Clear();
        }
    }

    // Render 4 times more chunks in the future than the past
    int GetPriority(int val)
    {
        if (val == int.MaxValue)
        {
            return int.MaxValue;
        }

        if (atsc != null)
        {
            val -= (int)atsc.CurrentBeat / chunkSize;
        }

        if (val < 0)
        {
            return -val * 4;
        }
        return val;
    }

    bool GetNextChunkToRender(out int result)
    {
        lock (chunkQueue)
        {
            if (chunkQueue.Count == 0)
            {
                result = 0;
                return false;
            }

            result = chunkQueue.Aggregate(int.MaxValue, (curMin, x) => (GetPriority(x) < GetPriority(curMin) ? x : curMin));
            chunkQueue.Remove(result);
            return true;
        }
    }

    public void PreProcessData()
    {
        preProcessedSamples = new double[numTotalSamples];

        // Average all audio channels together
        for (int i = 0; i < multiChannelSamples.Length; i++)
        {
            preProcessedSamples[i / numChannels] = multiChannelSamples[i] / numChannels;
        }

        Debug.Log("WaveformGenerator: Combine Channels done " + preProcessedSamples.Length);

        for (int i = 0; i < waveformData.Chunks; i++)
        {
            chunkQueue.Add(i);
        }

        // Precalculate the window function for our sample size
        windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.BH92, GetSampleCount());
        scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

        hzStep = sampleRate / (float)GetSampleCount();

        // Try and leave at least one cpu core for the main thread, limited by MAX_THREADS
        int threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, MAX_THREADS));
        for (int i = 0; i < threadCount; i++)
        {
            Thread FFTThread = new Thread(PerformFFTThreaded);
            // Calling this outside of the main Unity thread can cause a InvalidOperationException in IsAlive().
            backgroundThreads.Add(FFTThread);
            FFTThread.Start();
        }

        Debug.Log("Background Thread Completed");
    }

    public void PerformFFTThreaded()
    {
        uint fftSize = GetSampleCount();
        FFT fft = new FFT();
        fft.Initialize(fftSize);
        double[] sampleChunk = new double[fftSize];

        uint bins = fftSize / 2;
        double[] compFactors = new double[bins];

        if (!is3d)
        {
            // Precompute factors for spectrogram frequency compensation
            double scalingConstant = 8d / fftSize;
            for (int y = 0; y < bins; y++)
            {
                compFactors[y] = Math.Sqrt((y + 0.25) * scalingConstant);
            }
        }

        while (GetNextChunkToRender(out int chunkId))
        {
            Color[][] bandColors = new Color[ColumnsPerChunk][];
            for (int k = 0; k < ColumnsPerChunk; k++)
            {
                int i = (chunkId * ColumnsPerChunk) + k;
                // Grab the current chunk of audio sample data
                int curSampleSize = (int) fftSize;
                if (i * sampleOffset + fftSize > preProcessedSamples.Length)
                {
                    // We've reached the end of the track, pad with empty data
                    if (is3d)
                    {
                        waveformData.BandVolumes[i] = emptyArr;
                    } else
                    {
                        waveformData.BandVolumes[i] = new float[bins + 1];
                        if (FillEmptySpaceWithTransparency)
                        {
                            bandColors[k] = new Color[bins + 1];
                        }
                        else
                        {
                            bandColors[k] = Enumerable.Repeat(spectrogramHeightGradient.Evaluate(0f), (int)bins + 1).ToArray();
                        }
                    }
                    continue;
                }
                Buffer.BlockCopy(preProcessedSamples, (int)(i * sampleOffset) * sizeof(double), sampleChunk, 0, curSampleSize * sizeof(double));

                // Apply our chosen FFT Window
                double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);

                // Perform the FFT and convert output (complex numbers) to Magnitude
                Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
                double[] scaledFFTSpectrum = DSP.ConvertComplex.ToMagnitude(fftSpectrum);
                scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

                if (is3d)
                {
                    float[] bandVolumes = new float[_bands.Length - 1];
                    for (int j = 1; j < _bands.Length; j++)
                    {
                        bandVolumes[j - 1] = BandVol(_bands[j - 1], _bands[j], scaledFFTSpectrum, hzStep);
                    }
                    waveformData.BandVolumes[i] = bandVolumes;
                } else
                {
                    // Compensate for frequency bin
                    for (int y = 0; y < bins; y++)
                    {
                        scaledFFTSpectrum[y] *= compFactors[y];
                    }

                    int gradientFactor = 25;
                    waveformData.BandVolumes[i] = scaledFFTSpectrum.Select(it => {
                        if (it >= Math.Pow(Math.E, -255d / gradientFactor))
                        {
                            return (float) ((Math.Log(it) + (255d / gradientFactor)) * gradientFactor) / 128f;
                        }
                        return 0f;
                    }).ToArray();
                    bandColors[k] = waveformData.BandVolumes[i].Select(it =>
                    {
                        float lerp = Mathf.InverseLerp(0, 2, it);
                        return spectrogramHeightGradient.Evaluate(lerp);
                    }).ToArray();
                }
            }

            if (!is3d)
            {
                // Render 2d texture with spectogram for entire chunk
                var data = waveformData.BandCData[chunkId];

                try {
                    int index = 0;
                    if (bandColors == null) return;
                    for (int y = 0; y < bandColors[0].Length; y++)
                    {
                        for (int x = 0; x < bandColors.Length; x++)
                        {
                            data[index++] = bandColors[x][y];
                        }
                    }
                } catch (NullReferenceException) {
                    // Cancelled some other way
                } catch (InvalidOperationException) {
                    // NativeArray has been deallocated :(
                }
            }

            chunksComplete.Enqueue(chunkId);
            waveformData.ProcessedChunks++;
        }

        Debug.Log("FFT Thread Completed");
    }

    // Groups FFT samples to form the spectogram rendering, output values from 0-220ish
    public static float BandVol(float fLow, float fHigh, double[] samples, float hzStep)
    {
        int samples_count = Mathf.RoundToInt((fHigh - fLow) / hzStep);
        int firtSample = Mathf.RoundToInt(fLow / hzStep);
        int lastSample = Mathf.Min(firtSample + samples_count, (samples.Length * 2) - 3);

        double sum = 0;
        // This isn't an average but it appears to work fairly well
        for (int i = firtSample; i <= lastSample; i++) sum += samples[i];
        return Math.Max(0, (float)sum * Mathf.Sqrt(fLow + fHigh) * 2);
    }
}
