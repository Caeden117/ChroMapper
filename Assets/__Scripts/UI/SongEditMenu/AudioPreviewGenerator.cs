using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioPreviewGenerator : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;

    [GradientUsage(true)] [SerializeField] private Gradient spectrogramGradient2d;

    [SerializeField] private RawImage spectrogramTileGameObject;
    [SerializeField] private SongInfoEditUI songInfoEditUI;
    [SerializeField] private RectTransform previewSelection;
    [SerializeField] private Slider previewGenerationSlider;
    private readonly List<RawImage> activeTiles = new List<RawImage>();

    private Queue<RawImage> cachedTiles = new Queue<RawImage>();
    private float previewDuration;

    private float previewTime;
    public WaveformData WaveformData { get; private set; } = new WaveformData();

    private void Start() => songInfoEditUI.TempSongLoadedEvent += TempSongLoaded;

    private void OnDestroy() => songInfoEditUI.TempSongLoadedEvent -= TempSongLoaded;

    private void TempSongLoaded()
    {
        StopAllCoroutines();
        StartCoroutine(RefreshVisuals());
    }

    private IEnumerator RefreshVisuals()
    {
        if (BeatSaberSongContainer.Instance.LoadedSong == null) yield break;

        // Wait for previous run to end
        audioManager.OnDestroy();
        while (audioManager.IsAlive()) yield return new WaitForEndOfFrame();

        foreach (var image in activeTiles) image.gameObject.SetActive(false);
        cachedTiles = new Queue<RawImage>(activeTiles);
        activeTiles.Clear();
        previewSelection.gameObject.SetActive(false);

        WaveformData = new WaveformData();

        audioManager.SetSecondPerChunk(5);
        audioManager.Begin(false, spectrogramGradient2d, BeatSaberSongContainer.Instance.LoadedSong, WaveformData, null,
            5);

        while (audioManager.IsAlive())
        {
            previewGenerationSlider.value = (float)WaveformData.ProcessedChunks / WaveformData.Chunks;
            yield return new WaitForEndOfFrame();
        }

        for (var i = 0; i < WaveformData.Chunks; i++)
        {
            var toRender = new float[audioManager.ColumnsPerChunk][];
            WaveformData.GetChunk(i, audioManager.ColumnsPerChunk, ref toRender);

            var image = RetrieveOrCreateRawImage();
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
        var newImage = Instantiate(spectrogramTileGameObject.gameObject, transform).GetComponent<RawImage>();
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
        if (BeatSaberSongContainer.Instance.LoadedSong == null) return;
        var length = BeatSaberSongContainer.Instance.LoadedSong.length;

        // oh god look at all this jank
        var size = (transform.parent.parent as RectTransform).sizeDelta.x +
                   (transform.parent as RectTransform).sizeDelta.x;

        var ratio = size / length;

        previewSelection.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, previewTime * ratio,
            previewDuration * ratio);
        previewSelection.transform.SetAsLastSibling();
    }
}
