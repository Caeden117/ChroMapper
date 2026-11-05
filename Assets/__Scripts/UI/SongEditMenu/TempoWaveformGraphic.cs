using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A single UI quad sampling the GPU peak pyramid at the visible pixel density.</summary>
public class TempoWaveformGraphic : MaskableGraphic
{
    [SerializeField] private Material waveformTemplate;
    public TempoWaveformData Data;
    public int MaximumColumns = 2048;
    public bool RectifyPeaks;
    private double start;
    private double end;
    private Material waveformMaterial;
    private bool wasReady;
    private bool IsReady => waveformTemplate != null && Data != null && Data.LevelCount > 0 && end > start;

    public void SetWindow(double firstSample, double lastSample)
    {
        start = firstSample;
        end = lastSample;
    }

    private void LateUpdate()
    {
        var ready = IsReady;
        if (ready && waveformMaterial == null)
        {
            waveformMaterial = new Material(waveformTemplate);
            material = waveformMaterial;
        }
        if (ready != wasReady)
        {
            wasReady = ready;
            SetVerticesDirty();
            SetMaterialDirty();
        }
        if (ready) SetMaterialDirty();
    }

    protected override void UpdateMaterial()
    {
        // Do not submit a GPU shader without its required buffer, including in Prefab Mode.
        if (!IsReady || waveformMaterial == null)
        {
            canvasRenderer.Clear();
            return;
        }


        var target = materialForRendering;
        target.SetInt("_Ready", 1);

        var columns = Math.Clamp(rectTransform.rect.width * canvas.scaleFactor, 1, Math.Clamp(MaximumColumns, 128, 8192));
        var desiredSamples = (end - start) / columns;
        var level = Math.Clamp((int)Math.Ceiling(Math.Log(Math.Max(1, desiredSamples / Data.SamplesPerPeak), 2) - 0.000001), 0, Data.LevelCount - 1);
        var peaks = Data.GetLevel(level);
        target.SetBuffer("_Peaks", peaks);
        target.SetInt("_PeakCount", peaks.count);
        target.SetFloat("_SamplesPerPeak", (float)(Data.SamplesPerPeak * Math.Pow(2, level)));
        target.SetFloat("_SampleCount", Data.SampleCount);
        target.SetFloat("_ViewStart", (float)start);
        target.SetFloat("_ViewEnd", (float)end);
        target.SetFloat("_Rectify", RectifyPeaks ? 1 : 0);
        // Buffer bindings must exist before Graphic submits this material to CanvasRenderer.
        base.UpdateMaterial();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (!IsReady) return;
        var rect = rectTransform.rect;
        mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, new Vector2(0, 0));
        mesh.AddVert(new Vector3(rect.xMin, rect.yMax), color, new Vector2(0, 1));
        mesh.AddVert(new Vector3(rect.xMax, rect.yMax), color, new Vector2(1, 1));
        mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, new Vector2(1, 0));
        mesh.AddTriangle(0, 1, 2);
        mesh.AddTriangle(0, 2, 3);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (waveformMaterial != null)
        {
            if (Application.isPlaying) Destroy(waveformMaterial);
            else DestroyImmediate(waveformMaterial);
        }
    }
}