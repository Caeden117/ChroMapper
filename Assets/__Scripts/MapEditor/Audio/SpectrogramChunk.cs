using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     Thanks to SheetCode for being a huge help in making this work!
/// </summary>
public class SpectrogramChunk : MonoBehaviour
{
    private readonly Vector2 spectrogramScale = new Vector2(4f, 0.1f);
    private int chunkID;
    private float[][] localData;
    private float max = 1;
    private MeshRenderer meshRenderer;
    private float min;
    private float previousEditorScale;

    private Texture2D texture;

    private WaveformGenerator waveform;

    private void Start()
    {
        gameObject.layer = 12;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        var nearestChunk = (int)Math.Round(
            waveform.Atsc.CurrentBeat / (double)BeatmapObjectContainerCollection.ChunkSize
            , MidpointRounding.AwayFromZero);
        var enabled = chunkID > nearestChunk - Settings.Instance.ChunkDistance &&
                      chunkID < nearestChunk + Settings.Instance.ChunkDistance;

        if (meshRenderer.enabled != enabled) meshRenderer.enabled = enabled;

        if (!enabled) return;

        if (EditorScaleController.EditorScale != previousEditorScale)
        {
            previousEditorScale = EditorScaleController.EditorScale;
            // Beats per centisecond, only needed for the 3d waveform. I don't know why
            var bpcs = BeatSaberSongContainer.Instance.Song.BeatsPerMinute / (60f * 100);
            transform.localPosition = new Vector3(0, -0.15f,
                (chunkID + (waveform.WaveformType == 2 ? bpcs : 0)) *
                (EditorScaleController.EditorScale * BeatmapObjectContainerCollection.ChunkSize));
            transform.localScale = new Vector3(spectrogramScale.x, spectrogramScale.y,
                BeatmapObjectContainerCollection.ChunkSize * EditorScaleController.EditorScale);
        }

        meshRenderer.material.SetFloat("_Rotation", transform.rotation.eulerAngles.y);
    }

    public void UpdateMesh(float[][] data, Texture2D colors, int chunkID, WaveformGenerator gen)
    {
        localData = data;
        texture = colors;
        this.chunkID = chunkID;
        waveform = gen;
        ReCalculateMesh();
    }

    private void ReCalculateMesh()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        var verts = new List<Vector3>();
        var triangles = new List<int>();

        float xRange = 1;
        var xSamples = Math.Min(50, localData[0].Length) - 1;
        var zSamples = 299;

