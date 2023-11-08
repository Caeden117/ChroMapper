using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AudioSync;
using AudioSync.Util;
using UnityEngine;

public class SyncAnalyser : MonoBehaviour
{
    private readonly AudioSync.SyncAnalyser syncAnalysis = new AudioSync.SyncAnalyser(89, 205);

    private IList<SyncResult> results;
    private Thread detectionThread;

    private DialogBox detectingBPMBox;
    private DialogBox bpmResultsBox;
    private DropdownComponent bpmResultsDropdown;

    public void Analyse()
    {
        var clip = BeatSaberSongContainer.Instance.LoadedSong;
        var samples = new float[clip.samples * clip.channels];
        var numChannels = clip.channels;
        var sampleRate = clip.frequency;

        // Get the song audio data
        if (clip.GetData(samples, 0))
        {
            detectingBPMBox.Open();

            // don't block the main thread
            detectionThread = new Thread(() =>
            {
                var doubles = samples.ConvertToMonoSamples(numChannels);

                results = syncAnalysis.Run(doubles, sampleRate);
            });
            detectionThread.Start();
        }
    }

    private void Update()
    {
        // there is no good way to have a callback happen on the Unity main thread while sync analysis happens off-thread.
        // so we just detect when the thread dies.
        if (detectionThread != null && !detectionThread.IsAlive)
        {
            OnComplete();
            detectionThread = null;
        }
    }

    private void OnComplete()
    {
        detectingBPMBox.Close();

        var items = results.Select(it => $"{it.BPM:N2} BPM | Offset: {it.Offset:N2} sec");
        bpmResultsDropdown.WithOptions(items);

        bpmResultsBox.Open();
    }

    private void ApplyBPM()
    {

    }

    private void Start()
    {
        // Detecting BPM
        detectingBPMBox = PersistentUI.Instance.CreateNewDialogBox()
            .WithTitle("Detecting BPM...")
            .DontDestroyOnClose();

        detectingBPMBox.AddComponent<TextComponent>()
            .WithInitialValue("Attempting to detect the BPM of your song.\n\nThis can take several moments...");

        // BPM Results
        bpmResultsBox = PersistentUI.Instance.CreateNewDialogBox()
            .WithTitle("BPM Detection Results")
            .DontDestroyOnClose();

        bpmResultsBox.AddComponent<TextComponent>()
            .WithInitialValue("BPM detection complete.\n\nSelect a BPM from the results below, and ChroMapper will automatically apply the BPM and offset to your song.");

        bpmResultsDropdown = bpmResultsBox.AddComponent<DropdownComponent>()
            .WithLabel("BPM");

        bpmResultsBox.AddFooterButton(ApplyBPM, "PersistentUI", "submit");
        bpmResultsBox.AddFooterButton(null, "PersistentUI", "cancel");
    }
}
