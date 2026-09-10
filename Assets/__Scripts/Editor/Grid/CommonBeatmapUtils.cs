using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public static class CommonBeatmapUtils
{
    private static readonly float[] CutDirectionAngles =
    {
        180f, 0f, 270f, 90f, 225f, 135f, 315f, 45f, 0f
    };

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