using System;
using UnityEngine;

// ReSharper disable CompareOfFloatsByEqualityOperator

public sealed class TweenFloat
{
    public float StartTime;
    public float EndTime;

    public float StartValue;
    public float EndValue;

    public float Current;

    public Func<float, float> Easing;
    public Action<float> Callback;

    public bool UpdateTime(float time)
    {
        var prev = Current;
        if (time > EndTime) return prev != (Current = EndValue);
        if (time < StartTime) return prev != (Current = StartValue);
        return prev
            != (Current = Mathf.LerpUnclamped(
                StartValue,
                EndValue,
                Easing(Mathf.InverseLerp(StartTime, EndTime, time))));
    }

    public void UpdateWithCallback(float time)
    {
        if (UpdateTime(time)) Callback(Current);
    }

    public TweenFloat(
        float startTime,
        float endTime,
        float startValue,
        float endValue,
        Func<float, float> easing,
        Action<float> callback)
    {
        StartTime = startTime;
        StartValue = startValue;
        EndTime = endTime;
        EndValue = endValue;
        Easing = easing;
        Callback = callback;
    }

    public TweenFloat(float startTime, float endTime, float startValue, float endValue, Action<float> callback)
    {
        StartTime = startTime;
        StartValue = startValue;
        EndTime = endTime;
        EndValue = endValue;
        Easing = global::Easing.Linear;
        Callback = callback;
    }
}
