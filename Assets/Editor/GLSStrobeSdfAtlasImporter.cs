using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beatmap.Shared;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(4, "glsstrobesdf")]
public sealed class GLSStrobeSdfAtlasImporter : ScriptedImporter
{
    [Serializable]
    private sealed class Definition
    {
        public int height;
        public int samplesPerPhase;
    }

    private sealed class CurveData
    {
        public float[] Values;
        public float[] Progress;
        public int[] DistanceFieldIndices;
        public Vector4 Parameters;
    }

    public override void OnImportAsset(AssetImportContext context)
    {
        var definition = JsonUtility.FromJson<Definition>(File.ReadAllText(context.assetPath));
        if (definition.height != GLSStrobeSdfAtlas.FieldHeight || definition.samplesPerPhase < 512)
        {
            context.LogImportError("The strobe atlas requires 32-pixel fields and at least 512 samples per phase.");
            return;
        }

        context.DependsOnSourceAsset("Assets/__Scripts/Utils/Easings.cs");
        context.DependsOnSourceAsset("Assets/__Scripts/Beatmap/Shared/GLSStrobeSdfAtlas.cs");
        context.DependsOnSourceAsset("Assets/__Scripts/Beatmap/Shared/GLSStrobeIconRenderer.cs");
        var curves = new CurveData[GLSStrobeSdfAtlas.EasingCount];
        for (var type = GLSEventIconType.EaseLinear; type <= GLSEventIconType.EaseBeatSaberInOutBounce; type++)
        {
            AddCurve(type, definition.samplesPerPhase, curves);
        }

        AddCurve(GLSEventIconType.NoEasingStep, definition.samplesPerPhase, curves);
        var pixels = new byte[GLSStrobeSdfAtlas.Width * GLSStrobeSdfAtlas.Height];
        Array.Fill(pixels, byte.MaxValue);
        // Bake numeric distance data on worker threads; create and register the Unity texture on the import
        // thread.
        Parallel.For(0, curves.Length, easingId =>
        {
            var curve = curves[easingId];
            var first = new float[GLSStrobeSdfAtlas.TileWidth * GLSStrobeSdfAtlas.TileHeight];
            var second = new float[first.Length];
            for (var index = 0; index < GLSStrobeSdfAtlas.PulseWidthCount; index++)
            {
                var pulseWidth = GLSStrobeSdfAtlas.ReferencePulseWidth(index);
                BakeField(curve, first, second, pulseWidth);
                var field = (easingId * GLSStrobeSdfAtlas.PulseWidthCount) + index;
                WriteField(pixels, first, field);
                var cornerField = GLSStrobeSdfAtlas.CornerFieldIndex(easingId, index);
                if (cornerField >= 0)
                    WriteField(pixels, second, cornerField);
            }
        });

        var atlas = new Texture2D(GLSStrobeSdfAtlas.Width, GLSStrobeSdfAtlas.Height, TextureFormat.R8, false, true)
        {
            name = "GLS Strobe SDF Atlas",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 1
        };
        atlas.LoadRawTextureData(pixels);
        atlas.Apply(false, true);
        context.AddObjectToAsset("StrobeSdfAtlas", atlas);
        context.SetMainObject(atlas);
    }

    private static void WriteField(byte[] pixels, float[] distances, int field)
    {
        var originX = (field % GLSStrobeSdfAtlas.Columns) * GLSStrobeSdfAtlas.TileWidth;
        var originY = (field / GLSStrobeSdfAtlas.Columns) * GLSStrobeSdfAtlas.TileHeight;
        for (var y = 0; y < GLSStrobeSdfAtlas.TileHeight; y++)
        {
            for (var x = 0; x < GLSStrobeSdfAtlas.TileWidth; x++)
            {
                var source = (y * GLSStrobeSdfAtlas.TileWidth) + x;
                var destination = ((originY + y) * GLSStrobeSdfAtlas.Width) + originX + x;
                pixels[destination] = EncodeDistance(distances[source]);
            }
        }
    }

