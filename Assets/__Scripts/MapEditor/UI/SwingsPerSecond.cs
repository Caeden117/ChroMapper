using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SwingsPerSecond
{
    private readonly float maximumTolerance = .06f; // Magic number based on maximum tolerated swing speed
    private readonly float maximumWindowTolerance = .07f; // For windowed sliders

    private readonly NotesContainer notes;
    private readonly ObstaclesContainer obstacles;

    public SwingsPerSecond(NotesContainer notes, ObstaclesContainer obstacles)
    {
        this.notes = notes;
        this.obstacles = obstacles;
    }

    private int NotesCount => notes.LoadedObjects.Count;

    public Stats Blue { get; private set; }
    public Stats Red { get; private set; }
    public Stats Total { get; private set; }

    private float LastInteractiveObjectTime(float songBpm)
    {
        var lastNoteTime = 0f;
        if (NotesCount > 0) lastNoteTime = notes.LoadedObjects.Last().Time / songBpm * 60;

        var lastInteractiveObstacleTime = 0f;
        foreach (BeatmapObstacle obstacle in obstacles.LoadedObjects)
        {
            if (obstacle.Width >= 2 || obstacle.LineIndex == 1 || obstacle.LineIndex == 2)
            {
                var obstacleEnd = (obstacle.Time + obstacle.Duration) / songBpm * 60;
                lastInteractiveObstacleTime = Mathf.Max(lastInteractiveObstacleTime, obstacleEnd);
            }
        }

        return Mathf.Max(lastInteractiveObstacleTime, lastNoteTime);
    }

    private float FirstInteractiveObjectTime(float songBpm)
    {
        var firstNoteTime = float.MaxValue;
        if (NotesCount > 0) firstNoteTime = notes.LoadedObjects.First().Time / songBpm * 60;

        var firstInteractiveObstacleTime = float.MaxValue;
        foreach (BeatmapObstacle obstacle in obstacles.LoadedObjects)
        {
            if (obstacle.Width >= 2 || obstacle.LineIndex == 1 || obstacle.LineIndex == 2)
            {
                firstInteractiveObstacleTime = (obstacle.Time + obstacle.Duration) / songBpm * 60;
                break;
            }
        }

        return Mathf.Min(firstInteractiveObstacleTime, firstNoteTime);
    }

    private bool MaybeWindowed(BeatmapNote note1, BeatmapNote note2) =>
        Mathf.Max(
            Mathf.Abs(note1.LineIndex - note2.LineIndex),
            Mathf.Abs(note1.LineLayer - note2.LineLayer)
        ) >= 2;

    private void CheckWindow(BeatmapNote note, ref BeatmapNote lastNote, int[] swingCount, float realTime,
        float songBpm)
    {
        if (lastNote != null)
        {
            if ((MaybeWindowed(note, lastNote) &&
                 (note.Time - lastNote.Time) / songBpm * 60 > maximumWindowTolerance) ||
                (note.Time - lastNote.Time) / songBpm * 60 > maximumTolerance)
            {
                swingCount[Mathf.FloorToInt(realTime)] += 1;
            }
        }
        else
        {
            swingCount[Mathf.FloorToInt(realTime)] += 1;
        }

        lastNote = note;
    }

    private int[][] SwingCount(float songBpm)
    {
        if (NotesCount == 0) return new[] { Array.Empty<int>(), Array.Empty<int>() };

        // Get the timing of the last interaction in seconds and initialize an array 
        // with that number of buckets to tally swings occuring in each second
        var lastInteraction = LastInteractiveObjectTime(songBpm);
        var swingCountRed = new int[Mathf.FloorToInt(lastInteraction) + 1];
        var swingCountBlue = new int[Mathf.FloorToInt(lastInteraction) + 1];

        BeatmapNote lastRed = null, lastBlue = null;
        foreach (BeatmapNote note in notes.LoadedObjects)
        {
            var realTime = note.Time / songBpm * 60;
            if (note.Type == 0)
                CheckWindow(note, ref lastRed, swingCountRed, realTime, songBpm);
            else if (note.Type == 1) CheckWindow(note, ref lastBlue, swingCountBlue, realTime, songBpm);
        }

        return new[] { swingCountRed, swingCountBlue };
    }

    public void Update()
    {
        var interval = 10;
        var songBpm = BeatSaberSongContainer.Instance.Song.BeatsPerMinute;

        var swings = SwingCount(songBpm);
        var red = swings[0];
        var blue = swings[1];

        var swingCountList = new int[red.Length];
        for (var x = 0; x < red.Length; x++) swingCountList[x] = red[x] + blue[x];

        if (interval < 1)
        {
            Debug.LogWarning("Interval cannot be less than 1");
            return;
        }

        if (swingCountList.Sum() == 0)
        {
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
            var totalSps = swingCountSlice.Sum() / intervalLength;

            var redSlice = red.Skip(i).Take(interval);
            var redSps = redSlice.Sum() / intervalLength;

            var blueSlice = blue.Skip(i).Take(interval);
            var blueSps = blueSlice.Sum() / intervalLength;

            /*Debug.LogFormat("{0} to {1}: R({2:0.00})|B({3:0.00})|T({4:0.00})",
                ConvertTime(i),
                ConvertTime(i + (int)intervalLength - 1),
                red_sps, blue_sps, total_sps
            );*/

            blueSpsPerInterval.Add(blueSps);
            redSpsPerInterval.Add(redSps);
            totalSpsPerInterval.Add(totalSps);
        }

        var firstInteractionTime = FirstInteractiveObjectTime(songBpm);
        var lastInteractionTime = LastInteractiveObjectTime(songBpm);

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
        var mid = (ys.Count - 1) / 2.0;
        return (ys[(int)mid] + ys[(int)(mid + 0.5)]) / 2;
    }

    private float CalculateMaxRollingSps(int[] spsList, int interval)
    {
        if (spsList.Length == 0) return 0;
        if (spsList.Length < interval) return spsList.Sum() / spsList.Length;
        var currentSps = spsList.Take(interval).Sum();
        var maxSps = currentSps;
        for (var x = 0; x < spsList.Length - interval; x++)
        {
            currentSps = currentSps - spsList[x] + spsList[x + interval];
            maxSps = Mathf.Max(maxSps, currentSps);
        }

        return maxSps / (float)interval;
    }

    public class Stats
    {
        public readonly float Median;
        public readonly float Overall;
        public readonly float Peak;

        public Stats(float overall, float peak, double median)
        {
            Overall = overall;
            Peak = peak;
            Median = (float)median;
        }
    }
}
