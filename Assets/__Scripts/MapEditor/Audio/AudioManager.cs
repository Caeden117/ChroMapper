using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * Original Spectrogram code by SheetCode: https://github.com/rfiedorowicz/UnityProjects 
 *
 * I am done with it. I will not make any further modifications. If you complain about it, I will leave you two choices:
 * 1) Fork ChroMapper (https://github.com/Caeden117/ChroMapper) and fix it yourself (I wish you good luck) or
 * 2) Kindly fuck off. 
 *
 * I will be very surprised if you can find a more viable spectrogram solution for Unity.
 */
public class AudioManager : MonoBehaviour
{
    public float _dbMulti = 50;

    // first value is a "sample colums" and each of column have sample data
    // it's used for making nice geometric stuff
    public static float[][] _bandVolumes;

    private static float[] processedSamples = new float[SAMPLE_COUNT];

    [SerializeField] private AudioSource _audioSource;

    public static int SAMPLE_COUNT = 8192;
    public int ColumnsPerChunk = 4096;
    public int SpectrogramFrequencyDensity = 32;

    private List<float> _bands;

    private void Awake()
    {
        processedSamples = new float[SAMPLE_COUNT];
        for (int i = 0; i < SAMPLE_COUNT; i++)
            processedSamples[i] = 0;
    }

    public void Start()
    {
        _bands = new List<float>();
        _bands = new List<float>() {-1, 0, 100, 200, 300, 400, 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 5500, 6000, 6500, 7000, 7500, 8000, 8500, 9000, 9500, 10000, 10500, 11000, 11500, 12000, 12500, 13000, 13500, 14000, 14500, 15000, 15500, 16000, 16500, 17000, 17500, 18000, 18500, 19000, 19500, 20000, };
        // creating range of bands, they should work flexible
        //I have created an Exponential Regression equation for the original frequency list: 44.1701 * 1.4056^x
        //(Ever wondered if math will ever be useful in life? Here ya go.)
        //for (int i = 0; i < SpectrogramFrequencyDensity; i++)
        //    _bands.Add(Mathf.Ceil(44.1701f * Mathf.Pow(1.4056f, (float)i / SpectrogramFrequencyDensity * 18)));

        _bandVolumes = new float[ColumnsPerChunk][];

        for (int i = 0; i < ColumnsPerChunk; i++)
            _bandVolumes[i] = new float[_bands.Count - 1]; // -1 beause bands are "from,to" like from 20 to 50

    }

    public void PopulateData()
    {
        // copying last values level "up"
        for (int i = ColumnsPerChunk - 1; i > 0; i--)
            Array.Copy(_bandVolumes[i - 1], _bandVolumes[i], _bands.Count - 1);

        // reading current samples
        float[] samples = new float[SAMPLE_COUNT];
        _audioSource.GetSpectrumData(samples, 0, FFTWindow.BlackmanHarris);

        //Smoothing code largely taken from beat saber but also simplified a lot
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float num = samples[i];
            processedSamples[i] = Mathf.Lerp(processedSamples[i], num, deltaTime * 128f);
        }

        float[] bandVolumes = new float[_bands.Count - 1];
        for (int i = 1; i < _bands.Count; i++)
        {
            float db = BandVol(_bands[i - 1], _bands[i], samples) * _dbMulti;
            bandVolumes[i - 1] = db;
        }

        _bandVolumes[0] = bandVolumes;
    }

    public static float BandVol(float fLow, float fHigh, float[] samples)
    {
        float hzStep = 22000f / SAMPLE_COUNT;
        int samples_count = Mathf.RoundToInt((fHigh - fLow) / hzStep);
        int firtSample = Mathf.RoundToInt(fLow / hzStep);
        int lastSample = Mathf.Min(firtSample + samples_count, SAMPLE_COUNT - 1);

        float sum = 0;
        // average the volumes of frequencies fLow to fHigh
        for (int i = firtSample; i <= lastSample; i++) sum += samples[i];
        return sum;
    }
}
