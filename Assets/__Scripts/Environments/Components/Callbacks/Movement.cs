using UnityEngine;

public class Movement : MonoBehaviour
{
    public Transform[] Transforms;
    public Vector3[] MovementData;
    public float TransitionSpeed;

    private Vector3[] startLocalPositions;
    private bool initialized;

    private void Start() => Initialize();

    public void Initialize()
    {
        if (initialized)
            return;
        if (Transforms == null)
            throw new System.InvalidOperationException($"Movement '{name}' has no transforms collection.");
        if (MovementData == null || MovementData.Length == 0)
            throw new System.InvalidOperationException($"Movement '{name}' has no movement data.");

        startLocalPositions = new Vector3[Transforms.Length];
        for (var i = 0; i < Transforms.Length; i++)
        {
            if (Transforms[i] == null)
                throw new System.InvalidOperationException(
                    $"Movement '{name}' has an unassigned transform at index {i}.");

            startLocalPositions[i] = Transforms[i].localPosition;
        }

        SetLocalPositionOffsetsForAllObjects(MovementData[0]);
        initialized = true;
    }

    public void Apply(Vector3 localPositionOffset)
    {
        SetLocalPositionOffsetsForAllObjects(localPositionOffset);
    }

    private void SetLocalPositionOffsetsForAllObjects(Vector3 localPositionOffset)
    {
        for (var i = 0; i < Transforms.Length; i++)
            Transforms[i].localPosition = startLocalPositions[i] + localPositionOffset;
    }
}
