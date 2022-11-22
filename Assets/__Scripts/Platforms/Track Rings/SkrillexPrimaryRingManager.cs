using SimpleJSON;

public class SkrillexPrimaryRingManager : TrackLaneRingsManager
{
    protected override bool IsAffectedByZoom() => true;

    public override void HandlePositionEvent(JSONNode customData = null)
    {
        // Do nothing
    }

    public override void HandleRotationEvent(JSONNode customData = null)
    {
        base.HandleRotationEvent(customData);
        base.HandlePositionEvent(customData);
    }
}
