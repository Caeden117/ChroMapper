using System.Collections.Generic;
using UnityEngine;

public abstract class InterscopeCarEventHandler : PlatformEventHandler
{
    [SerializeField] protected Rigidbody carRigidbody;
    [Tooltip("All Cars = 0 & 1\nLeft Side = 2\nRight Side = 3\nFirst Row = 4\nSecond Row = 5\nThird Row = 6\nFourth Row = 7")]
    [SerializeField] protected int[] carFlags;

    private HashSet<int> eventValuesHash;

    protected virtual void Start()
    {
        eventValuesHash = new HashSet<int>(carFlags);
    }

    public override void OnEventTrigger(int type, MapEvent @event)
    {
        // Values 0 and 1 affect all cars, but after that, the events affect different cars depending on their flags.
        if (@event._value == 0
            || @event._value == 1
            || eventValuesHash.Contains(@event._value))
        {
            OnCarGroupTriggered(@event);
        }
    }

    protected abstract void OnCarGroupTriggered(MapEvent @event);
}
