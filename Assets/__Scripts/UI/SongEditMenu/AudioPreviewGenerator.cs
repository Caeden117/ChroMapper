using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioPreviewGenerator : MonoBehaviour
{
    private static readonly int viewStart = Shader.PropertyToID("_ViewStart");
    private static readonly int viewEnd = Shader.PropertyToID("_ViewEnd");
    
    [SerializeField] private AudioManager audioManager;
    [GradientUsage(true), SerializeField] private Gradient spectrogramGradient2d;
    [SerializeField] private SongInfoEditUI songInfoEditUI;
    [SerializeField] private GameObject previewGameObject;
    [SerializeField] private RectTransform previewSelection;
    
    private float previewDuration;
    private float previewTime;

    private void Start() => songInfoEditUI.TempSongLoadedEvent += TempSongLoaded;

    private void OnDestroy() => songInfoEditUI.TempSongLoadedEvent -= TempSongLoaded;

    private void TempSongLoaded()
    {
        if (BeatSaberSongContainer.Instance.LoadedSong == null)
        {
            previewGameObject.SetActive(false);
            return;
        }
        
        // Reinitialize view bounds to encompass the entire thing
        Shader.SetGlobalFloat(viewStart, 0);
        Shader.SetGlobalFloat(viewEnd, BeatSaberSongContainer.Instance.LoadedSongLength);
        
        ColorBufferManager.GenerateBuffersForGradient(spectrogramGradient2d);
        SampleBufferManager.GenerateSamplesBuffer(BeatSaberSongContainer.Instance.LoadedSong);
        audioManager.GenerateFFT(BeatSaberSongContainer.Instance.LoadedSong, 1024, 1);

        UpdatePreviewSelection();
        previewGameObject.SetActive(true);
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
        var size = (transform.parent as RectTransform).sizeDelta.x +
                   (transform as RectTransform).sizeDelta.x;

        var ratio = size / length;

        previewSelection.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, previewTime * ratio,
            previewDuration * ratio);
    }
}
