using System.Threading;
using UnityEngine;

public class BPMAnalyser : MonoBehaviour
{
    public void Analyse()
    {
        var clip = BeatSaberSongContainer.Instance.LoadedSong;
        var samples = new float[clip.samples * clip.channels];
        var numChannels = clip.channels;
        var sampleRate = clip.frequency;

        // Get the song audio data
        if (clip.GetData(samples, 0))
        {
            var thread = new Thread(() =>
            {
                var tempo = new SyncAnalysis(89, 205);
                tempo.Run(samples, numChannels, sampleRate);
                foreach (var result in tempo.Results) Debug.Log($"[BPM:{result.BPM}] [Fitness:{result.Fitness}] [Beat Dur:{result.Beat}] [Offset:{result.Offset}]");
            });
            thread.Start();
        }
    }
}
