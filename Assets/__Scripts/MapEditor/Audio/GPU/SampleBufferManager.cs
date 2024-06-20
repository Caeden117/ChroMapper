using UnityEngine;

public static class SampleBufferManager
{
    private static readonly int frequency = Shader.PropertyToID("SongFrequency");
    private static readonly int sampleCount = Shader.PropertyToID("SampleCount");
    private static readonly int monoSamples = Shader.PropertyToID("MonoSamples");
    
    private static ComputeBuffer sampleBuffer;

    public static int MonoSampleCount;
    public static float[] MonoSamples;

    public static void GenerateSamplesBuffer(AudioClip clip)
    {
        ClearSamplesBuffer();

        var channels = clip.channels;
        var samples = clip.samples;
        MonoSampleCount = samples;

        var multiChannelSamples = new float[samples * channels];
        clip.GetData(multiChannelSamples, 0);
        
        // Average all audio channels together
        // divide by 1.5 to roughly match amplitude to previous broken behavior (not just 2 because of some destructive interference)
        MonoSamples = new float[samples];
        
        for (var i = 0; i < multiChannelSamples.Length; i++)
            MonoSamples[i / channels] += multiChannelSamples[i] / channels / 1.5f;
        
        sampleBuffer = new ComputeBuffer(MonoSampleCount, sizeof(float));
        sampleBuffer.SetData(MonoSamples);
        
        // Forward this information to our compute shaders.
        Shader.SetGlobalInt(frequency, clip.frequency);
        Shader.SetGlobalInt(sampleCount, MonoSampleCount);
        Shader.SetGlobalBuffer(monoSamples, sampleBuffer);
    }

    public static void ClearSamplesBuffer()
    {
        if (sampleBuffer == null) return;
        
        sampleBuffer.Dispose();
        sampleBuffer = null;
    }
}
