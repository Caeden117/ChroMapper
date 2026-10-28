using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

/// <summary>One audio decode pass; dyadic min/max levels make visible-range queries independent of song length.</summary>
public sealed class TempoWaveformData
{
    private readonly List<Vector2[]> levels = new();
    public int SamplesPerPeak { get; private set; }
    public int SampleCount { get; private set; }
    internal int LevelCount => levels.Count;
    internal Vector2[] GetLevel(int index) => levels[index];

    public static IEnumerator Generate(AudioClip clip, int samplesPerPeak, Action<TempoWaveformData> completed)
    {
        // Bound memory on long recordings, while retaining the requested detail for ordinary songs.
        var result = new TempoWaveformData
        {
            SamplesPerPeak = Math.Max(Math.Clamp(samplesPerPeak, 8, 8192), (int)Math.Ceiling(clip.samples / 1048576d)),
            SampleCount = clip.samples
        };
        var peaks = new Vector2[(int)Math.Ceiling(clip.samples / (double)result.SamplesPerPeak)];
        var frames = Math.Min(32768, clip.samples);
        var buffer = new float[frames * clip.channels];
        var budget = Stopwatch.StartNew();
        for (var offset = 0; offset < clip.samples; offset += frames)
        {
            if (!clip.GetData(buffer, offset))
            {
                UnityEngine.Debug.LogWarning("Tempo waveform: unable to read decoded audio samples.");
                completed(null);
                yield break;
            }
            var valid = Math.Min(frames, clip.samples - offset);
            for (var frame = 0; frame < valid; frame++)
            {
                var index = (offset + frame) / result.SamplesPerPeak;
                var peak = peaks[index];
                for (var channel = 0; channel < clip.channels; channel++)
                {
                    var value = buffer[frame * clip.channels + channel];
                    if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                    peak.x = Math.Min(peak.x, value);
                    peak.y = Math.Max(peak.y, value);
                }
                peaks[index] = peak;
            }
            if (budget.ElapsedMilliseconds < 4) continue;
            yield return null;
            budget.Restart();
        }
        result.levels.Add(peaks);
        while (peaks.Length > 1)
        {
            var next = new Vector2[(peaks.Length + 1) / 2];
            for (var i = 0; i < next.Length; i++)
            {
                var left = peaks[i * 2];
                var right = peaks[Math.Min(i * 2 + 1, peaks.Length - 1)];
                next[i] = new Vector2(Math.Min(left.x, right.x), Math.Max(left.y, right.y));
            }
            result.levels.Add(next);
            peaks = next;
            if (budget.ElapsedMilliseconds < 4) continue;
            yield return null;
            budget.Restart();
        }
        completed(result);
    }
}
