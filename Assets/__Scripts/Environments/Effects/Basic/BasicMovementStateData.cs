using Beatmap.Base;

// Snapshot data for a single node in a Basic Event movement timeline.
// The snapshot stores the visual transform at the start of the node, plus the
// parameters that drive the interval until the next node, so we can evaluate the
// effect at any later time without running Time.deltaTime-based simulation.
public abstract class BasicMovementStateData : BasicEventStateData
{
    protected BasicMovementStateData(BaseEvent data) : base(data)
    {
    }

    public bool SnapshotValid;

    // Carry Beat Saber's same-type index through snapshot traversal so hot-path effects do not scan the container.
    public int SameTypeIndex;
}
