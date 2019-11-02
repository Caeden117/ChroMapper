using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thanks to SheetCode for being a huge help in making this work!
/// </summary>
public class AudioManager : MonoBehaviour
{
    public float _dbMulti = 50;

    // first value is a "sample colums" and each of column have sample data
    // it's used for making nice geometric stuff
    public static float[][] _bandVolumes;

    [SerializeField] private AudioSource _audioSource;

    public static int SAMPLE_COUNT = 1024;
    public int ColumnsPerChunk = 4096;
    public int SpectrogramFrequencyDensity = 32;

    private List<float> _bands;

    public void Start()
    {
        // creating range of bands, they should work flexible
        _bands = new List<float>() { };

        //I have created an Exponential Regression equation for the original frequency list: 44.1701 * 1.4056^x
        //(Ever wondered if math will ever be useful in life? Here ya go.)
        for (int i = 0; i < SpectrogramFrequencyDensity; i++)
            _bands.Add(Mathf.Ceil(44.1701f * Mathf.Pow(1.4056f, (float)i / SpectrogramFrequencyDensity * 18)));

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
        float hzStep = 20000 / SAMPLE_COUNT;
        int samples_count = Mathf.RoundToInt((fHigh - fLow) / hzStep);
        int firtSample = Mathf.RoundToInt(fLow / hzStep);
        int lastSample = Mathf.Min(firtSample + samples_count, SAMPLE_COUNT - 1);

        float sum = 0;
        // average the volumes of frequencies fLow to fHigh
        for (int i = firtSample; i <= lastSample; i++) sum += samples[i];
        return sum;
    }
}
