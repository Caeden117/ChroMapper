using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static readonly int sampleSize = Shader.PropertyToID("SampleSize");
    private static readonly int processingOffset = Shader.PropertyToID("ProcessingOffset");

    private static readonly int fftSize = Shader.PropertyToID("FFTSize");
    private static readonly int fftCount = Shader.PropertyToID("FFTCount");
    private static readonly int fftFrequency = Shader.PropertyToID("FFTFrequency");
    private static readonly int fftScaleFactor = Shader.PropertyToID("FFTScaleFactor");

    private static readonly int multiplyA = Shader.PropertyToID("A");
    private static readonly int multiplyB = Shader.PropertyToID("B");

    private static readonly int initializeBuffer = Shader.PropertyToID("BufferToInitialize");

    private static readonly int fftReal = Shader.PropertyToID("Real");
    private static readonly int fftImaginary = Shader.PropertyToID("Imaginary");
    private static readonly int fftResults = Shader.PropertyToID("FFTResults");
    
    [SerializeField] private ComputeShader multiplyShader;
    [SerializeField] private ComputeShader fftShader;
    [SerializeField] private ComputeShader initializeShader;

    private ComputeBuffer cachedFFTBuffer;
    
    // ReSharper disable ParameterHidesMember
    // ReSharper disable LocalVariableHidesMember
    public void GenerateFFT(AudioClip clip, int sampleSize, int quality)
    {
        if (SampleBufferManager.MonoSamples == null)
        {
            throw new InvalidOperationException("remember to call SampleBufferManager first, thanks.");
        }

        ClearFFTCache();

        // Reduce spectrogram quality if it would exceed max buffer size 
        var sampleCount = SampleBufferManager.MonoSampleCount;
        while ((long)sampleCount * quality * sizeof(float) > SystemInfo.maxGraphicsBufferSize)
        {
            if (quality < 1)
            {
                Debug.LogWarning("Audio file is too large to display spectrogram.");
                return;
            }
            
            quality /= 2;
            Debug.Log($"FFT buffer exceeded. Reduced spectrogram quality to: {quality}");
        }
        
        var fftSize = sampleSize / 2;
        var fftCount = sampleCount * quality;

        // Generate window coefficients and signal scale factor
        var window = WindowCoefficients.GetWindowForSize(sampleSize);
        var signal = WindowCoefficients.Signal(window);
        
        // Set global shader variables
        Shader.SetGlobalInt(AudioManager.sampleSize, sampleSize);
        Shader.SetGlobalInt(AudioManager.fftSize, fftSize);
        Shader.SetGlobalInt(AudioManager.fftCount, fftCount);
        Shader.SetGlobalFloat(fftScaleFactor, signal);
        Shader.SetGlobalFloat(fftFrequency, clip.frequency * quality);

        cachedFFTBuffer = new ComputeBuffer(fftCount, sizeof(float));
        Shader.SetGlobalBuffer(fftResults, cachedFFTBuffer);

        // Step 1: Prepare real components of our FFT by multiply song samples by window coefficients for FFT
        using ComputeBuffer windowedSamples = new(fftCount, sizeof(float));
        using (ComputeBuffer windowCoeffBuffer = new(sampleSize, sizeof(float)))
        {
            for(var i = 0; i < sampleCount; i += sampleSize / quality)
            {
                var length = Mathf.Clamp(sampleCount - i, 0, sampleSize);
                windowedSamples.SetData(SampleBufferManager.MonoSamples, i, i * quality, length);
            }

            windowCoeffBuffer.SetData(window);

            multiplyShader.SetBuffer(0, multiplyA, windowedSamples);
            multiplyShader.SetBuffer(0, multiplyB, windowCoeffBuffer);

            ExecuteOverLargeArray(multiplyShader, fftCount);
        }

        // Step 2: Prepare imaginary components of our FFT by initializing the entire buffer to 0
        using ComputeBuffer imaginaryBuffer = new(fftCount, sizeof(float));
        initializeShader.SetBuffer(0, initializeBuffer, imaginaryBuffer);
        ExecuteOverLargeArray(initializeShader, fftCount);

        // Step 3: Execute FFT
        fftShader.SetBuffer(0, fftReal, windowedSamples);
        fftShader.SetBuffer(0, fftImaginary, imaginaryBuffer);
        
        ExecuteOverLargeArray(fftShader, fftCount / sampleSize);
    }
    // ReSharper restore ParameterHidesMember
    // ReSharper restore LocalVariableHidesMember

    // if GPU threads >= 65535, exception is thrown.
    // this usually happens when our buffers get too big (quality go brrrr)
    // fix this by executing the shader in steps, adding the processed offset as a shader variable so we can
    //   correct for the offset.
    private static void ExecuteOverLargeArray(ComputeShader shader, int length)
    {
        const int maxThreadCount = 65535;

        shader.GetKernelThreadGroupSizes(0, out var x, out var y, out var z);
        var kernelGroupArea = (int)(x * y * z);

        int elementStep;
        for (var i = 0; i < length; i += elementStep)
        {
            elementStep = Mathf.Clamp(length - i, 0, maxThreadCount);

            shader.SetInt(processingOffset, i);
            shader.Dispatch(0, elementStep / kernelGroupArea, 1, 1);
        }
    }

    private void ClearFFTCache()
    {
        if (cachedFFTBuffer == null) return;
        
        cachedFFTBuffer.Dispose();
        cachedFFTBuffer = null;
    }

    private void OnDestroy() => ClearFFTCache();
}
