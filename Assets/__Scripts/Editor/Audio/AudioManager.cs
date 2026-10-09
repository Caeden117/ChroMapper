using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static readonly int sampleSize = Shader.PropertyToID("SampleSize");
    private static readonly int processingOffset = Shader.PropertyToID("ProcessingOffset");
    private static readonly int chunkOffset = Shader.PropertyToID("ChunkOffset");

    private static readonly int fftSize = Shader.PropertyToID("FFTSize");
    private static readonly int fftCount = Shader.PropertyToID("FFTCount");
    private static readonly int fftFrequency = Shader.PropertyToID("FFTFrequency");
    private static readonly int fftScaleFactor = Shader.PropertyToID("FFTScaleFactor");
    private static readonly int fftInitialized = Shader.PropertyToID("FFTInitialized");
    private static readonly int fftQuality = Shader.PropertyToID("FFTQuality");

    private static readonly int multiplyA = Shader.PropertyToID("A");
    private static readonly int multiplyB = Shader.PropertyToID("B");

    private static readonly int initializeBuffer = Shader.PropertyToID("BufferToInitialize");

    private static readonly int fftReal = Shader.PropertyToID("Real");
    private static readonly int fftImaginary = Shader.PropertyToID("Imaginary");
    private static readonly int fftResults = Shader.PropertyToID("FFTResults");

    // Number of FFT windows to process per frame during deferred generation
    private const int chunkWindowCount = 512;

    [SerializeField] private ComputeShader multiplyShader;
    [SerializeField] private ComputeShader fftShader;
    [SerializeField] private ComputeShader initializeShader;

    private ComputeBuffer cachedFFTBuffer;
    private ComputeBuffer dummyBuffer;

    // The deferred coroutine's temporary buffers are instance fields because StopCoroutine and scene teardown
    // abandon the enumerator without running its end-of-method cleanup. NativeArray(.., Allocator.Persistent) has no
    // finalizer, so OnDestroy and the next generation must be able to release it.
    private ComputeBuffer fftWindowCoeffBuffer;
    private ComputeBuffer fftRealBuffer;
    private ComputeBuffer fftImaginaryBuffer;
    private NativeArray<float> fftChunkData;

    private Coroutine activeFFTCoroutine;

    // ReSharper disable ParameterHidesMember
    // ReSharper disable LocalVariableHidesMember
    public void GenerateFFT(AudioClip clip, int sampleSize, int quality, bool showDuringGenerating = false)
    {
        if (activeFFTCoroutine != null)
        {
            StopCoroutine(activeFFTCoroutine);
            activeFFTCoroutine = null;
        }

        ReleaseFFTChunkBuffers();

        activeFFTCoroutine = StartCoroutine(GenerateFFTDeferred(clip, sampleSize, quality, showDuringGenerating));
    }

    private IEnumerator GenerateFFTDeferred(AudioClip clip, int sampleSize, int quality, bool showDuringGenerating)
    {
        if (SampleBufferManager.MonoSamples == null)
        {
            throw new InvalidOperationException("remember to call SampleBufferManager first, thanks.");
        }

        ClearFFTCache();

        var sampleCount = SampleBufferManager.MonoSampleCount;

        // Reduce spectrogram quality if it would exceed max buffer size 
        while ((long)sampleCount * quality * sizeof(uint) > SystemInfo.maxGraphicsBufferSize)
        {
            quality /= 2;
            Debug.Log($"FFT buffer exceeded. Reduced spectrogram quality to: {quality}");
        }
        if (quality < 1)
        {
            Debug.LogWarning("Refusing to render spectrogram: Exceeds maximum Compute Buffer size.");
            PersistentUI.Instance.ShowDialogBox("PersistentUI", "spectrofailed.computebuffer", null, PersistentUI.DialogBoxPresetType.Ok);
            yield break;
        }

        // Reduce spectrogram quality if it would exceed half of total VRAM capacity
        //   (Video memory should still be available for ChroMapper and other programs)
        var videoMemoryBytes = SystemInfo.graphicsMemorySize * 1024L * 1024L;
        var chunkBufferBytes = 2L * chunkWindowCount * sampleSize * sizeof(float);
        var fftBufferBytes = (((long)sampleCount * quality * sizeof(byte)) + 3L) / 4L;
        var fullVramUsage = fftBufferBytes + chunkBufferBytes;
        while (fullVramUsage > videoMemoryBytes / 2L)
        {
            quality /= 2;
            Debug.Log($"Video Memory exceeded. Reduced spectrogram quality to: {quality}");
        }
        if (quality < 1)
        {
            Debug.LogWarning("Refusing to render spectrogram: Exceeds half of available video memory.");
            PersistentUI.Instance.ShowDialogBox("PersistentUI", "spectrofailed.vram", null, PersistentUI.DialogBoxPresetType.Ok);
            yield break;
        }

        var fftSize = sampleSize / 2;
        var fftCount = sampleCount * quality;
        var totalWindows = fftCount / sampleSize;

        // Generate window coefficients and signal scale factor
        var window = WindowCoefficients.GetWindowForSize(sampleSize);
        var signal = WindowCoefficients.Signal(window);

        // Set global shader variables
        Shader.SetGlobalInt(AudioManager.sampleSize, sampleSize);
        Shader.SetGlobalInt(AudioManager.fftSize, fftSize);
        Shader.SetGlobalInt(AudioManager.fftCount, fftCount);
        Shader.SetGlobalFloat(fftScaleFactor, signal);
        Shader.SetGlobalFloat(fftFrequency, clip.frequency * quality);
        Shader.SetGlobalFloat(fftQuality, quality);
        Shader.SetGlobalInt(fftInitialized, showDuringGenerating ? 1 : 0);

        // Calculate packed buffer size: each uint stores 4 samples as bytes
        var packedFftCount = (fftCount + 3) / 4; // Round up to ensure all values fit
        cachedFFTBuffer = new ComputeBuffer(packedFftCount, sizeof(uint));
        Shader.SetGlobalBuffer(fftResults, cachedFFTBuffer);

        // Prepare window coefficient buffer (persists across all chunks)
        fftWindowCoeffBuffer = new ComputeBuffer(sampleSize, sizeof(float));
        fftWindowCoeffBuffer.SetData(window);

        // Process FFT in chunks, one chunk per frame
        var samplesPerWindow = sampleSize / quality;

        // Allocate our temporary buffers once and reuse them for each chunk
        fftRealBuffer = new ComputeBuffer(chunkWindowCount * sampleSize, sizeof(float));
        fftImaginaryBuffer = new ComputeBuffer(chunkWindowCount * sampleSize, sizeof(float));
        // This bad boy was memory leaking hard
        fftChunkData = new NativeArray<float>(chunkWindowCount * sampleSize, Allocator.Persistent);
        var zeroData = new float[chunkWindowCount * sampleSize]; // Used to reinitialize buffers to zero

        // We generate chucnkWindowCount FFT windows per frame.
        // This keeps our VRAM usage manageable by keeping small buffers for FFT generation,
        // at the slight cost of increased generation time.
        for (var windowStart = 0; windowStart < totalWindows; windowStart += chunkWindowCount)
        {
            // Clear buffers to zero to remove garbage data
            fftRealBuffer.SetData(zeroData);
            fftImaginaryBuffer.SetData(zeroData);
            fftChunkData.CopyFrom(zeroData);

            var windowsThisChunk = Mathf.Min(chunkWindowCount, totalWindows - windowStart);
            var chunkElementCount = windowsThisChunk * sampleSize;

            // Step 1: Prepare real components for this chunk
            var globalSampleStart = windowStart * samplesPerWindow;
            for (var i = 0; i < windowsThisChunk * samplesPerWindow; i += samplesPerWindow)
            {
                var srcIndex = globalSampleStart + i;
                var dstIndex = i * quality;
                var length = Mathf.Clamp(sampleCount - srcIndex, 0, sampleSize);
                if (length > 0)
                    NativeArray<float>.Copy(SampleBufferManager.MonoSamples, srcIndex, fftChunkData, dstIndex, length);
            }

            fftRealBuffer.SetData(fftChunkData);

            // Multiply by window coefficients
            multiplyShader.SetBuffer(0, multiplyA, fftRealBuffer);
            multiplyShader.SetBuffer(0, multiplyB, fftWindowCoeffBuffer);
            ExecuteOverLargeArray(multiplyShader, chunkElementCount);

            // Step 2: Prepare imaginary components (zeroed)
            initializeShader.SetBuffer(0, initializeBuffer, fftImaginaryBuffer);
            ExecuteOverLargeArray(initializeShader, chunkElementCount);

            // Step 3: Execute FFT for this chunk
            fftShader.SetBuffer(0, fftReal, fftRealBuffer);
            fftShader.SetBuffer(0, fftImaginary, fftImaginaryBuffer);
            fftShader.SetInt(chunkOffset, windowStart);

            ExecuteOverLargeArray(fftShader, windowsThisChunk);

            yield return null;
        }

        ReleaseFFTChunkBuffers();

        activeFFTCoroutine = null;
        Shader.SetGlobalInt(fftInitialized, 1);
    }

    private void ReleaseFFTChunkBuffers()
    {
        if (fftWindowCoeffBuffer != null)
        {
            fftWindowCoeffBuffer.Dispose();
            fftWindowCoeffBuffer = null;
        }

        if (fftRealBuffer != null)
        {
            fftRealBuffer.Dispose();
            fftRealBuffer = null;
        }

        if (fftImaginaryBuffer != null)
        {
            fftImaginaryBuffer.Dispose();
            fftImaginaryBuffer = null;
        }

        if (fftChunkData.IsCreated)
        {
            fftChunkData.Dispose();
        }
    }
    // ReSharper restore ParameterHidesMember
    // ReSharper restore LocalVariableHidesMember

    // if GPU threads >= 65535, exception is thrown.
    // this usually happens when our buffers get too big (quality go brrrr)
    // fix this by executing the shader in steps, adding the processed offset as a shader variable so we can
    //   correct for the offset.
    private static void ExecuteOverLargeArray(ComputeShader shader, int length, int maxThreadCount = 65535)
    {
        maxThreadCount = Mathf.Min(maxThreadCount, 65535);

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
        if (activeFFTCoroutine != null)
        {
            StopCoroutine(activeFFTCoroutine);
            activeFFTCoroutine = null;
        }

        ReleaseFFTChunkBuffers();

        ClearFFTCache();

        dummyBuffer.Dispose();
        dummyBuffer = null;
    }
}
