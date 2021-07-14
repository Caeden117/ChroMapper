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

        return Intersections.CurrentGroup - (offset > 0 ? offset : offset - 1);
    };

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Transform trackTransform;

    private Transform t;

    private void Start()
    {
        t = transform;
        Intersections.NextGroupSearchFunction = AlternatingChunkFunc;
    }

    private void Update()
    {
        var beat = trackTransform.InverseTransformPoint(t.position).z / EditorScaleController.EditorScale;
        var chunk = (int)(beat / Intersections.ChunkSize);

        Intersections.CurrentGroup = chunk;
    }
}
