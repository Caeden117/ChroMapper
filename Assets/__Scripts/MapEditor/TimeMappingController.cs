using System.Diagnostics;
using Beatmap.Base;
using UnityEngine;

public class TimeMappingController : MonoBehaviour
{
    private BaseDifficulty map;
    private Stopwatch stopwatch;
    private float timeAtLoad;

    private void Start()
    {
        map = BeatSaberSongContainer.Instance.Map;
        timeAtLoad = map.Time;
        stopwatch = Stopwatch.StartNew();
    }

    private void Update() => map.Time = timeAtLoad + (float)stopwatch.Elapsed.TotalMinutes;

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            stopwatch.Start();
        }
        else
        {
            stopwatch.Stop();
        }
    }
}
