using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class WaveformGenerator : MonoBehaviour {
    public AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectroParent;
    [SerializeField] private float saturation = 1;
    public Gradient spectrogramHeightGradient;

    public static float UpdateTick = 0.1f;

    private float secondPerChunk = float.NaN;
    private int chunksGenerated = 0;

    private void Start()
    { 
        secondPerChunk = atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize);
        if (Settings.Instance.WaveformGenerator) StartCoroutine(GenerateAllWaveforms());
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => secondPerChunk != float.NaN); //How we know "Start" has been called
        mixer.SetFloat("WaveformVolume", -80);
        source.Play();
        while (chunksGenerated * secondPerChunk < source.clip.length)
        {
            for (int i = 0; i < audioManager.ColumnsPerChunk; i++)
            {
                float newTime = (chunksGenerated * secondPerChunk) + (secondPerChunk / audioManager.ColumnsPerChunk * i);
                if (newTime >= source.clip.length) break;
                source.time = newTime;
                yield return new WaitForEndOfFrame();
                audioManager.PopulateData();
            }
            SpectrogramChunk chunk = Instantiate(spectrogramChunkPrefab, spectroParent).GetComponent<SpectrogramChunk>();
            chunk.UpdateMesh(AudioManager._bandVolumes, chunksGenerated, this);
            audioManager.Start(); //WE GO AGANE
            chunksGenerated++;
        }
        source.Stop();
        source.time = 0;
        mixer.SetFloat("WaveformVolume", 0);
    }
}
