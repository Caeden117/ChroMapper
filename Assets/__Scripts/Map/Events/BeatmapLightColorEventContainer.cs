using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// It's strange to inherit from <see cref="BeatmapEventContainer"/>, but I need to use those shader properties to reuse <see cref="EventAppearanceSO"/>
/// </summary>
public class BeatmapLightColorEventContainer : BeatmapEventContainer
{
    public BeatmapLightColorEvent ColorEventData;
    public LightColorEventsContainer ColorEventsContainer;

    public override BeatmapObject ObjectData { get => ColorEventData; set => ColorEventData = (BeatmapLightColorEvent)value; }

    public override void UpdateGridPosition()
    {
        var stackList = ColorEventsContainer.GetBetween(ColorEventData.Time - 1e-3f, ColorEventData.Time + 1e-3f)
            .Cast<BeatmapLightColorEvent>()
            .Where(x => x.Group == ColorEventData.Group)
            .ToList();
        int y = stackList.IndexOf(ColorEventData);
        transform.localPosition = new Vector3(
            ColorEventsContainer.platformDescriptor.GroupIdToLaneIndex(ColorEventData.Group) + 0.5f,
            y + 0.5f,
            ColorEventData.Time * EditorScaleController.EditorScale
            );
    }

    public static BeatmapLightColorEventContainer SpawnLightColorEvent(LightColorEventsContainer lightEventsContainer, BeatmapLightColorEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapLightColorEventContainer>();
        container.ColorEventData = data;
        container.ColorEventsContainer = lightEventsContainer;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }
}
