using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class WaveformGenerator : MonoBehaviour {
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private BeatmapObjectCallbackController lookAheadController;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private GameObject spectrogramChunkPrefab;
    [SerializeField] private Transform spectroParent;
    [SerializeField] private float saturation = 1;
    public Gradient spectrogramHeightGradient;

    public static float UpdateTick = 0.1f;

    private float secondPerChunk = float.NaN;
    private int chunksToGenerate = 0;
    private int chunksGenerated = 0;

    private void Awake()
    {
        if (Settings.Instance.WaveformGenerator)
            SceneTransitionManager.Instance.AddLoadRoutine(GenerateAllWaveforms());
    }

    private void Start()
    { 
        secondPerChunk = atsc.GetSecondsFromBeat(BeatmapObjectContainerCollection.ChunkSize);
    }

    public void UpdateActive(bool active)
    {
        Settings.Instance.WaveformGenerator = active;
        if (active) PersistentUI.Instance.SendMessage("Refresh the Editor to see changes!");
    }

    private IEnumerator GenerateAllWaveforms()
    {
        yield return new WaitUntil(() => secondPerChunk != float.NaN); //How we know "Start" has been called
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(true);
        PersistentUI.Instance.LevelLoadSliderLabel.text = "";
        mainMixer.SetFloat("Volume", -80);
        chunksToGenerate = Mathf.CeilToInt(source.clip.length / secondPerChunk);
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
            PersistentUI.Instance.LevelLoadSlider.value = chunksGenerated / (float)chunksToGenerate;
            PersistentUI.Instance.LevelLoadSliderLabel.text =
                $"Loading Waveform... THIS WILL TAKE A WHILE! ({chunksGenerated} / {chunksToGenerate} chunks loaded," +
                $" {(chunksGenerated / (float)chunksToGenerate * 100).ToString("F2")}% complete.)";
        }
        source.Stop();
        source.time = 0;
        mainMixer.SetFloat("Volume", 0);
        PersistentUI.Instance.LevelLoadSlider.gameObject.SetActive(false); 
    }
}
