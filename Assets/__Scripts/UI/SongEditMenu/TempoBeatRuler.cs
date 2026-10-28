using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Pooled labels with an adaptive whole-beat stride and minimum screen spacing.</summary>
public class TempoBeatRuler : MonoBehaviour
{
    public TempoMapperView View;
    public TMP_FontAsset Font;
    private readonly List<TextMeshProUGUI> labels = new();
    private bool dirty = true;
    private float previousWidth;
    public void Refresh() => dirty = true;

    private void LateUpdate()
    {
        var rect = (RectTransform)transform;
        if (!dirty && previousWidth == rect.rect.width) return;
        dirty = false;
        previousWidth = rect.rect.width;
        if (View == null || View.Map == null || View.ViewEnd <= View.ViewStart || previousWidth <= 0) return;
        var first = View.Map.BeatAt((int)Math.Clamp(View.ViewStart, 0, View.Map.EndSample));
        var last = View.Map.BeatAt((int)Math.Clamp(View.ViewEnd, 0, View.Map.EndSample));
        var desired = (last - first) / Math.Max(1, previousWidth / 60);
        var stride = Math.Pow(2, Math.Max(0, Math.Ceiling(Math.Log(Math.Max(1, desired), 2))));
        var count = 0;
        var previousX = -100f;
        for (var index = 0; index < View.Map.Regions.Count; index++)
        {
            var region = View.Map.Regions[index];
            if (region.EndSampleIndex < View.ViewStart || region.StartSampleIndex > View.ViewEnd) continue;
            for (var beat = Math.Ceiling(Math.Max(first, region.StartBeat) / stride) * stride;
                 beat < Math.Min(last + 0.0001, region.EndBeat) && count < 128; beat += stride)
            {
                var sample = View.Map.SampleAt(index, beat);
                var x = (float)((sample - View.ViewStart) / (View.ViewEnd - View.ViewStart)) * previousWidth;
                if (x < previousX + 55 || x > previousWidth - 25) continue;
                if (count == labels.Count)
                {
                    var item = new GameObject("Beat number", typeof(RectTransform), typeof(TextMeshProUGUI));
                    item.transform.SetParent(transform, false);
                    var label = item.GetComponent<TextMeshProUGUI>();
                    label.font = Font;
                    label.fontSize = 14;
                    label.color = new Color(1, 1, 1, 0.85f);
                    label.raycastTarget = false;
                    label.textWrappingMode = TextWrappingModes.NoWrap;
                    label.alignment = TextAlignmentOptions.TopLeft;
                    label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0, 1);
                    label.rectTransform.pivot = new Vector2(0, 1);
                    label.rectTransform.sizeDelta = new Vector2(70, 20);
                    labels.Add(label);
                }
                labels[count].gameObject.SetActive(true);
                labels[count].text = beat.ToString("0");
                labels[count].rectTransform.anchoredPosition = new Vector2(x + 2, 0);
                previousX = x;
                count++;
            }
        }
        for (var i = count; i < labels.Count; i++) labels[i].gameObject.SetActive(false);
    }
}
