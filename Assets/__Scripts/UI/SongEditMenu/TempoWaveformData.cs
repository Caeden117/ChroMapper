using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GPU min/max pyramid. Audio uploads are bounded; peak reduction and rendering stay on the GPU.
/// The workspace owns the buffers and must dispose them on replacement or close.
/// </summary>
public sealed class TempoWaveformData : IDisposable
{
    private readonly List<ComputeBuffer> levels = new();
    public int SamplesPerPeak { get; private set; }
    public int SampleCount { get; private set; }
    internal int LevelCount => levels.Count;
    internal ComputeBuffer GetLevel(int index) => levels[index];

    public static IEnumerator Generate(AudioClip clip, int samplesPerPeak, ComputeShader shader,
        Action<TempoWaveformData> completed)
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("Waveform rendering requires compute shader support.");
            completed(null);
            yield break;
        }

        var result = new TempoWaveformData
        {
            SamplesPerPeak = Math.Max(Math.Clamp(samplesPerPeak, 8, 8192), (int)Math.Ceiling(clip.samples / 1048576d)),
            SampleCount = clip.samples
        };
        var transferred = false;
        try
        {
            var peakCount = (clip.samples + result.SamplesPerPeak - 1) / result.SamplesPerPeak;
            var peaks = new ComputeBuffer(peakCount, sizeof(float) * 2);
            result.levels.Add(peaks);

            // Align chunks with bins: each invocation owns a complete peak, including all channels.
            var frames = Math.Min(clip.samples, Math.Max(1, 32768 / result.SamplesPerPeak) * result.SamplesPerPeak);
            var samples = new float[frames * clip.channels];
            using var upload = new ComputeBuffer(samples.Length, sizeof(float));
            var generate = shader.FindKernel("GeneratePeaks");
            shader.SetInt("Channels", clip.channels);
            shader.SetInt("SamplesPerPeak", result.SamplesPerPeak);
            shader.SetBuffer(generate, "Samples", upload);
            shader.SetBuffer(generate, "Output", peaks);

            for (var offset = 0; offset < clip.samples; offset += frames)
            {
                if (!clip.GetData(samples, offset))
                    throw new InvalidOperationException("Unable to read decoded audio samples for the waveform.");

                var valid = Math.Min(frames, clip.samples - offset);
                upload.SetData(samples);
                shader.SetInt("FrameCount", valid);
                shader.SetInt("OutputOffset", offset / result.SamplesPerPeak);
                shader.Dispatch(generate, ((valid + result.SamplesPerPeak - 1) / result.SamplesPerPeak + 63) / 64, 1, 1);
                yield return null;
            }

            var reduce = shader.FindKernel("ReducePeaks");
            while (peaks.count > 1)
            {
                var next = new ComputeBuffer((peaks.count + 1) / 2, sizeof(float) * 2);
                result.levels.Add(next);
                shader.SetInt("InputCount", peaks.count);
                shader.SetBuffer(reduce, "Input", peaks);
                shader.SetBuffer(reduce, "Output", next);
                shader.Dispatch(reduce, (next.count + 63) / 64, 1, 1);
                peaks = next;
            }

            completed(result);
            transferred = true;
        }
        finally
        {
            if (!transferred) result.Dispose();
        }
    }

    public void Dispose()
    {
        foreach (var level in levels) level.Dispose();
        levels.Clear();
    }
}