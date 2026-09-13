using System;
using System.Linq;
using Beatmap.Enums;
using UnityEngine;

public class TransformSpectrogramData : EnvironmentComponentData<TransformSpectrogram>
{
    public int[] Transforms;
    public string Axis;
    public float MinPosition;
    public float MaxPosition;
    public bool ScaleSamples = true;
    public float Scale = 1f;

    public override void
        FillComponents(GameObject self, TransformSpectrogram comp, CreateContainer container)
    {
        comp.SpectrogramDataProvider = container.Descriptor.SpectrogramDataProvider;
        comp.Transforms = Transforms.Select(container.GetComponentOrNull<Transform>).ToArray();
        comp.Axis = Enum.Parse<Axis>(Axis);
        comp.MinPosition = MinPosition;
        comp.MaxPosition = MaxPosition;
        comp.ScaleSamples = ScaleSamples;
        comp.Scale = Scale;
    }
}
