using System.Linq;
using UnityEngine;

public class InterscopeCarSuspensionEffect : InterscopeCarEventHandler
{
    [SerializeField] private float contractDistance = 0.35f;
    [SerializeField] private float expandDistance = 0.45f;

    private SpringJoint frontWheelSpringJoint;

    public override int[] ListeningEventTypes => new[]
    {
        MapEvent.EventTypeCustomEvent1, MapEvent.EventTypeCustomEvent2
    };

    protected override void Start()
    {
        base.Start();

        // ok so like this is a pretty jank way to do it but im lazy
        frontWheelSpringJoint = GetComponentsInChildren<SpringJoint>()
            .Where(x => x.connectedBody.name.Contains("FrontWheel"))
            .FirstOrDefault();
    }

    protected override void OnCarGroupTriggered(MapEvent @event)
    {
        if (@event.Type == MapEvent.EventTypeCustomEvent1)
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = expandDistance;
            CarRigidbody.WakeUp();
        }
        else
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = contractDistance;
            CarRigidbody.WakeUp();
        }
    }
}
