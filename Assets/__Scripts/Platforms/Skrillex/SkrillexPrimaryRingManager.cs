using Beatmap.Base;

public class SkrillexPrimaryRingManager : TrackLaneRingsManager
{
    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(RingRotationState state, BaseEvent evt, int index)
    {
        // Do nothing
    }

    public override void HandleRotationEvent(RingRotationState state, BaseEvent evt, int index)
    {
        base.HandleRotationEvent(state, evt, index);
        base.HandlePositionEvent(state, evt, index);
    }
}
