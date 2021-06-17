using System;
using UnityEngine;

public class CameraPositionToChunk : MonoBehaviour
{
    private static readonly Func<int, int> IncreasingChunkFunc = x => ++x;
    private static readonly Func<int, int> DecreasingChunkFunc = x => --x;
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
        var chunk = (int)(beat / BeatmapObjectContainerCollection.ChunkSize);

        var forward = trackTransform.forward;
        var cameraForward = t.forward;

        var dot = VectorUtils.FastDot(in forward, in cameraForward);

        if (dot > 0.5f)
        {
            Intersections.NextGroupSearchFunction = IncreasingChunkFunc;
        }
        else if (dot < -0.5f)
        {
            Intersections.NextGroupSearchFunction = DecreasingChunkFunc;
        }
        else
        {
            Intersections.NextGroupSearchFunction = AlternatingChunkFunc;
        }

        Intersections.CurrentGroup = chunk;
    }
}
