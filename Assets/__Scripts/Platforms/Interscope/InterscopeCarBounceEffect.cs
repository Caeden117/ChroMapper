using Beatmap.Enums;
using Beatmap.Base;
using UnityEngine;

public class InterscopeCarBounceEffect : InterscopeCarEventHandler
{
    [SerializeField] private Rigidbody wheelRigidbody;
    [SerializeField] private Vector3 impulse;
    [SerializeField] private float forceRandomness = 0.5f;
    [SerializeField] private float eventDelay = 0.5f;

    private float timeSinceLastEvent;

    public override int[] ListeningEventTypes => new[] { (int)EventTypeValue.RingZoom };

    protected override void OnCarGroupTriggered(IEvent @event)
    {
        var timeSinceLevelLoad = Time.timeSinceLevelLoad;

        if (timeSinceLevelLoad - timeSinceLastEvent < eventDelay) return;

        timeSinceLastEvent = timeSinceLevelLoad;

        var t = transform;

        wheelRigidbody.AddForceAtPosition(impulse * (1 + Random.Range(-forceRandomness / 2f, forceRandomness / 2f)),
            t.position + (t.forward * 0.2f));

        CarRigidbody.WakeUp();
    }
}
