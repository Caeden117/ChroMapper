using System;
using UnityEngine;
public class AudioManager : MonoBehaviour
{
    private static readonly int SAMPLE_SIZE = Shader.PropertyToID("SampleSize");
    private static readonly int PROCESSING_OFFSET = Shader.PropertyToID("ProcessingOffset");

    private static readonly int FFT_SIZE = Shader.PropertyToID("FFTSize");
    private static readonly int FFT_COUNT = Shader.PropertyToID("FFTCount");
    private static readonly int FFT_FREQUENCY = Shader.PropertyToID("FFTFrequency");
    private static readonly int FFT_SCALE_FACTOR = Shader.PropertyToID("FFTScaleFactor");

    private static readonly int MULTIPLY_A = Shader.PropertyToID("A");
    private static readonly int MULTIPLY_B = Shader.PropertyToID("B");
    private static readonly int MULTIPLY_RESULTS = Shader.PropertyToID("Results");

    private static readonly int FFT_REAL = Shader.PropertyToID("Real");
    private static readonly int FFT_IMAGINARY = Shader.PropertyToID("Imaginary");
    private static readonly int FFT_RESULTS = Shader.PropertyToID("FFTResults");
    
    [SerializeField] private ComputeShader multiplyShader;
    [SerializeField] private ComputeShader fftShader;

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

        var sampleCount = SampleBufferManager.MonoSampleCount;
        var fftSize = sampleSize / 2;
        var fftCount = sampleCount * quality;

        // Generate window coefficients and signal scale factor
        var window = WindowCoefficients.GetWindowForSize(sampleSize);
        var signal = WindowCoefficients.Signal(window);
        
        // Set global sahder variables
        Shader.SetGlobalInt(AudioManager.SAMPLE_SIZE, sampleSize);
        Shader.SetGlobalInt(AudioManager.FFT_SIZE, fftSize);
        Shader.SetGlobalInt(AudioManager.FFT_COUNT, fftCount);
        Shader.SetGlobalFloat(FFT_SCALE_FACTOR, (float)signal);
        Shader.SetGlobalFloat(FFT_FREQUENCY, clip.frequency * quality);

        cachedFFTBuffer = new ComputeBuffer(fftCount, sizeof(float));
        Shader.SetGlobalBuffer(FFT_RESULTS, cachedFFTBuffer);

        // Step 1: Prepare real components of our FFT by multiply song samples by window coefficients for FFT
        using ComputeBuffer windowedSamples = new(fftCount, sizeof(float));

        //using(ComputeBuffer expandedSamples = new(fftCount, sizeof(float)))
        using(ComputeBuffer windowCoeffBuffer = new(sampleSize, sizeof(float)))
        {
            for(var i = 0; i < sampleCount; i += sampleSize / quality)
            {
                var length = Mathf.Clamp(sampleCount - i, 0, sampleSize);
                windowedSamples.SetData(SampleBufferManager.MonoSamples, i, i * quality, length);
            }

            windowCoeffBuffer.SetData(window);

            multiplyShader.SetBuffer(0, MULTIPLY_A, windowedSamples);
            multiplyShader.SetBuffer(0, MULTIPLY_B, windowCoeffBuffer);
            //multiplyShader.SetBuffer(0, MULTIPLY_RESULTS, windowedSamples);

            ExecuteOverLargeArray(multiplyShader, fftCount);
        }

        // Step 2: Execute FFT
        using ComputeBuffer imaginaryBuffer = new(fftCount, sizeof(float));

        fftShader.SetBuffer(0, FFT_REAL, windowedSamples);
        fftShader.SetBuffer(0, FFT_IMAGINARY, imaginaryBuffer);
        
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
        for(var i = 0; i < length; i += elementStep)
        {
            elementStep = Mathf.Clamp(length - i, 0, maxThreadCount);

            shader.SetInt(PROCESSING_OFFSET, i);
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
