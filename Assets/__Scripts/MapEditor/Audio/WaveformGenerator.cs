using UnityEngine;
using UnityEngine.Serialization;

public class WaveformGenerator : MonoBehaviour
{
    [FormerlySerializedAs("audioManager")] public AudioManager AudioManager;

    [FormerlySerializedAs("spectrogramGradient2d")] [GradientUsage(true)] public Gradient SpectrogramGradient2d;
    
    private void Start()
    {
        if (BeatSaberSongContainer.Instance.LoadedSong == null) return;
        
        ColorBufferManager.GenerateBuffersForGradient(SpectrogramGradient2d);
        SampleBufferManager.GenerateSamplesBuffer(BeatSaberSongContainer.Instance.LoadedSong);
        
        AudioManager.GenerateFFT(BeatSaberSongContainer.Instance.LoadedSong,
            Settings.Instance.SpectrogramSampleSize,
            Settings.Instance.SpectrogramEditorQuality);
    }
}
