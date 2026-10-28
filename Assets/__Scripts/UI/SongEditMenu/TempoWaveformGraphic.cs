using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Draws bounded peak columns only when the waveform, viewport, or dimensions change.</summary>
public class TempoWaveformGraphic : MaskableGraphic
{
    public TempoWaveformData Data;
    public int MaximumColumns = 2048;
    public bool RectifyPeaks;
    private double start;
    private double end;

    public void SetWindow(double firstSample, double lastSample)
    {
        if (firstSample == start && lastSample == end) return;
        start = firstSample;
        end = lastSample;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (Data == null || end <= start) return;
        var rect = rectTransform.rect;
        var columns = Math.Clamp((int)Math.Ceiling(rect.width * canvas.scaleFactor), 1, Math.Clamp(MaximumColumns, 128, 8192));
        var middle = rect.center.y;
        var height = rect.height * 0.47f;
        // Keep each column tied to the same cached audio bin while the viewport moves.
        // Re-bucketing into screen pixels every frame changes peaks and produces visible shimmer.
        var desiredSamples = (end - start) / columns;
        var level = Math.Clamp((int)Math.Ceiling(Math.Log(Math.Max(1, desiredSamples / Data.SamplesPerPeak), 2) - 0.000001), 0, Data.LevelCount - 1);
        var peaks = Data.GetLevel(level);
        var samplesPerColumn = Data.SamplesPerPeak * Math.Pow(2, level);
        var firstColumn = Math.Clamp((int)Math.Floor(start / samplesPerColumn), 0, peaks.Length);
        var lastColumn = Math.Clamp((int)Math.Ceiling(end / samplesPerColumn), firstColumn, peaks.Length);
        for (var i = firstColumn; i < lastColumn; i++)
        {
            var first = i * samplesPerColumn;
            var last = Math.Min(Data.SampleCount, first + samplesPerColumn);
            var peak = peaks[i];
            var x0 = rect.xMin + rect.width * (float)((Math.Max(start, first) - start) / (end - start));
            var x1 = rect.xMin + rect.width * (float)((Math.Min(end, last) - start) / (end - start));
            if (x1 <= x0) continue;
            var y0 = RectifyPeaks ? rect.yMin : middle + Mathf.Clamp(peak.x, -1, 0) * height;
            var y1 = RectifyPeaks
                ? rect.yMin + Mathf.Clamp01(Math.Max(Math.Abs(peak.x), Math.Abs(peak.y))) * rect.height * 0.94f
                : middle + Mathf.Clamp(peak.y, 0, 1) * height;
            y1 = Math.Max(y0 + 0.5f, y1);
            var index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x0, y0), color, Vector2.zero);
            mesh.AddVert(new Vector3(x0, y1), color, Vector2.zero);
            mesh.AddVert(new Vector3(x1, y1), color, Vector2.zero);
            mesh.AddVert(new Vector3(x1, y0), color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
