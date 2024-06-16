using System;
using Unity.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static readonly int sampleSize = Shader.PropertyToID("SampleSize");
    private static readonly int processingOffset = Shader.PropertyToID("ProcessingOffset");

    private static readonly int fftSize = Shader.PropertyToID("FFTSize");
    private static readonly int fftCount = Shader.PropertyToID("FFTCount");
    private static readonly int fftFrequency = Shader.PropertyToID("FFTFrequency");
    private static readonly int fftScaleFactor = Shader.PropertyToID("FFTScaleFactor");
    private static readonly int fftInitialized = Shader.PropertyToID("FFTInitialized");

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
    private ComputeBuffer dummyBuffer;

    // ReSharper disable ParameterHidesMember
    // ReSharper disable LocalVariableHidesMember
    public void GenerateFFT(AudioClip clip, int sampleSize, int quality)
    {
        if (SampleBufferManager.MonoSamples == null)
        {
            throw new InvalidOperationException("remember to call SampleBufferManager first, thanks.");
        }

        ClearFFTCache();

        var sampleCount = SampleBufferManager.MonoSampleCount;

        // TODO: Should we consider a CPU spectrogram fallback in cases where the GPU spectrogram would fail?

        // Reduce spectrogram quality if it would exceed max buffer size 
        while ((long)sampleCount * quality * sizeof(float) > SystemInfo.maxGraphicsBufferSize)
        {
            quality /= 2;
            Debug.Log($"FFT buffer exceeded. Reduced spectrogram quality to: {quality}");
        }
        if (quality < 1)
        {
            Debug.LogWarning("Audio file is too large to display spectrogram.");
            return;
        }

        // Reduce spectrogram quality if it would exceed half of total VRAM capacity
        //   (Some video memory should still be available for ChroMapper and other programs to still function)
        var videoMemoryBytes = SystemInfo.graphicsMemorySize * 1024L * 1024L;
        while ((long)sampleCount * quality * sizeof(float) > videoMemoryBytes)
        {
            quality /= 2;
            Debug.Log($"Video Memory exceeded. Reduced spectrogram quality to: {quality}");
        }
        if (quality < 1)
        {
            Debug.LogWarning("Audio file is too large to display spectrogram.");
            return;
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
        //   We allocate a temporary CPU buffer that will hold our real component data before copying to the GPU in one block.
        using var windowedSamples = new ComputeBuffer(fftCount, sizeof(float));
        using (var windowWrite = new NativeArray<float>(fftCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory))
        using (var windowCoeffBuffer = new ComputeBuffer(sampleSize, sizeof(float)))
        {   
            for (var i = 0; i < sampleCount; i += sampleSize / quality)
            {
                var length = Mathf.Clamp(sampleCount - i, 0, sampleSize);
                NativeArray<float>.Copy(SampleBufferManager.MonoSamples, i, windowWrite, i * quality, length);
            }

            windowedSamples.SetData(windowWrite);
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

        Shader.SetGlobalInt(fftInitialized, 1);
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

        Shader.SetGlobalInt(fftCount, 0);
        Shader.SetGlobalInt(fftInitialized, 0);
        Shader.SetGlobalBuffer(fftReal, dummyBuffer);
        Shader.SetGlobalBuffer(fftImaginary, dummyBuffer);
        Shader.SetGlobalBuffer(fftResults, dummyBuffer);
    }

    private void Awake()
    {
        dummyBuffer = new ComputeBuffer(1, sizeof(float));
        Shader.SetGlobalBuffer(fftReal, dummyBuffer);
        Shader.SetGlobalBuffer(fftImaginary, dummyBuffer);
        Shader.SetGlobalBuffer(fftResults, dummyBuffer);
    }

    private void OnDestroy()
    {
        ClearFFTCache();
        
        dummyBuffer.Dispose();
        dummyBuffer = null;
    }
}
