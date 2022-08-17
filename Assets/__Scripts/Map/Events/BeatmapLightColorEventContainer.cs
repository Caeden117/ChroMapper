using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapLightColorEventContainer : BeatmapObjectContainer
{
    public BeatmapLightColorEvent LightEventData;
    public LightColorEventsContainer LightEventsContainer;

    public override BeatmapObject ObjectData { get => LightEventData; set => LightEventData = (BeatmapLightColorEvent)value; }

    public override void UpdateGridPosition()
    {
        var position = LightEventData.GetPosition();
        transform.localPosition = new Vector3(
            position.x,
            position.y,
            LightEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightColorEventContainer SpawnLightColorEvent(LightColorEventsContainer lightEventsContainer, BeatmapLightColorEvent data, 
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightColorEventContainer>();
        container.LightEventData = data;
        container.LightEventsContainer = lightEventsContainer;
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
