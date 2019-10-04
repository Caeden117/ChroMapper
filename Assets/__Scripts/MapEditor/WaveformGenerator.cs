using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveformGenerator : MonoBehaviour {

    [SerializeField] private RawImage waveformImage;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private float saturation = 1;

    public static float UpdateTick = 0.1f;

    private float secondPerChunk;
    private int chunksGenerated = 0;

    private void Start()
    { 
        waveformImage.gameObject.SetActive(Settings.Instance.WaveformGenerator);
        secondPerChunk = atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize);
        StartCoroutine(LoadWaveform());
    }

    public void UpdateActive(bool active)
    {
        Settings.Instance.WaveformGenerator = active;
        waveformImage.gameObject.SetActive(active);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U)) StartCoroutine(LoadWaveform());
    }

    /// <summary>
    /// Credits to Rolo and CircuitLord for NotReaper/AudicaEditor code for the waveform.
    /// </summary>
    private IEnumerator LoadWaveform()
    {
        yield return new WaitForEndOfFrame();
        AudioClip clip = BeatSaberSongContainer.Instance.loadedSong;
        int samplePos = Mathf.CeilToInt(Mathf.Abs(secondPerChunk * chunksGenerated * clip.frequency));
        int width = 2048;
        int height = 2048;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float[] samples = new float[Mathf.CeilToInt(secondPerChunk * clip.frequency)];
        try
        {
            clip.GetData(samples, samplePos);
        }
        catch { yield break; }
        int packSize = (Mathf.CeilToInt(secondPerChunk * clip.frequency) / width) + 1;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++) tex.SetPixel(x, y, new Color(0, 0, 0, 0));
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y,
                    Color.white * Mathf.Abs(samples[(x * (packSize - 1)) + Mathf.CeilToInt((float)packSize / height * y)]));
        }
        tex.Apply();
        Destroy(waveformImage.texture);
        waveformImage.texture = tex;
        chunksGenerated++;
    }
}
