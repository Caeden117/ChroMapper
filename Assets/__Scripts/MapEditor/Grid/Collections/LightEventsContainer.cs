using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightEventsContainer : BeatmapObjectContainerCollection
{
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightEvent;

    public override BeatmapObjectContainer CreateContainer() => throw new System.NotImplementedException();
    internal override void SubscribeToCallbacks() => throw new System.NotImplementedException();
    internal override void UnsubscribeToCallbacks() => throw new System.NotImplementedException();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
