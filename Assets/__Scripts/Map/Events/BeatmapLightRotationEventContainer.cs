using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapLightRotationEventContainer : BeatmapObjectContainer
{
    public BeatmapLightRotationEvent RotationEventData;
    public LightRotationEventsContainer RotationEventsContainer;

    public override BeatmapObject ObjectData { get => RotationEventData; set => RotationEventData = (BeatmapLightRotationEvent)value; }

    public override void UpdateGridPosition()
    {
        var position = RotationEventData.GetPosition();
        transform.localPosition = new Vector3(
            position.x,
            -position.y,
            RotationEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightRotationEventContainer SpawnLightRotationEvent(LightRotationEventsContainer rotationEventsContainer, BeatmapLightRotationEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightRotationEventContainer>();
        container.RotationEventData = data;
        container.RotationEventsContainer = rotationEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