    private static void AddCurve(GLSEventIconType type, int samples, CurveData[] curves)
    {
        var parameters = GLSStrobeIconRenderer.EasingParameters(type);
        var easing = Easing.FromID(GLSStrobeIconRenderer.EaseTypeForIcon(type));
        var shaderId = (int)parameters.x;
        // Split Bounce at its peaks and troughs as well as its sharp joins. Otherwise opposite arc branches
        // cancel the direction estimate and create outline spikes.
        var sectionBoundaries = shaderId switch
        {
            28 => new[] { 1f / 22f, 1f / 11f, 2f / 11f, 3f / 11f, 5f / 11f, 7f / 11f },
            29 or 34 => new[] { 4f / 11f, 6f / 11f, 8f / 11f, 9f / 11f, 10f / 11f, 21f / 22f },
            30 => new[]
            {
                1f / 44f, 1f / 22f, 1f / 11f, 3f / 22f, 5f / 22f, 7f / 22f, 0.5f,
                15f / 22f, 17f / 22f, 19f / 22f, 10f / 11f, 21f / 22f, 43f / 44f
            },
            32 => new[] { 0.517f },
            33 => new[] { 0.3f },
            _ => Array.Empty<float>()
        };
        var progress = new List<float>(samples + 1 + sectionBoundaries.Length);
        for (var i = 0; i <= samples; i++)
        {
            progress.Add(i / (float)samples);
        }

        progress.AddRange(sectionBoundaries);
        progress.Sort();
        var values = new float[progress.Count];
        var distanceFieldIndices = new int[progress.Count];
        var section = 0;
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = easing(progress[i]);
            if (i == 0)
            {
                continue;
            }

            var middle = (progress[i - 1] + progress[i]) * 0.5f;
            while (section < sectionBoundaries.Length && middle >= sectionBoundaries[section])
            {
                section++;
            }

            distanceFieldIndices[i] = section % 2;
        }

        curves[shaderId] = new CurveData
        {
            Parameters = parameters,
            Values = values,
            Progress = progress.ToArray(),
            DistanceFieldIndices = distanceFieldIndices
        };
    }

    private static void BakeField(CurveData curve, float[] first, float[] second, float duration)
    {
        var reach = GLSStrobeSdfAtlas.CoreRadius + GLSStrobeSdfAtlas.DistanceRange;
        Array.Fill(first, reach * reach);
        Array.Fill(second, reach * reach);
        var previousX = 13f;
        var previousY = ValueHeight(curve.Values[0], curve.Parameters);
        if (duration == 0f)
        {
            RasterSegment(first, previousX, ValueHeight(0, curve.Parameters), previousX, ValueHeight(1, curve.Parameters), reach);
            return;
        }

        if ((int)curve.Parameters.x == 31)
        {
            var endX = 13f + (258f * duration);
            RasterSegment(first, previousX, previousY, endX, previousY, reach);
            RasterSegment(first, endX, previousY, endX, ValueHeight(1, curve.Parameters), reach);
            return;
        }

        // Keep adjacent curve sections in separate fields so bilinear filtering cannot blur their joins
        // together.
        for (var i = 1; i < curve.Values.Length; i++)
        {
            var x = 13f + (258f * duration * curve.Progress[i]);
            var y = ValueHeight(curve.Values[i], curve.Parameters);
            RasterSegment(curve.DistanceFieldIndices[i] == 0 ? first : second, previousX, previousY, x, y, reach);
            previousX = x;
            previousY = y;
        }
    }

    private static float ValueHeight(float value, Vector4 parameters)
    {
        return 13f + (102f * (value - parameters.z) / parameters.w);
    }

    private static void RasterSegment(float[] distances, float startX, float startY, float endX, float endY, float reach)
    {
        const float scale = GLSStrobeSdfAtlas.CoordinateScale;
        const int padding = GLSStrobeSdfAtlas.Padding;
        var minimumX = Mathf.Max(0, Mathf.CeilToInt(((Mathf.Min(startX, endX) - reach) / scale) + padding - 0.5f));
        var maximumX = Mathf.Min(GLSStrobeSdfAtlas.TileWidth - 1, Mathf.FloorToInt(((Mathf.Max(startX, endX) + reach) / scale) + padding - 0.5f));
        var minimumY = Mathf.Max(0, Mathf.CeilToInt(((Mathf.Min(startY, endY) - reach) / scale) + padding - 0.5f));
        var maximumY = Mathf.Min(GLSStrobeSdfAtlas.TileHeight - 1, Mathf.FloorToInt(((Mathf.Max(startY, endY) + reach) / scale) + padding - 0.5f));
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var inverseLength = 1f / Mathf.Max((deltaX * deltaX) + (deltaY * deltaY), 1e-12f);
        for (var y = minimumY; y <= maximumY; y++)
        {
            var offsetY = ((y - padding + 0.5f) * scale) - startY;
            for (var x = minimumX; x <= maximumX; x++)
            {
                var offsetX = ((x - padding + 0.5f) * scale) - startX;
                var progress = Mathf.Clamp01(((offsetX * deltaX) + (offsetY * deltaY)) * inverseLength);
                var distanceX = offsetX - (progress * deltaX);
                var distanceY = offsetY - (progress * deltaY);
                var squared = (distanceX * distanceX) + (distanceY * distanceY);
                var index = (y * GLSStrobeSdfAtlas.TileWidth) + x;
                distances[index] = Mathf.Min(distances[index], squared);
            }
        }
    }

    private static byte EncodeDistance(float squared)
    {
        var signed = Mathf.Sqrt(squared) - GLSStrobeSdfAtlas.CoreRadius;
        var encoded = Mathf.Clamp01(0.5f + (signed / (2f * GLSStrobeSdfAtlas.DistanceRange)));
        return (byte)Mathf.RoundToInt(encoded * 255f);
    }
}
