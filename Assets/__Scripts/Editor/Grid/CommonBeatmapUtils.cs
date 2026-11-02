using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class CommonBeatmapUtils
{
    private static readonly float[] CutDirectionAngles =
    {
        180f, 0f, 270f, 90f, 225f, 135f, 315f, 45f, 0f
    };

    public static float GetFinalSongJsonTime(AudioTimeSyncController atsc) =>
        (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(
            atsc.GetBeatFromSeconds(atsc.SongAudioSource.clip.length));

    public static void GetJsonTimeRange(BaseObject obj, out float start, out float end)
    {
        start = end = obj.JsonTime;
        switch (obj)
        {
            case BaseObstacle wall:
                start = Mathf.Min(start, wall.JsonTime + wall.Duration);
                end = Mathf.Max(end, wall.JsonTime + wall.Duration);
                break;
            case BaseSlider slider:
                start = Mathf.Min(start, slider.TailJsonTime);
                end = Mathf.Max(end, slider.TailJsonTime);
                break;
            case BaseEventBoxGroup group:
                if (group.OrderedEventsInitialized)
                {
                    if (group.OrderedEvents.Count > 0)
                        end += Mathf.Max(0f, group.OrderedEvents[group.OrderedEvents.Count - 1].RelativeJsonTime);
                    break;
                }
                foreach (var box in group.ReadOnlyBoxes)
                {
                    var events = box.ReadOnlyEvents;
                    if (events.Count > 0)
                        end = Mathf.Max(end, group.JsonTime + events[events.Count - 1].RelativeJsonTime);
                }
                break;
        }
    }

    public static bool TryClampTimeOffset(HashSet<BaseObject> objects, float requestedOffset,
        float finalJsonTime, out float offset)
    {
        var minimumOffset = float.NegativeInfinity;
        var maximumOffset = float.PositiveInfinity;
        foreach (var obj in objects)
        {
            GetJsonTimeRange(obj, out var start, out var end);
            minimumOffset = Mathf.Max(minimumOffset, -start);
            maximumOffset = Mathf.Min(maximumOffset, finalJsonTime - end);
            if (obj is BaseGLSEvent evt)
                minimumOffset = Mathf.Max(minimumOffset, -evt.RelativeJsonTime);
        }

        offset = Mathf.Clamp(requestedOffset, minimumOffset, maximumOffset);
        return minimumOffset <= maximumOffset;
    }

    /// <summary>
    /// Moving BPM events changes the song-end beat. Validate the offset against the resulting tempo map
    /// and reject it if the moved range exceeds the audio clip. Selections without BPM events bypass
    /// this check and report <paramref name="changesTempo"/> = false.
    /// </summary>
    public static bool TryClampOffsetWhenMovingBpmEvents(HashSet<BaseObject> objects, AudioTimeSyncController atsc,
        bool paste, bool overwrite, ref float offset, out bool changesTempo)
    {
        changesTempo = false;
        foreach (var obj in objects)
        {
            if (obj is not BaseBpmEvent)
                continue;
            changesTempo = true;
            break;
        }
        var feasible = !changesTempo || IsTempoOffsetWithinSongBounds(objects, atsc, paste, overwrite, offset);
        if (!feasible)
        {
            PersistentUI.Instance.DisplayMessage(
                "Mapper", "selection.bpmoutside", PersistentUI.DisplayMessageType.Bottom);
        }
        return feasible;
    }

    /// <summary>
    /// Tempo edits change the song-end beat, so validate the requested offset against the merged tempo map.
    /// </summary>
    private static bool IsTempoOffsetWithinSongBounds(
        HashSet<BaseObject> objects,
        AudioTimeSyncController atsc,
        bool paste,
        bool overwrite,
        float offset)
    {
        var moving = new List<BaseBpmEvent>();
        var lastBeat = float.NegativeInfinity;
        var firstHead = float.PositiveInfinity;
        var lastHead = float.NegativeInfinity;
        foreach (var obj in objects)
        {
            GetJsonTimeRange(obj, out _, out var end);
            lastBeat = Mathf.Max(lastBeat, end);
            firstHead = Mathf.Min(firstHead, obj.JsonTime);
            lastHead = Mathf.Max(lastHead, obj.JsonTime);
            if (obj is BaseBpmEvent bpm)
                moving.Add(bpm);
        }

        moving.Sort();
        var song = BeatSaberSongContainer.Instance;
        var stationary = new List<BaseBpmEvent>();
        foreach (var bpm in song.Map.BpmEvents)
        {
            if (paste || !objects.Contains(bpm))
                stationary.Add(bpm);
        }
        var songBpm = song.Info.BeatsPerMinute;
        var songEnd = atsc.GetBeatFromSeconds(atsc.SongAudioSource.clip.length);

        // Mirror BPM ordering and paste conflict replacement without cloning beatmap objects or rebuilding Unity preview caches.
        var limit = lastBeat + offset;
        var staticIndex = 0;
        var movingIndex = 0;
        var previousBeat = 0f;
        var previousBpm = songBpm;
        var songTime = 0f;
        while (staticIndex < stationary.Count || movingIndex < moving.Count)
        {
            var fixedBpm = staticIndex < stationary.Count ? stationary[staticIndex] : null;
            var movedBpm = movingIndex < moving.Count ? moving[movingIndex] : null;
            var movedBeat = movedBpm != null ? movedBpm.JsonTime + offset : float.PositiveInfinity;
            if (paste && fixedBpm != null
                && ((movedBpm != null && Mathf.Abs(fixedBpm.JsonTime - movedBeat) < BeatmapObjectContainerCollection.Epsilon)
                    || (overwrite && fixedBpm.JsonTime >= firstHead + offset && fixedBpm.JsonTime <= lastHead + offset)))
            {
                staticIndex++;
                continue;
            }

            var useMoving = movedBpm != null && (fixedBpm == null || movedBeat < fixedBpm.JsonTime
                || (movedBeat == fixedBpm.JsonTime && movedBpm.Bpm <= fixedBpm.Bpm));
            var next = useMoving ? movedBpm : fixedBpm;
            var beat = useMoving ? movedBeat : next.JsonTime;
            if (beat >= limit)
                break;

            songTime += (beat - previousBeat) * (Mathf.Approximately(songBpm, previousBpm) ? 1f : songBpm / previousBpm);
            previousBeat = beat;
            previousBpm = next.Bpm;
            if (previousBpm <= 0f)
                return false;

            if (useMoving)
                movingIndex++;
            else
                staticIndex++;
        }

        songTime += (limit - previousBeat) * (Mathf.Approximately(songBpm, previousBpm) ? 1f : songBpm / previousBpm);
        return songTime <= songEnd;
    }

    public static float GetAngle(int cutDirection)
    {
        if (cutDirection < 0 || cutDirection >= CutDirectionAngles.Length)
            throw new ArgumentOutOfRangeException(nameof(cutDirection));
        return CutDirectionAngles[cutDirection];
    }

    public static Vector2 AngleToVector(float angle)
    {
        angle *= Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
    }

    public static float VectorToAngle(Vector2 vector)
    {
        vector.Normalize();
        return Mathf.Atan2(vector.x, -vector.y) * Mathf.Rad2Deg;
    }

    public static NoteCutDirection AngleToCutDirection(float angle, out float angleOffset, bool useAny = false)
    {
        if (useAny)
        {
            angleOffset = angle;
            return NoteCutDirection.Any;
        }

        angle = Mathf.Repeat(angle, 360f);

        var bestDir = 0;
        var bestDelta = float.MaxValue;
        for (var i = 0; i < CutDirectionAngles.Length; i++)
        {
            var delta = Mathf.DeltaAngle(angle, CutDirectionAngles[i]);
            if (Mathf.Abs(delta) < Mathf.Abs(bestDelta))
            {
                bestDelta = delta;
                bestDir = i;
            }
        }

        angleOffset = angle - CutDirectionAngles[bestDir];
        return (NoteCutDirection)bestDir;
    }

    public static float GetOverallCutAngle(BaseNote note)
    {
        var angle = GetAngle(note.CutDirection);
        if (note.AngleOffset != 0) angle += note.AngleOffset;
        return angle;
    }

    public static Vector2 GetOverallCutVector(BaseNote note)
    {
        var angle = GetOverallCutAngle(note);
        return AngleToVector(angle);
    }

    public static bool HeadPointsTowardTail(BaseNote head, BaseNote tail)
    {
        var headDir = head.CutDirection == (int)NoteCutDirection.Any ? Vector2.zero : GetOverallCutVector(head);
        var tailDir = tail.CutDirection == (int)NoteCutDirection.Any ? Vector2.zero : GetOverallCutVector(tail);
        if (Vector2.Dot(headDir, tailDir) < -0.9f)
            return false;

        var averageDir = (headDir + tailDir).normalized;
        // if both are dots, averageDir is zero; treat as not misaligned so no swap
        if (averageDir.sqrMagnitude < 0.001f) return true;

        var headPos = head.GetPosition();
        var tailPos = tail.GetPosition();

        // this fixes the case where a dot is sitting right next to an arrow note,
        // otherwise it wouldnt be consistent. Arrow chain is more common anyway.
        // probably can move this somewhere else because this *is* chain related,
        // but it also makes this a little more consistent for non chain cases if this
        // is ever to be used. idk.
        headPos -= headDir * 0.25f;
        tailPos -= tailDir * 0.25f;

        var headDot = Vector2.Dot(headPos, averageDir);
        var tailDot = Vector2.Dot(tailPos, averageDir);

        return headDot < tailDot;
    }
}
