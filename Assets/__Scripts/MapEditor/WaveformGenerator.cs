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

    public static bool IsActive = false;

    private void Start()
    { 
        waveformImage.gameObject.SetActive(IsActive);
        StartCoroutine(WaveformGenerationLoop());
    }

    public void UpdateActive(bool active)
    {
        IsActive = active;
        waveformImage.gameObject.SetActive(active);
    }

    private IEnumerator WaveformGenerationLoop()
    {
        while (true)
        {
            if (!IsActive) yield return new WaitForSeconds(0.1f);
            else
            {
                yield return new WaitForSeconds(UpdateTick);
                transform.localScale = new Vector3(1, 1, 0.75f * EditorScaleController.EditorScale);
                StartCoroutine(LoadWaveform());
            }
        }
    }

    /// <summary>
    /// Credits to Rolo and CircuitLord for NotReaper/AudicaEditor code for the waveform.
    /// </summary>
    private IEnumerator LoadWaveform()
    {
        yield return new WaitForEndOfFrame();
        AudioClip clip = BeatSaberSongContainer.Instance.loadedSong;
        float previewLength = (60 / BeatSaberSongContainer.Instance.song.beatsPerMinute) * lookAheadController.offset;
        int samplePos = Mathf.CeilToInt(Mathf.Abs(atsc.CurrentSeconds * clip.frequency));
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float[] samples = new float[Mathf.CeilToInt(previewLength * clip.frequency)];
        float[] waveform = new float[width];
        try
        {
            clip.GetData(samples, samplePos);
        }
        catch { yield break; }
        int packSize = (Mathf.CeilToInt(previewLength * clip.frequency) / width) + 1;
        int s = 0;
        float max = 1.5f;
        for (int i = 0; i < Mathf.CeilToInt(previewLength * clip.frequency); i += packSize)
        {
            waveform[s] = Mathf.Abs(samples[i]);
            if (waveform[s] > max) max = waveform[s];
            s++;
        }
        for (int i = 0; i < width; i++)
        {
            waveform[i] /= (max * saturation);
            if (waveform[i] > 1) waveform[i] = 1;
        }
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, new Color(0, 0, 0, 0));
        }
        for (int x = 0; x < waveform.Length; x++)
        {
            for (int y = 0; y < waveform[x] * ((float) height * 0.75f); y++)
            {
                tex.SetPixel(x, (height / 2) + y, Color.gray);
                tex.SetPixel(x, (height / 2) - y, Color.gray);
            }
        }
        tex.Apply();
        Destroy(waveformImage.texture);
        waveformImage.texture = tex;
    }
}
