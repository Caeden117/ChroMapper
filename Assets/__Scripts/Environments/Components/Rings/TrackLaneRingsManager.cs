using System.Collections.Generic;
using UnityEngine;

public class TrackLaneRingsManager : MonoBehaviour
{
    public List<TrackLaneRing> Rings;
    public float RingPositionStep;
    public bool SpawnAsChildren;
    public bool UseCached;
    private AudioTimeSyncController atsc;

    private bool hasAtsc;

    public AudioTimeSyncController Atsc
    {
        get => atsc;
        set
        {
            atsc = value;
            hasAtsc = atsc != null;
        }
    }

    public void Start()
    {
        hasAtsc = Atsc != null;
        var forward = transform.forward;
        var len = Rings.Count;
        for (var i = 0; i < len; i++)
        {
            var t = Rings[i];
            if (SpawnAsChildren)
            {
                var pos = new Vector3(0f, 0f, i * RingPositionStep);
                t.Init(pos, Vector3.zero);
            }
            else
            {
                var pos = forward * (i * RingPositionStep);
                t.Init(pos, transform.position);
            }
        }
    }

    private void FixedUpdate()
    {
        if (UseCached)
            return;

        var fdt = TimeHelper.FixedDeltaTime;
        var len = Rings.Count;
        for (var i = 0; i < len; i++)
            Rings[i].FixedUpdateRing(fdt);
    }

    private void LateUpdate()
    {
        if (UseCached || !hasAtsc)
            return;

        var intF = TimeHelper.InterpolationFactor;
        var len = Rings.Count;
        for (var i = 0; i < len; i++)
            Rings[i].LateUpdateRing(intF);
    }
}
