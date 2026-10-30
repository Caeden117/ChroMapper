using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Tempo markers and input over the existing spectrogram renderer.</summary>
public class TempoGridOverlay : MaskableGraphic, IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    public TempoMapperView View;
    public bool Overview;
    private int boundary = -1;
    private int region;
    private double beat;
    private int pressedSample;
    private int splitSample;
    private bool moving;
    private bool dragged;
    private bool shifting;
    private double Start => Overview ? 0 : View.ViewStart;
    private double End => Overview ? View.Map.EndSample : View.ViewEnd;

    protected override void OnPopulateMesh(VertexHelper vertices)
    {
        vertices.Clear();
        if (View == null || View.Map == null || End <= Start) return;
        var rect = rectTransform.rect;
        if (Overview)
        {
            Quad(vertices, Mathf.Max(rect.xMin, X(View.ViewStart)), rect.yMin, Mathf.Min(rect.xMax, X(View.ViewEnd)), rect.yMax, new Color(0.2f, 0.6f, 1, 0.2f));
        }
        else
        {
            for (var i = 0; i < View.Map.Regions.Count; i++)
            {
                var r = View.Map.Regions[i];
                if (r.EndSampleIndex < Start || r.StartSampleIndex > End) continue;
                var first = Math.Max(r.StartBeat, View.Map.BeatAt((int)Math.Max(Start, r.StartSampleIndex)));
                var last = Math.Min(r.EndBeat, View.Map.BeatAt((int)Math.Min(End, r.EndSampleIndex)));
                var step = GridStep(first, last);
                for (var b = Math.Ceiling(first / step) * step; b < last; b += step)
                {
                    if (i > 0 && Math.Abs(b - r.StartBeat) < 0.000001) continue;
                    Line(vertices, View.Map.SampleAt(i, b), new Color(1, 1, 1, 0.5f), 1);
                }
            }
        }
        for (var i = 1; i < View.Map.Regions.Count; i++)
        {
            var sample = View.Map.Regions[i].StartSampleIndex;
            var markerColor = new Color(1, 0.92f, 0.2f, 0.5f);
            if (Overview) { Line(vertices, sample, markerColor, 1); continue; }
            if (sample < Start || sample > End) continue;
            var x = X(sample);
            // Do not draw the line behind the handle: overlapping 50% quads would become 75% opaque.
            Quad(vertices, Math.Max(rect.xMin, x - 0.5f), rect.yMin, Math.Min(rect.xMax, x + 0.5f), rect.center.y - 5.5f, markerColor);
            Quad(vertices, Math.Max(rect.xMin, x - 0.5f), rect.center.y + 5.5f, Math.Min(rect.xMax, x + 0.5f), rect.yMax, markerColor);
            Quad(vertices, Math.Max(rect.xMin, x - 1.5f), rect.center.y - 5.5f, Math.Min(rect.xMax, x + 1.5f), rect.center.y + 5.5f, markerColor);
        }
        var pulse = View.PlaybackMarkerPulse;
        var playbackColor = Color.Lerp(new Color(1, 0.35f, 0.12f, 1), Color.white, pulse);
        Line(vertices, View.Sample, playbackColor, 1.5f + pulse * 4.5f, true);
    }

    private double GridStep(double first, double last) => Math.Max(1d / View.Subdivision,
        Math.Ceiling((last - first) * View.Subdivision / Math.Max(1, rectTransform.rect.width / 12)) / View.Subdivision);

    private float X(double sample) => rectTransform.rect.xMin
        + (float)((sample - Start) / (End - Start)) * rectTransform.rect.width;

    private int Sample(PointerEventData data)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out var point);
        var t = Mathf.InverseLerp(rectTransform.rect.xMin, rectTransform.rect.xMax, point.x);
        return (int)Math.Round(Start + t * (End - Start));
    }

    private int BoundaryAt(int sample)
    {
        for (var i = 1; i < View.Map.Regions.Count; i++)
            if (Mathf.Abs(X(sample) - X(View.Map.Regions[i].StartSampleIndex)) <= 6) return i;
        return -1;
    }

    public void OnPointerDown(PointerEventData data)
    {
        var sample = Sample(data);
        if (Overview)
        {
            if (data.button == PointerEventData.InputButton.Left) View.Seek(sample);
            return;
        }
        boundary = BoundaryAt(sample);
        if (data.button == PointerEventData.InputButton.Right)
        {
            if (boundary > 0) View.MergeBoundary(boundary);
            return;
        }
        if (data.button != PointerEventData.InputButton.Left) return;
        EventSystem.current.SetSelectedGameObject(null);
        region = View.Map.RegionAt(sample);
        var r = View.Map.Regions[region];
        var first = Math.Max(r.StartBeat, View.Map.BeatAt((int)Math.Max(Start, r.StartSampleIndex)));
        var last = Math.Min(r.EndBeat, View.Map.BeatAt((int)Math.Min(End, r.EndSampleIndex)));
        var step = GridStep(first, last);
        beat = Math.Round(View.Map.BeatAt(sample) / step) * step;
        var nearest = View.Map.SampleAt(region, beat);
        splitSample = boundary < 0 && beat > r.StartBeat && beat < r.EndBeat && Mathf.Abs(X(sample) - X(nearest)) <= 6 ? nearest : -1;
        shifting = View.ShiftTimeline;
        moving = shifting || boundary > 0 || splitSample >= 0;
        pressedSample = sample;
        dragged = false;
        if (moving) View.BeginGesture();
        else View.Seek(sample);
    }

    public void OnBeginDrag(PointerEventData data) { }

    public void OnDrag(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        if (Overview) { View.Seek(Sample(data)); return; }
        dragged = true;
        if (shifting) View.ShiftGesture(Sample(data) - pressedSample);
        else if (moving) View.Drag(boundary, region, beat, Sample(data));
        else View.Seek(Sample(data));
    }

    public void OnPointerUp(PointerEventData data) => Finish(data);
    public void OnEndDrag(PointerEventData data) => Finish(data);

    private void Finish(PointerEventData data)
    {
        if (Overview || data.button != PointerEventData.InputButton.Left || !moving) return;
        View.EndGesture();
        moving = false;
        if (!dragged && !shifting && splitSample >= 0) View.Split(splitSample);
    }

    public void OnScroll(PointerEventData data)
    {
        if (Overview || View.PrecisionScrollActive) return;
        if (Keyboard.current?.shiftKey.isPressed == true) View.Pan(-data.scrollDelta.y * (View.ViewEnd - View.ViewStart) * 0.1);
        else View.Zoom(data.scrollDelta.y > 0 ? 0.8 : 1.25, Sample(data));
    }

    private void Line(VertexHelper vertices, int sample, Color color, float width, bool playhead = false)
    {
        if (sample < Start || sample > End) return;
        var rect = rectTransform.rect;
        var x = playhead && !Overview && View.IsFollowingPlayback ? rect.center.x : X(sample);
        Quad(vertices, Math.Max(rect.xMin, x - width / 2), rect.yMin, Math.Min(rect.xMax, x + width / 2), rect.yMax, color);
    }

    private static void Quad(VertexHelper vertices, float x0, float y0, float x1, float y1, Color color)
    {
        if (x1 <= x0) return;
        var offset = vertices.currentVertCount;
        vertices.AddVert(new Vector3(x0, y0), color, Vector2.zero);
        vertices.AddVert(new Vector3(x0, y1), color, Vector2.zero);
        vertices.AddVert(new Vector3(x1, y1), color, Vector2.zero);
        vertices.AddVert(new Vector3(x1, y0), color, Vector2.zero);
        vertices.AddTriangle(offset, offset + 1, offset + 2);
        vertices.AddTriangle(offset, offset + 2, offset + 3);
    }
}
