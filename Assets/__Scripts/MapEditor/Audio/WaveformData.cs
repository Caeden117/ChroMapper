using System;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

public class WaveformData
{
    public int Chunks = 0;
    public int ProcessedChunks = 0;
    public float[][] BandVolumes { get; internal set; }
    public NativeArray<Color32>[] BandCData { get; internal set; }
    public Texture2D[] BandColors { get; internal set; }

    ~WaveformData()
    {
        foreach (var tex in BandColors) Object.Destroy(tex);
    }

    public void InitBandVolumes(int len, int p)
    {
        if (BandVolumes == null)
        {
            BandVolumes = new float[len * Chunks][];
            BandColors = new Texture2D[Chunks];
            BandCData = new NativeArray<Color32>[Chunks];

            for (var i = 0; i < Chunks; i++)
            {
                BandColors[i] = new Texture2D(len, p, TextureFormat.RGBA32, false);
                BandCData[i] = BandColors[i].GetRawTextureData<Color32>();
            }
        }
    }

    public void GetChunk(int chunkId, int len, ref float[][] toRender)
    {
        Array.Copy(BandVolumes, chunkId * len, toRender, 0, len);
        BandColors[chunkId].Apply();
    }
}
