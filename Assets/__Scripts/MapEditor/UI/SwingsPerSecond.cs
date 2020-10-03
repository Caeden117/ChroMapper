using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SwingsPerSecond {

    private readonly NotesContainer notes;
    private readonly ObstaclesContainer obstacles;

    private readonly float MaximumTolerance = .06f; // Magic number based on maximum tolerated swing speed
    private readonly float MaximumWindowTolerance = .07f; // For windowed sliders

    public SwingsPerSecond(NotesContainer notes, ObstaclesContainer obstacles)
    {
        this.notes = notes;
        this.obstacles = obstacles;
    }

    private int NotesCount => notes.LoadedObjects.Count;

    private float LastInteractiveObjectTime(float songBPM)
    {
        float lastNoteTime = 0f;
        if (NotesCount > 0)
        {
            lastNoteTime = notes.LoadedObjects.Last()._time / songBPM * 60;
        }

        float lastInteractiveObstacleTime = 0f;
        foreach (BeatmapObstacle obstacle in obstacles.LoadedObjects)
        {
            if (obstacle._width >= 2 || obstacle._lineIndex == 1 || obstacle._lineIndex == 2)
            {
                float obstacleEnd = (obstacle._time + obstacle._duration) / songBPM * 60;
                lastInteractiveObstacleTime = Mathf.Max(lastInteractiveObstacleTime, obstacleEnd);
            }
        }

        return Mathf.Max(lastInteractiveObstacleTime, lastNoteTime);
    }

    private float FirstInteractiveObjectTime(float songBPM)
    {
        float firstNoteTime = float.MaxValue;
        if (NotesCount > 0)
        {
            firstNoteTime = notes.LoadedObjects.First()._time / songBPM * 60;
        }

        float firstInteractiveObstacleTime = float.MaxValue;
        foreach (BeatmapObstacle obstacle in obstacles.LoadedObjects)
        {
            if (obstacle._width >= 2 || obstacle._lineIndex == 1 || obstacle._lineIndex == 2)
            {
                firstInteractiveObstacleTime = (obstacle._time + obstacle._duration) / songBPM * 60;
                break;
            }
        }

        return Mathf.Min(firstInteractiveObstacleTime, firstNoteTime);
    }

    private bool MaybeWindowed(BeatmapNote note1, BeatmapNote note2)
    {
        return Mathf.Max(
            Mathf.Abs(note1._lineIndex - note2._lineIndex),
            Mathf.Abs(note1._lineLayer - note2._lineLayer)
        ) >= 2;
    }

    private void CheckWindow(BeatmapNote note, ref BeatmapNote lastNote, int[] swingCount, float realTime, float songBPM)
    {
        if (lastNote != null)
        {
            if ((MaybeWindowed(note, lastNote) &&
                (note._time - lastNote._time) / songBPM * 60 > MaximumWindowTolerance) ||
                (note._time - lastNote._time) / songBPM * 60 > MaximumTolerance) {
                swingCount[Mathf.FloorToInt(realTime)] += 1;
            }
        }
        else
        {
            swingCount[Mathf.FloorToInt(realTime)] += 1;
        }
        lastNote = note;
    }

    private int[][] SwingCount(float songBPM)
    {
        if (NotesCount == 0) {
            return new int[2][];
        }

        // Get the timing of the last interaction in seconds and initialize an array 
        // with that number of buckets to tally swings occuring in each second
        float lastInteraction = LastInteractiveObjectTime(songBPM);
        int[] swingCountRed = new int[Mathf.FloorToInt(lastInteraction) + 1];
        int[] swingCountBlue = new int[Mathf.FloorToInt(lastInteraction) + 1];

        BeatmapNote lastRed = null, lastBlue = null;
        foreach (BeatmapNote note in notes.LoadedObjects)
        {
            float real_time = note._time / songBPM * 60;
            if (note._type == 0)
            {
                CheckWindow(note, ref lastRed, swingCountRed, real_time, songBPM);
            }
            else if (note._type == 1)
            {
                CheckWindow(note, ref lastBlue, swingCountBlue, real_time, songBPM);
            }
        }

        return new int[][] { swingCountRed, swingCountBlue };
    }

    private string ConvertTime(int time)
    {
        return string.Format("{0:D2}:{1:D2}", time / 60, time % 60);
    }

    public void Update()
    {
        int interval = 10;
        float songBPM = BeatSaberSongContainer.Instance.song.beatsPerMinute;

        int[][] swings = SwingCount(songBPM);
        int[] red = swings[0];
        int[] blue = swings[1];

        int[] swingCountList = new int[red.Length];
        for (int x = 0; x < red.Length; x++)
        {
            swingCountList[x] = red[x] + blue[x];
        }

        if (interval < 1) {
            Debug.LogWarning("Interval cannot be less than 1");
            return;
        }
        if (swingCountList.Sum() == 0) {
            Total = new Stats(0, 0, 0);
            //Debug.LogWarning("Map has no notes");
            return;
        }

        // Used to calculate median and SPS across the set of all intervals
        var redSpsPerInterval = new List<double>();
        var blueSpsPerInterval = new List<double>();
        var totalSpsPerInterval = new List<double>();

        for (var i = 0; i < swingCountList.Length; i += interval)
        {
            double intervalLength = i + interval > swingCountList.Length ? swingCountList.Length - i : interval;

            var swingCountSlice = swingCountList.Skip(i).Take(interval);
            double totalSps = swingCountSlice.Sum() / intervalLength;

            var redSlice = red.Skip(i).Take(interval);
            double redSps = redSlice.Sum() / intervalLength;

            var blueSlice = blue.Skip(i).Take(interval);
            double blueSps = blueSlice.Sum() / intervalLength;

            /*Debug.LogFormat("{0} to {1}: R({2:0.00})|B({3:0.00})|T({4:0.00})",
                ConvertTime(i),
                ConvertTime(i + (int)intervalLength - 1),
                red_sps, blue_sps, total_sps
            );*/

            blueSpsPerInterval.Add(blueSps);
            redSpsPerInterval.Add(redSps);
            totalSpsPerInterval.Add(totalSps);
        }

        float firstInteractionTime = FirstInteractiveObjectTime(songBPM);
        float lastInteractionTime = LastInteractiveObjectTime(songBPM);

        Red = new Stats(
            red.Sum() / (lastInteractionTime - firstInteractionTime),
            CalculateMaxRollingSps(red, interval),
            Median(redSpsPerInterval)
        );

        Blue = new Stats(
            blue.Sum() / (lastInteractionTime - firstInteractionTime),
            CalculateMaxRollingSps(blue, interval),
            Median(blueSpsPerInterval)
        );

        Total = new Stats(
            swingCountList.Sum() / (lastInteractionTime - firstInteractionTime),
            CalculateMaxRollingSps(swingCountList, interval),
            Median(totalSpsPerInterval)
        );

        /*Debug.LogFormat("Normalized Deviation: R({0})|B({})|T({})".format(
            round(statistics.pstdev(red_sps_per_interval) / red_total if red_total > 0 else 1, 2),
            round(statistics.pstdev(blue_sps_per_interval) / blue_total if blue_total > 0 else 1, 2),
            round(statistics.pstdev(total_sps_per_interval) / total if total > 0 else 1, 2)))
        if len(red_sps_per_interval) > 1 and len(blue_sps_per_interval) > 1:
            print("Variance: R({})|B({})|T({})".format(
                round(statistics.variance(red_sps_per_interval), 2),
                round(statistics.variance(blue_sps_per_interval), 2),
                round(statistics.variance(total_sps_per_interval, 2))))
        return total*/
    }

    public void Log()
    {
        Debug.Log("-----------------------------------------");
        Debug.LogFormat("[ Overall | Peak | Median ] Red SPS:\n[ {0:0.00} | {1:0.00} | {2:0.00} ]",
            Red.Overall, Red.Peak, Red.Median);

        Debug.LogFormat("[ Overall | Peak | Median ] Blue SPS:\n[ {0:0.00} | {1:0.00} | {2:0.00} ]",
            Blue.Overall, Blue.Peak, Blue.Median);

        Debug.LogFormat("[ Overall | Peak | Median ] Combined SPS:\n[ {0:0.00} | {1:0.00} | {2:0.00} ]",
            Total.Overall, Total.Peak, Total.Median);

        Debug.LogFormat("Overall Combined SPS: {0:0.00}", Total.Overall);
    }

    private double Median(List<double> xs)
    {
        if (xs.Count == 0) return 0;

        var ys = xs.OrderBy(x => x).ToList();
        double mid = (ys.Count - 1) / 2.0;
        return (ys[(int)(mid)] + ys[(int)(mid + 0.5)]) / 2;
    }

    private float CalculateMaxRollingSps(int[] spsList, int interval)
    {
        if (spsList.Length == 0) {
            return 0;
        }
        if (spsList.Length < interval) {
            return spsList.Sum() / spsList.Length;
        }
        int currentSps = spsList.Take(interval).Sum();
        int maxSps = currentSps;
        for (var x = 0; x < spsList.Length - interval; x++)
        {
            currentSps = currentSps - spsList[x] + spsList[x + interval];
            maxSps = Mathf.Max(maxSps, currentSps);
        }

        return maxSps / (float)interval;
    }

    public class Stats
    {
        public readonly float Overall;
        public readonly float Peak;
        public readonly float Median;

        public Stats(float overall, float peak, double median)
        {
            Overall = overall;
            Peak = peak;
            Median = (float) median;
        }
    }

    public Stats Blue { get; private set; } = null;
    public Stats Red { get; private set; } = null;
    public Stats Total { get; private set; } = null;
}
