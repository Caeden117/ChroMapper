using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackLaneRingsManagerData : EnvironmentComponentData<TrackLaneRingsManager>
{
    public int[] Rings;
    public float RingPositionZStep;
    public bool SpawnAsChildren;

    public override void FillComponents(GameObject self, TrackLaneRingsManager comp, CreateContainer container)
    {
        comp.Rings = Rings is null
            ? new List<TrackLaneRing>()
            : Rings.Select(container.GetComponentOrNull<TrackLaneRing>).ToList();
        comp.RingPositionStep = RingPositionZStep;
        comp.SpawnAsChildren = SpawnAsChildren;
    }
}
