using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class WaveformGenerator : MonoBehaviour {
    public AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectroParent;
    [SerializeField] private float saturation = 1;
    public AudioManager audioManager;
    [GradientUsage(true)]
    public Gradient spectrogramHeightGradient;

    public static float UpdateTick = 0.1f;

    private WaveformData waveformData;
    public int WaveformType;

    private void Start()
    {
        WaveformType = Settings.Instance.Waveform;
        waveformData = new WaveformData();
        audioManager.SetSecondPerChunk(atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize));
        spectroParent.position = new Vector3(spectroParent.position.x, 0, -atsc.offsetBeat * EditorScaleController.EditorScale * 2);
        if (WaveformType > 0) StartCoroutine(GenerateAllWaveforms());
        else gameObject.SetActive(false);
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading); //How we know "Start" has been called

        // Start the background worker
        audioManager.Begin(WaveformType == 2, spectrogramHeightGradient, source.clip, waveformData, atsc, BeatmapObjectContainerCollection.ChunkSize);

        int chunkId;
        HashSet<int> renderedChunks = new HashSet<int>();

        // Loop while we have completed calulations waiting to be rendered
        //  or there are threads still running generating new chunks
        //  or we are still writing a previous save to disk (we save again at the end)
        while (audioManager.chunksComplete.Count > 0 || audioManager.IsAlive())
        {
            if (audioManager.chunksComplete.TryDequeue(out chunkId)) {
                float[][] toRender = new float[audioManager.ColumnsPerChunk][];
                waveformData.GetChunk(chunkId, audioManager.ColumnsPerChunk, ref toRender);

                SpectrogramChunk chunk = Instantiate(spectrogramChunkPrefab, spectroParent).GetComponent<SpectrogramChunk>();
                chunk.UpdateMesh(toRender, waveformData.BandColors[chunkId], chunkId, this);
                renderedChunks.Add(chunkId);

                // Wait 2 frames for smoooth
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("WaveformGenerator: Main thread done");
    }
}
