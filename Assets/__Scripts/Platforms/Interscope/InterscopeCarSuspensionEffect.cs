using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InterscopeCarSuspensionEffect : InterscopeCarEventHandler
{
    [SerializeField] private float contractDistance = 0.35f;
    [SerializeField] private float expandDistance = 0.45f;

    private SpringJoint frontWheelSpringJoint;

    protected override void Start()
    {
        base.Start();

        // ok so like this is a pretty jank way to do it but im lazy
        frontWheelSpringJoint = GetComponentsInChildren<SpringJoint>()
            .Where(x => x.connectedBody.name.Contains("FrontWheel"))
            .FirstOrDefault();
    }

    public override int[] ListeningEventTypes => new[]
    { 
        MapEvent.EVENT_TYPE_CUSTOM_EVENT_1,
        MapEvent.EVENT_TYPE_CUSTOM_EVENT_2
    };

    protected override void OnCarGroupTriggered(MapEvent @event)
    {
        if (@event._type == MapEvent.EVENT_TYPE_CUSTOM_EVENT_1)
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = expandDistance;
            carRigidbody.WakeUp();
        }
        else
        {
            frontWheelSpringJoint.minDistance = frontWheelSpringJoint.maxDistance = contractDistance;
            carRigidbody.WakeUp();
        }
    }
}