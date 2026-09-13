using System;
using UnityEngine;

public class LightPairSinMove : MonoBehaviour
{
    public TransformContainer[] Transforms = new TransformContainer[2];

    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    private bool initialized;

    private void Awake() => TryInitializeTransforms();

    public void Initialize()
    {
        if (!TryInitializeTransforms())
            throw new InvalidOperationException($"Light pair sine movement '{name}' requires two initialized transforms.");
    }

    public void Apply(float leftPhase, float rightPhase)
    {
        // Direct phase evaluation makes pause, rewind, and arbitrary playhead jumps
        // agree with continuous song-time playback.
        Apply(Transforms[0], leftPhase);
        Apply(Transforms[1], rightPhase);
    }

    private bool TryInitializeTransforms()
    {
        if (initialized)
            return true;
        if (Transforms == null || Transforms.Length < 2)
            return false;
        if (Transforms[0] == null || Transforms[0].Transform == null
            || Transforms[1] == null || Transforms[1].Transform == null)
        {
            return false;
        }

        for (var i = 0; i < 2; i++)
        {
            var container = Transforms[i];
            container.Side = i == 0 ? 1f : -1f;
            container.StartPosition = container.Transform.localPosition;
        }

        initialized = true;
        return true;
    }

    private void Apply(TransformContainer container, float phase)
    {
        var vector = Vector3.LerpUnclamped(
            StartPositionOffset,
            EndPositionOffset,
            (Mathf.Sin(phase) * 0.5f) + 0.5f);
        vector.x *= container.Side;
        container.Transform.localPosition = container.StartPosition + vector;
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public float Side;
    }
}
