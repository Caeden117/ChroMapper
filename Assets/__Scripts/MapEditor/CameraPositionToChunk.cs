using System;
using UnityEngine;

public class CameraPositionToChunk : MonoBehaviour
{
    // Used when facing towards positive time; simply increases chunk ID to check
    private static readonly Func<int, int> IncreasingChunkFunc = x => ++x;

    // Used when facing towards negative time; simply decreases chunk ID to check
    private static readonly Func<int, int> DecreasingChunkFunc = x => --x;

    // Used when our camera is somewhere in the middle, and the previous two methods aren't garaunteed to work.
    // We take our current group, then alternate one spot in both positive and negative directions.
    // For example, if our center is at ID 5, then this method will check like so: 5, 6, 4, 7, 3, 8, 2, 9, 1, ...
    private static readonly Func<int, int> AlternatingChunkFunc = x =>
    {
        var offset = x - Intersections.CurrentGroup;

        var index = (int)Mathf.Max(1, (Mathf.Abs(offset) * 2) - ((Mathf.Sign(offset) - 1) / 2f));

        return Intersections.CurrentGroup + (Mathf.CeilToInt(index / 2f) * ((index % 2 * 2) - 1));
    };

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Transform trackTransform;

    private Transform t;

    private void Start() => t = transform;

    private void Update()
    {
        var beat = trackTransform.InverseTransformPoint(t.position).z / EditorScaleController.EditorScale;
        var chunk = (int)(beat / Intersections.ChunkSize);

        var forward = trackTransform.forward;
        var cameraForward = t.forward;

        // Use fancy dot product math to determine which chunk method to use for best performance.
        var dot = VectorUtils.FastDot(in forward, in cameraForward);

        // If we're facing down the track towards positive time, use increasing chunk IDs
        if (dot > 0.5f)
        {
            Intersections.NextGroupSearchFunction = IncreasingChunkFunc;
        }
        // If we're facing down the track towards negative time, use decreasing chunk IDs
        else if (dot < -0.5f)
        {
            Intersections.NextGroupSearchFunction = DecreasingChunkFunc;
        }
        // If we're in a weird middle state, use an alternating combination
        else
        {
            Intersections.NextGroupSearchFunction = AlternatingChunkFunc;
        }

        Intersections.CurrentGroup = chunk;
    }
}
