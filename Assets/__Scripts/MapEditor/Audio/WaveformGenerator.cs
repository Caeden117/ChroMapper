using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

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

    private WaveformCache waveformCache;

    private void Start()
    {
        waveformCache = new WaveformCache(this, audioManager.ColumnsPerChunk);
        audioManager.SetSecondPerChunk(atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize));
        spectroParent.position = new UnityEngine.Vector3(spectroParent.position.x, 0, -atsc.offsetBeat * EditorScaleController.EditorScale * 2);
        if (Settings.Instance.WaveformGenerator) StartCoroutine(GenerateAllWaveforms());
        else gameObject.SetActive(false);
    }

    private void StartAudioManagerThread()
    {
        audioManager.Begin(source.clip, waveformCache.waveformData);
    }

    public void ChunkComplete(int chunkId)
    {
        audioManager.chunksComplete.Enqueue(chunkId);
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading); //How we know "Start" has been called

        // Spawn a thread to read the cache file and let the game continue until it's complete
        Thread readThread = new Thread(waveformCache.ReadWaveformFromFile);
        readThread.Start();
        while (readThread.IsAlive)
        {
            yield return null;
        }

        // If we need to calculate chunks start the background worker
        if (!waveformCache.fileHadAllChunks)
        {
            StartAudioManagerThread();
        }

        int chunkId;
        float[][] toRender = new float[audioManager.ColumnsPerChunk][];

        // Add chunks loaded from file so that the save mechanism knows it can save them
        HashSet<int> renderedChunks = new HashSet<int>();
        for (int i = 0; i < waveformCache.waveformData.chunksLoaded; i++)
        {
            renderedChunks.Add(i);
        }

        // Loop while we have completed calulations waiting to be rendered
        //  or there are threads still running generating new chunks
        //  or we are still writing a previous save to disk (we save again at the end)
        while (audioManager.chunksComplete.Count > 0 || audioManager.IsAlive() || waveformCache.OngoingSave())
        {
            if (audioManager.chunksComplete.TryDequeue(out chunkId)) {
                Debug.Log("WaveformGenerator: Rendering chunk " + chunkId);
                waveformCache.waveformData.getChunk(chunkId, audioManager.ColumnsPerChunk, ref toRender);

                SpectrogramChunk chunk = Instantiate(spectrogramChunkPrefab, spectroParent).GetComponent<SpectrogramChunk>();
                chunk.UpdateMesh(toRender, chunkId, this);
                renderedChunks.Add(chunkId);

                // Don't save too often as it's an expensive operation in itself
                // Don't save if the first chunk hasn't been rendered as we only save linearly from the start
                if (renderedChunks.Count % 10 == 0 && renderedChunks.Contains(0))
                {
                    waveformCache.ScheduleSave(renderedChunks);
                }
            }
            yield return new WaitForEndOfFrame();
        }

        // If we calculated some chunks since reading the file we should have new data to save
        if (!waveformCache.fileHadAllChunks)
        {
            Debug.Log("WaveformGenerator: Final save");
            waveformCache.ScheduleSave(renderedChunks);
        }

        Debug.Log("WaveformGenerator: Main thread done");
    }
}
