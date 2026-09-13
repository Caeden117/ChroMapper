using System;
using UnityEngine;

public class Vector3Tween
{
    public float StartTime;
    public float EndTime;

    public Vector3 StartValue;
    public Vector3 EndValue;

    public Vector3 Current;

    public Func<float, float> Easing = global::Easing.Step;

    public bool UpdateTime(float time) =>
        Current
        != (Current = Vector3.LerpUnclamped(
            StartValue,
            EndValue,
            Easing(Mathf.InverseLerp(StartTime, EndTime, time))));
}
