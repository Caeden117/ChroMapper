using Beatmap.Enums;
using UnityEngine;

public class TransformSpectrogram : MonoBehaviour
{
    [SerializeField] public SpectrogramDataProvider SpectrogramDataProvider;
    [SerializeField] public Transform[] Transforms;
    [SerializeField] public Axis Axis = Axis.Y;
    [SerializeField] public float MinPosition;
    [SerializeField] public float MaxPosition;
    [SerializeField] public bool ScaleSamples = true;
    [SerializeField] public float Scale = 1f;

    private Vector3 direction;
    private Vector3[] defaultPositions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        var a = Vector3.zero;
        switch (Axis)
        {
            case Axis.X:
                direction = new Vector3(1f, 0.0f, 0.0f);
                a = new Vector3(0.0f, 1f, 1f);
                break;
            case Axis.Y:
                direction = new Vector3(0.0f, 1f, 0.0f);
                a = new Vector3(1f, 0.0f, 1f);
                break;
            case Axis.Z:
                direction = new Vector3(0.0f, 0.0f, 1f);
                a = new Vector3(1f, 1f, 0.0f);
                break;
        }

        defaultPositions = new Vector3[Transforms.Length];
        for (var index = 0; index < Transforms.Length; ++index)
            defaultPositions[index] = Vector3.Scale(a, Transforms[index].localPosition);
    }

    private void Update()
    {
        for (var i = 0; i < Transforms.Length; i++)
        {
            var index = ScaleSamples
                ? Mathf.RoundToInt(
                    i / (Transforms.Length - 1f) * (SpectrogramDataProvider.NumberOfSamples - 1f) * Scale)
                : i;
            var t = SpectrogramDataProvider.ProcessedSamples[index % SpectrogramDataProvider.NumberOfSamples];
            var value = Mathf.Lerp(MinPosition, MaxPosition, t);
            Transforms[i].localPosition =
                defaultPositions[i] + (direction * value);
        }
    }
}
