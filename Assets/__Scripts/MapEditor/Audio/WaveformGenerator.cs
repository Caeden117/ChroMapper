using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

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

    private float secondPerChunk = float.NaN;
    private int chunksGenerated;

    private void Start()
    { 
        secondPerChunk = atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize);
        spectroParent.position = new Vector3(spectroParent.position.x, 0, -atsc.offsetBeat * EditorScaleController.EditorScale * 2);
        if (Settings.Instance.WaveformGenerator) StartCoroutine(GenerateAllWaveforms());
        else gameObject.SetActive(false);
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => !SceneTransitionManager.IsLoading); //How we know "Start" has been called
        mixer.SetFloat("WaveformVolume", -80);
        source.Play();
        while (chunksGenerated * secondPerChunk < source.clip.length)
        {
            for (int i = 0; i < audioManager.ColumnsPerChunk; i++)
            {
                float newTime = (chunksGenerated * secondPerChunk) + (i / (float)audioManager.ColumnsPerChunk * secondPerChunk);
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
    }
}
