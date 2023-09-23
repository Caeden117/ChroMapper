using System.Linq;
using System.Threading;
using UnityEngine;

public class SyncAnalyser : MonoBehaviour
{
    private SyncAnalysis syncAnalysis = new SyncAnalysis(89, 205);
    private Thread detectionThread;
    private DialogBox dialogBox;

    public void Analyse()
    {
        var clip = BeatSaberSongContainer.Instance.LoadedSong;
        var samples = new float[clip.samples * clip.channels];
        var numChannels = clip.channels;
        var sampleRate = clip.frequency;

        // Get the song audio data
        if (clip.GetData(samples, 0))
        {
            dialogBox = PersistentUI.Instance.CreateNewDialogBox()
                .WithTitle("Detecting BPM...");

            dialogBox.AddComponent<TextComponent>()
                .WithInitialValue("Attempting to detect the BPM of your song.\n\nThis can take several moments...");

            dialogBox.Open();

            // don't block the main thread
            detectionThread = new Thread(() =>
            {
                syncAnalysis.Run(samples, numChannels, sampleRate);
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
        dialogBox.Close();

        dialogBox = PersistentUI.Instance.CreateNewDialogBox()
            .WithTitle("BPM Detection Results");

        dialogBox.AddComponent<TextComponent>()
            .WithInitialValue("BPM detection complete.\n\nSelect a BPM from the results below, and ChroMapper will automatically apply the BPM and offset to your song.");

        var items = syncAnalysis.Results.Select(it => $"{it.BPM} BPM");
        dialogBox.AddComponent<DropdownComponent>()
            .WithLabel("BPM")
            .WithOptions(items);

        dialogBox.AddFooterButton(ApplyBPM, "PersistentUI", "submit");
        dialogBox.AddFooterButton(null, "PersistentUI", "cancel");

        dialogBox.Open();
    }

    private void ApplyBPM()
    {

    }
}
