using System;
using UnityEngine;

public class FloatTween
{
    public float StartTime;
    public float EndTime;

    public float StartValue;
    public float EndValue;

    public float Current;

    public Func<float, float> Easing = global::Easing.Step;

    public bool UpdateTime(float time) =>
        Current
        != (Current = Mathf.LerpUnclamped(
            StartValue,
            EndValue,
            Easing(Mathf.InverseLerp(StartTime, EndTime, time))));
}
