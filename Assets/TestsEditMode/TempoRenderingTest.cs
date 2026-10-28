using System;
using System.Linq;
using System.Reflection;
using Beatmap.Info;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TestsEditMode
{
    public class TempoRenderingTest
    {
        private static VertexHelper Draw(Graphic graphic)
        {
            var mesh = new VertexHelper();
            graphic.GetType().GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(VertexHelper) }, null)
                .Invoke(graphic, new object[] { mesh });
            return mesh;
        }

        [Test]
        public void RectifyUsesNegativePeakMagnitudeAndKeepsTheSameAudioCache()
        {
            var root = new GameObject("Waveform test", typeof(Canvas));
            var clip = AudioClip.Create("Peaks", 16, 1, 8000, false);
            try
            {
                clip.SetData(Enumerable.Range(0, 16).Select(i => i % 2 == 0 ? -0.75f : 0.25f).ToArray(), 0);
                TempoWaveformData data = null;
                var generate = TempoWaveformData.Generate(clip, 8, result => data = result);
                while (generate.MoveNext()) { }
                var child = new GameObject("Peaks", typeof(RectTransform), typeof(TempoWaveformGraphic));
                child.transform.SetParent(root.transform, false);
                var graphic = child.GetComponent<TempoWaveformGraphic>();
                graphic.rectTransform.sizeDelta = new Vector2(100, 100);
                graphic.Data = data;
                graphic.SetWindow(0, 16);
                using var bipolar = Draw(graphic);
                var vertex = new UIVertex();
                bipolar.PopulateUIVertex(ref vertex, 0);
                Assert.That(vertex.position.y, Is.EqualTo(-35.25f).Within(0.01));
                graphic.RectifyPeaks = true;
                using var rectified = Draw(graphic);
                rectified.PopulateUIVertex(ref vertex, 0);
                Assert.That(vertex.position.y, Is.EqualTo(-50).Within(0.01));
                rectified.PopulateUIVertex(ref vertex, 1);
                Assert.That(vertex.position.y, Is.EqualTo(20.5f).Within(0.01), "Negative amplitude must contribute to the rectified peak.");
                Assert.That(graphic.Data, Is.SameAs(data), "Rectification should not decode the clip again.");
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(clip); }
        }

        [Test]
        public void MarkersAreHalfOpaqueAndHandlesAreThreeTimesTheLineWidthWithoutOutlines()
        {
            var root = new GameObject("Marker test", typeof(Canvas));
            try
            {
                var view = root.AddComponent<TempoMapperView>();
                var map = new TempoMap(new[] { new BpmInfoBpmRegion { StartSampleIndex = 0, EndSampleIndex = 480000, StartBeat = 0, EndBeat = 20 } }, 48000);
                map.Split(240000);
                foreach (var pair in new[] { ("Map", (object)map), ("ViewStart", (object)0d), ("ViewEnd", (object)480000d), ("Subdivision", (object)1) })
                    typeof(TempoMapperView).GetProperty(pair.Item1).SetValue(view, pair.Item2);
                var child = new GameObject("Grid", typeof(RectTransform), typeof(TempoGridOverlay));
                child.transform.SetParent(root.transform, false);
                var grid = child.GetComponent<TempoGridOverlay>();
                grid.rectTransform.sizeDelta = new Vector2(200, 100);
                grid.View = view;
                using var mesh = Draw(grid);
                var goldWidths = new System.Collections.Generic.List<float>();
                for (var i = 0; i < mesh.currentVertCount; i += 4)
                {
                    var left = new UIVertex(); var right = new UIVertex();
                    mesh.PopulateUIVertex(ref left, i); mesh.PopulateUIVertex(ref right, i + 2);
                    if (left.color.g < 100) continue; // Playback cursor remains distinct and opaque.
                    Assert.That(left.color.a, Is.InRange(127, 128));
                    if (left.color.b < 100) goldWidths.Add(right.position.x - left.position.x);
                }
                Assert.That(goldWidths, Has.Count.EqualTo(3), "Two line segments and one handle, with no outline geometry.");
                Assert.That(goldWidths[0], Is.EqualTo(1).Within(0.01));
                Assert.That(goldWidths[1], Is.EqualTo(1).Within(0.01));
                Assert.That(goldWidths[2], Is.EqualTo(3).Within(0.01));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