        for (var l = 0; l < zSamples; l++)
        {
            var m = (localData.Length - 1) * l / zSamples;
            var m2 = (localData.Length - 1) * (l + 1) / zSamples;

            var currentVolumes = localData[m];
            var previousVolumes = localData[m2];

            var zBandValue = (float)m / (localData.Length - 1);
            var zBandNextValue = (float)m2 / (localData.Length - 1);

            for (var k = 0; k < xSamples; k++)
            {
                var i = (currentVolumes.Length - 1) * k / xSamples;
                var i2 = (currentVolumes.Length - 1) * (k + 1) / xSamples;

                // calculating x position 
                var x = (float)i / (currentVolumes.Length - 2) * xRange;
                var xNext = (float)i2 / (currentVolumes.Length - 2) * xRange;
                var volume = currentVolumes[i];
                var voulumeNext = currentVolumes[i2];

                // two volumes that was previous
                var volumePrevious = previousVolumes[i];
                var volumeNextPrevious = previousVolumes[i2];
                if (volume > max) max = volume;
                if (volume < min) min = volume;

                if (m == 0)
                    GenerateFrontFace(x, xNext, volume, voulumeNext, verts, triangles, zBandValue);

                // connection with previous band

                // adding verst connecting this band with the next one
                verts.Add(new Vector3(x, volume, zBandValue));
                verts.Add(new Vector3(xNext, voulumeNext, zBandValue));
                verts.Add(new Vector3(x, volumePrevious, zBandNextValue));
                verts.Add(new Vector3(xNext, volumeNextPrevious, zBandNextValue));

                var startPoint = verts.Count - 4;
                // adding 2 triangles using this vertex
                triangles.Add(startPoint + 0);
                triangles.Add(startPoint + 2);
                triangles.Add(startPoint + 1);
                triangles.Add(startPoint + 2);
                triangles.Add(startPoint + 3);
                triangles.Add(startPoint + 1);

                // left side
                if (i == 0)
                {
                    verts.Add(new Vector3(x, 0, zBandValue));
                    verts.Add(new Vector3(x, 0, zBandNextValue));
                    verts.Add(new Vector3(x, volume, zBandValue));
                    verts.Add(new Vector3(x, volumePrevious, zBandNextValue));
                    startPoint = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(startPoint + 0);
                    triangles.Add(startPoint + 1);
                    triangles.Add(startPoint + 2);
                    triangles.Add(startPoint + 1);
                    triangles.Add(startPoint + 3);
                    triangles.Add(startPoint + 2);
                }

                // right side
                if (i == currentVolumes.Length - 2)
                {
                    verts.Add(new Vector3(xNext, 0, zBandValue));
                    verts.Add(new Vector3(xNext, 0, zBandNextValue));
                    verts.Add(new Vector3(xNext, volume, zBandValue));
                    verts.Add(new Vector3(xNext, volumePrevious, zBandNextValue));
                    startPoint = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(startPoint + 0);
                    triangles.Add(startPoint + 2);
                    triangles.Add(startPoint + 1);
                    triangles.Add(startPoint + 1);
                    triangles.Add(startPoint + 2);
                    triangles.Add(startPoint + 3);
                }
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.triangles = triangles.ToArray();
        if (waveform.WaveformType == 2)
        {
            // 3D color the mesh
            var meshColors = new List<Color>(verts.Count);
            foreach (var vertex in verts)
            {
                var lerp = Mathf.InverseLerp(min, max, vertex.y);
                if (float.IsNaN(lerp)) lerp = 0;
                meshColors.Add(waveform.SpectrogramHeightGradient.Evaluate(lerp));
            }

            mesh.colors = meshColors.ToArray();
        }
        else
        {
            // In 2d we have too much data to draw in the mesh so we have to render a texture to color space between vertexes
            // Simplest UV ever
            var uv = new Vector2[verts.Count];
            for (var i = 0; i < verts.Count; i++)
            {
                var it = verts[i];
                uv[i] = new Vector2(
                    Mathf.Clamp(it.z, 0.001f, 0.999f),
                    Mathf.Clamp(it.x, 0.001f, 0.999f)
                );
            }

            mesh.uv = uv;

            // apply texture to mesh
            var customMaterial = new Material(Shader.Find("Shader Graphs/Spectrogram 2D"))
            {
                // wait why am i not setting this to the same render queue as lights, pepega
                renderQueue = 2925
            };
            customMaterial.SetFloat("_Rotation", transform.rotation.eulerAngles.y);
            customMaterial.SetTexture("_MainTex", texture);
            GetComponent<MeshRenderer>().material = customMaterial;
        }

        mesh.RecalculateNormals();
    }

    private void GenerateFrontFace(float x, float xNext, float volume, float volumeNext, List<Vector3> verts,
        List<int> triangles, float zBandValue)
    {
        verts.Add(new Vector3(x, 0, zBandValue));
        verts.Add(new Vector3(x, volume, zBandValue));
        verts.Add(new Vector3(xNext, 0, zBandValue));
        verts.Add(new Vector3(xNext, volumeNext, zBandValue));
        var startPoint = verts.Count - 4;
        triangles.Add(startPoint + 0);
        triangles.Add(startPoint + 1);
        triangles.Add(startPoint + 2);
        triangles.Add(startPoint + 1);
        triangles.Add(startPoint + 3);
        triangles.Add(startPoint + 2);
    }
}
