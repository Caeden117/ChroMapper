using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;

public class AudioPreviewGenerator : MonoBehaviour
{
    public WaveformData WaveformData { get; private set; } = new WaveformData();
    
    [SerializeField] private AudioManager audioManager;
    [GradientUsage(true)]
    [SerializeField] private Gradient spectrogramGradient2d;
    [SerializeField] private RawImage spectrogramTileGameObject;
    [SerializeField] private SongInfoEditUI songInfoEditUI;
    [SerializeField] private RectTransform previewSelection;
    [SerializeField] private Slider previewGenerationSlider;

    private Queue<RawImage> cachedTiles = new Queue<RawImage>();
    private List<RawImage> activeTiles = new List<RawImage>();

    private float previewTime;
    private float previewDuration;

    private void Start()
    {
        songInfoEditUI.TempSongLoadedEvent += TempSongLoaded;
    }

    private void TempSongLoaded()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshVisuals());
    }

    private void OnDestroy()
    {
        songInfoEditUI.TempSongLoadedEvent -= TempSongLoaded;
    }

    private IEnumerator RefreshVisuals()
    {
        if (BeatSaberSongContainer.Instance.loadedSong == null)
        {
            yield break;
        }

        // Wait for previous run to end
        audioManager.OnDestroy();
        while (audioManager.IsAlive())
        {
            yield return new WaitForEndOfFrame();
        }

        foreach (RawImage image in activeTiles)
        {
            image.gameObject.SetActive(false);
        }
        cachedTiles = new Queue<RawImage>(activeTiles);
        activeTiles.Clear();
        previewSelection.gameObject.SetActive(false);

        WaveformData = new WaveformData();

        audioManager.SetSecondPerChunk(5);
        audioManager.Begin(false, spectrogramGradient2d, BeatSaberSongContainer.Instance.loadedSong, WaveformData, null, 5);

        while (audioManager.IsAlive())
        {
            previewGenerationSlider.value = (float)WaveformData.ProcessedChunks / WaveformData.Chunks;
            yield return new WaitForEndOfFrame();
        }

        for (int i = 0; i < WaveformData.Chunks; i++)
        {
            float[][] toRender = new float[audioManager.ColumnsPerChunk][];
            WaveformData.GetChunk(i, audioManager.ColumnsPerChunk, ref toRender);

            RawImage image = RetrieveOrCreateRawImage();
            image.texture = WaveformData.BandColors[i];
            image.gameObject.SetActive(true);
            activeTiles.Add(image);
        }

        UpdatePreviewSelection();
        previewSelection.gameObject.SetActive(true);
    }

    private RawImage RetrieveOrCreateRawImage()
    {
        if (cachedTiles.Count > 0) return cachedTiles.Dequeue();
        RawImage newImage = Instantiate(spectrogramTileGameObject.gameObject, transform).GetComponent<RawImage>();
        newImage.gameObject.SetActive(false);
        return newImage;
    }

    public void UpdatePreviewStart(string start)
    {
        if (float.TryParse(start, out previewTime)) UpdatePreviewSelection();
    }

    public void UpdatePreviewDuration(string duration)
    {
        if (float.TryParse(duration, out previewDuration)) UpdatePreviewSelection();
    }

    private void UpdatePreviewSelection()
    {
        if (BeatSaberSongContainer.Instance.loadedSong == null) return;
        float length = BeatSaberSongContainer.Instance.loadedSong.length;

        // oh god look at all this jank
        float size = (transform.parent.parent as RectTransform).sizeDelta.x + (transform.parent as RectTransform).sizeDelta.x;
        
        float ratio = size / length;
        
        previewSelection.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, previewTime * ratio, previewDuration * ratio);
        previewSelection.transform.SetAsLastSibling();
    }
}
