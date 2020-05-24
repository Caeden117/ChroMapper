using DSPLib;
using System;
using System.Numerics;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    // first value is a "sample colums" and each of column have sample data
    // it's used for making nice geometric stuff
    public static float[] _bands { get; private set; } = new float[] { 0, 100, 200, 300, 400, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11500, 12000, 12500, 13000, 13500, 14000, 14500, 15000, 15500, 16000, 16500, 17000, 17500, 18000, 18500, 19000, 19500, 20000, };

    [SerializeField] private AudioSource _audioSource;

    public static int MAX_THREADS = 4;
    public static int SAMPLE_COUNT = 4096;
    public int ColumnsPerChunk = 300;

    private float secondPerChunk = float.NaN;

    private WaveformCache.WaveformData waveformData;
    private ConcurrentQueue<int> chunkQueue = new ConcurrentQueue<int>();
    public ConcurrentQueue<int> chunksComplete = new ConcurrentQueue<int>();

    private List<Thread> backgroundThreads = new List<Thread>();
    private float[] emptyArr = new float[_bands.Length - 1];

    // Information about the audio track to be used in threads
    private float[] multiChannelSamples;
    private int numChannels;
    private int numTotalSamples;
    private int sampleRate;
    private float clipLength;

    // Cached FFT data
    private float[] preProcessedSamples;
    private double[] windowCoefs;
    private double scaleFactor;
    private float hzStep;
    private float sampleOffset;

    public void SetSecondPerChunk(float secondPerChunk)
    {
        this.secondPerChunk = secondPerChunk;
    }

    public bool IsAlive()
    {
        return backgroundThreads.Any(it => it.IsAlive);
    }

    // We use the unity api on the main thread to pull data to be processed in the background
    public void Begin(AudioClip audioClip, WaveformCache.WaveformData waveformData)
    {
        this.waveformData = waveformData;

        numChannels = audioClip.channels;
        numTotalSamples = audioClip.samples;
        clipLength = audioClip.length;
        sampleRate = audioClip.frequency;

        multiChannelSamples = new float[numTotalSamples * numChannels];
        audioClip.GetData(multiChannelSamples, 0);

        Debug.Log("WaveformGenerator: Starting Background Thread");

        Thread bgThread = new Thread(preProcessData);
        backgroundThreads.Add(bgThread);
        bgThread.Start();
    }

    // This will be triggered if the game wants to close or we're exiting to the main menu
    // Remove all chunks from the queue so the background threads quit
    void OnDestroy()
    {
        int dummy;
        while (chunkQueue.TryDequeue(out dummy)) { }
    }

    public void preProcessData()
    {
        waveformData.chunks = (int) Math.Ceiling(clipLength / secondPerChunk);
        preProcessedSamples = new float[this.numTotalSamples];

        // Average all audio channels together
        for (int i = 0; i < multiChannelSamples.Length; i++)
        {
            preProcessedSamples[i / numChannels] = multiChannelSamples[i] / numChannels;
        }

        Debug.Log("WaveformGenerator: Combine Channels done " + preProcessedSamples.Length);

        for (int i = waveformData.chunksLoaded; i < waveformData.chunks; i++)
        {
            chunkQueue.Enqueue(i);
        }

        // How many audio samples wide a column is, we will likely use more samples
        // than the width to perform the FFT which has a smoothing effect
        sampleOffset = ((secondPerChunk / (float)ColumnsPerChunk) * sampleRate);

        // Precalculate the window function for our sample size
        windowCoefs = DSP.Window.Coefficients(DSP.Window.Type.BH92, (uint)SAMPLE_COUNT);
        scaleFactor = DSP.Window.ScaleFactor.Signal(windowCoefs);

        hzStep = sampleRate / (float)SAMPLE_COUNT;
        waveformData.initBandVolumes(ColumnsPerChunk);

        // Try and leave at least one cpu core for the main thread, limited by MAX_THREADS
        int threadCount = Math.Max(1, Math.Min(Environment.ProcessorCount - 1, MAX_THREADS));
        for (int i = 0; i < threadCount; i++)
        {
            Thread FFTThread = new Thread(performFFTThreaded);
            backgroundThreads.Add(FFTThread);
            FFTThread.Start();
        }

        Debug.Log("Background Thread Completed");
    }

    public void performFFTThreaded()
    {
        FFT fft = new FFT();
        fft.Initialize((uint)SAMPLE_COUNT);
        double[] sampleChunk = new double[SAMPLE_COUNT];

        int chunkId;
        while (chunkQueue.TryDequeue(out chunkId))
        {
            for (int k = 0; k < ColumnsPerChunk; k++)
            {
                int i = (chunkId * ColumnsPerChunk) + k;
                // Grab the current chunk of audio sample data
                int curSampleSize = SAMPLE_COUNT;
                if (i * sampleOffset + SAMPLE_COUNT > preProcessedSamples.Length)
                {
                    // We've reached the end of the track, pad with empty data
                    waveformData.bandVolumes[i] = emptyArr;
                    continue;
                }
                Array.Copy(preProcessedSamples, (int)(i * sampleOffset), sampleChunk, 0, curSampleSize);

                // Apply our chosen FFT Window
                double[] scaledSpectrumChunk = DSP.Math.Multiply(sampleChunk, windowCoefs);

                // Perform the FFT and convert output (complex numbers) to Magnitude
                Complex[] fftSpectrum = fft.Execute(scaledSpectrumChunk);
                double[] scaledFFTSpectrum = DSPLib.DSP.ConvertComplex.ToMagnitude(fftSpectrum);
                scaledFFTSpectrum = DSP.Math.Multiply(scaledFFTSpectrum, scaleFactor);

                float[] bandVolumes = new float[_bands.Length - 1];
                for (int j = 1; j < _bands.Length; j++)
                {
                    bandVolumes[j - 1] = bandVol(_bands[j - 1], _bands[j], scaledFFTSpectrum, hzStep);
                }

                waveformData.bandVolumes[i] = bandVolumes;
            }
            chunksComplete.Enqueue(chunkId);
        }

        Debug.Log("FFT Thread Completed");
    }

    // Groups FFT samples to form the spectogram rendering, output values from 0-220ish
    public static float bandVol(float fLow, float fHigh, double[] samples, float hzStep)
    {
        int samples_count = Mathf.RoundToInt((fHigh - fLow) / hzStep);
        int firtSample = Mathf.RoundToInt(fLow / hzStep);
        int lastSample = Mathf.Min(firtSample + samples_count, SAMPLE_COUNT - 1);

        double sum = 0;
        // This isn't an average but it appears to work fairly well
        for (int i = firtSample; i <= lastSample; i++) sum += samples[i];
        return Math.Max(0, (float)sum * Mathf.Sqrt(fLow + fHigh) * 2);
    }
}
