using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thanks to SheetCode for being a huge help in making this work!
/// </summary>
public class SpectrogramChunk : MonoBehaviour
{
    private readonly Vector2 spectrogramScale = new Vector2(0.04f, 0.1f);

    Vector3[] verts;
    int[] triangles;

    private WaveformGenerator waveform;
    private MeshRenderer meshRenderer;
    private float[][] localData;
    private int previousEditorScale;
    private int chunkID;
    private float min;
    private float max = 1;

    private void Start()
    {
        gameObject.layer = 12;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void UpdateMesh(float[][] data, int chunkID, WaveformGenerator gen)
    {
        localData = data;
        this.chunkID = chunkID;
        waveform = gen;
        ReCalculateMesh();
    }

    private void Update()
    {
        if (EditorScaleController.EditorScale != previousEditorScale)
        {
            previousEditorScale = EditorScaleController.EditorScale;
            transform.localPosition = new Vector3(0, -0.15f,
                (chunkID + 1f) * ((float)EditorScaleController.EditorScale * BeatmapObjectContainerCollection.ChunkSize));
            transform.localScale = new Vector3(spectrogramScale.x, spectrogramScale.y,
                BeatmapObjectContainerCollection.ChunkSize * EditorScaleController.EditorScale * -1);
        }
        int nearestChunk = (int)Math.Round(waveform.atsc.CurrentBeat / (double)BeatmapObjectContainerCollection.ChunkSize
            , MidpointRounding.AwayFromZero);
        bool enabled = chunkID > nearestChunk - Settings.Instance.ChunkDistance && chunkID < nearestChunk + Settings.Instance.ChunkDistance;
        if (meshRenderer.enabled != enabled) meshRenderer.enabled = enabled;
        meshRenderer.material.SetFloat("_Rotation", transform.rotation.eulerAngles.y);
    }

    void ReCalculateMesh()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();

        float xRange = 100;

        for (int m = 0; m < localData.Length - 1; m++)
        {
            float[] currentVolumes = localData[m];
            float[] previousVolumes = localData[m + 1];

            float zBandValue = (float)m / (localData.Length - 1);
            float zBandNextValue = (float)(m + 1) / (localData.Length - 1);

            for (int i = 0; i < currentVolumes.Length - 1; i++)
            {
                // calculating x position 
                float x = ((float)i / (currentVolumes.Length - 2)) * xRange;
                float xNext = ((float)(i + 1) / (currentVolumes.Length - 2)) * xRange;
                float volume = currentVolumes[i];
                float voulumeNext = currentVolumes[i + 1];

                // two volumes that was previous
                float volumePrevious = previousVolumes[i];
                float volumeNextPrevious = previousVolumes[i + 1];
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

                int start_point = verts.Count - 4;
                // adding 2 triangles using this vertex
                triangles.Add(start_point + 0);
                triangles.Add(start_point + 2);
                triangles.Add(start_point + 1);
                triangles.Add(start_point + 2);
                triangles.Add(start_point + 3);
                triangles.Add(start_point + 1);

                // left side
                if (i == 0)
                {
                    verts.Add(new Vector3(x, 0, zBandValue));
                    verts.Add(new Vector3(x, 0, zBandNextValue));
                    verts.Add(new Vector3(x, volume, zBandValue));
                    verts.Add(new Vector3(x, volumePrevious, zBandNextValue));
                    start_point = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(start_point + 0);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 3);
                    triangles.Add(start_point + 2);
                }

                // right side
                if (i == currentVolumes.Length - 2)
                {
                    verts.Add(new Vector3(xNext, 0, zBandValue));
                    verts.Add(new Vector3(xNext, 0, zBandNextValue));
                    verts.Add(new Vector3(xNext, volume, zBandValue));
                    verts.Add(new Vector3(xNext, volumePrevious, zBandNextValue));
                    start_point = verts.Count - 4;
                    // adding 2 triangles using this vertex
                    triangles.Add(start_point + 0);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 1);
                    triangles.Add(start_point + 2);
                    triangles.Add(start_point + 3);
                }

            }

        }
        List<Color> meshColors = new List<Color>(verts.Count);
        foreach (Vector3 vertex in verts)
        {
            float lerp = Mathf.InverseLerp(min, max, vertex.y);
            if (float.IsNaN(lerp)) lerp = 0;
            meshColors.Add(waveform.spectrogramHeightGradient.Evaluate(lerp));
        }
        mesh.vertices = verts.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.colors = meshColors.ToArray();
        mesh.RecalculateNormals();
    }

    private void GenerateFrontFace(float x, float x_next, float volume, float volume_next, List<Vector3> verts, List<int> triangles, float zBandValue)
    {
        verts.Add(new Vector3(x, 0, zBandValue));
        verts.Add(new Vector3(x, volume, zBandValue));
        verts.Add(new Vector3(x_next, 0, zBandValue));
        verts.Add(new Vector3(x_next, volume_next, zBandValue));
        int start_point = verts.Count - 4;
        triangles.Add(start_point + 0);
        triangles.Add(start_point + 1);
        triangles.Add(start_point + 2);
        triangles.Add(start_point + 1);
        triangles.Add(start_point + 3);
        triangles.Add(start_point + 2);
    }
}