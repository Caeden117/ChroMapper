using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class WaveformGenerator : MonoBehaviour
{
    [FormerlySerializedAs("atsc")] public AudioTimeSyncController Atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private AudioSource source;
    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectroParent;
    [FormerlySerializedAs("audioManager")] public AudioManager AudioManager;

    [FormerlySerializedAs("spectrogramHeightGradient")] [GradientUsage(true)] public Gradient SpectrogramHeightGradient;

    [FormerlySerializedAs("spectrogramGradient2d")] [GradientUsage(true)] public Gradient SpectrogramGradient2d;

    public int WaveformType;

    private WaveformData waveformData;

    private void Start()
    {
        WaveformType = Settings.Instance.Waveform;
        waveformData = new WaveformData();
        AudioManager.SetSecondPerChunk(Atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize));
        spectroParent.localPosition = new Vector3(0, 0, 0);
        if (WaveformType > 0) StartCoroutine(GenerateAllWaveforms());
        else gameObject.SetActive(false);
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading); //How we know "Start" has been called

        // Start the background worker
        AudioManager.Begin(WaveformType == 2, WaveformType == 2 ? SpectrogramHeightGradient : SpectrogramGradient2d,
            source.clip, waveformData, Atsc, BeatmapObjectContainerCollection.ChunkSize);


        // Loop while we have completed calulations waiting to be rendered
        //  or there are threads still running generating new chunks
        //  or we are still writing a previous save to disk (we save again at the end)
        while (AudioManager.ChunksComplete.Count > 0 || AudioManager.IsAlive())
        {
            if (AudioManager.ChunksComplete.TryDequeue(out var chunkId))
            {
                var toRender = new float[AudioManager.ColumnsPerChunk][];
                waveformData.GetChunk(chunkId, AudioManager.ColumnsPerChunk, ref toRender);

                var chunk = Instantiate(spectrogramChunkPrefab, spectroParent).GetComponent<SpectrogramChunk>();
                chunk.UpdateMesh(toRender, waveformData.BandColors[chunkId], chunkId, this);

                // Wait 2 frames for smoooth
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForEndOfFrame();
        }

        Debug.Log("WaveformGenerator: Main thread done");
    }
}
