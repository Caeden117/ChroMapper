using Beatmap.Base;

public class SkrillexPrimaryRingManager : TrackLaneRingsManager
{
    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(BaseEvent evt)
    {
        // Do nothing
    }

    public override void HandleRotationEvent(BaseEvent evt)
    {
        base.HandleRotationEvent(evt);
        base.HandlePositionEvent(evt);
    }
}
