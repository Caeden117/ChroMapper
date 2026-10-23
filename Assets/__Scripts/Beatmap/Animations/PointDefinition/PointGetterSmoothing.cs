using System;
using UnityEngine;

public class PointGetterSmoothing
{
    private float previous;
    private readonly float smoothFactor;
    private readonly Func<float> getter;
    private int sampledVersion = -1;

    public float Value
    {
        get
        {
            PointDefinitionParser.BeginSample();
            if (sampledVersion == PointDefinitionParser.SampleVersion) return previous;
            sampledVersion = PointDefinitionParser.SampleVersion;
            var current = getter();
            previous = PointDefinitionParser.ResetSmoothing
                ? current
                : Mathf.Lerp(previous, current, smoothFactor * TimeHelper.DeltaTime);
            return previous;
        }
    }
    public float GetValue() => Value;

    public PointGetterSmoothing(Func<float> getter, float smoothFactor)
    {
        this.getter = getter;
        this.smoothFactor = smoothFactor;
    }
}
