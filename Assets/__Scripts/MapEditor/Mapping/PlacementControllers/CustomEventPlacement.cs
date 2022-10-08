using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.V2.Customs;
using SimpleJSON;
using UnityEngine;

public class
    CustomEventPlacement : PlacementController<BaseCustomEvent, CustomEventContainer, CustomEventGridContainer>
{
    private readonly List<TextAsset> customEventDataPresets = new List<TextAsset>();

    public override int PlacementXMax => objectContainerCollection.CustomEventTypes.Count;

    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    internal override void Start()
    {
        gameObject.SetActive(Settings.Instance.AdvancedShit);
        foreach (var asset in Resources.LoadAll<TextAsset>("Custom Event Presets"))
            customEventDataPresets.Add(asset);
        Debug.Log($"Loaded {customEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a Custom Event.");

    // TODO: check v2 or v3
    public override BaseCustomEvent GenerateOriginalData() => new V2CustomEvent(0, "", null);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        var localPosition = instantiatedContainer.transform.localPosition;
        localPosition += Vector3.left * 0.5f;
        instantiatedContainer.transform.localPosition = new Vector3(localPosition.x, 0.5f, localPosition.z);
        var customEventTypeId = Mathf.CeilToInt(instantiatedContainer.transform.localPosition.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData.Type = objectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    internal override void ApplyToMap()
    {
        var preset = customEventDataPresets.Find(x => x.name.Contains(queuedData.Type));
        if (preset != null)
        {
            try
            {
                var node = JSON.Parse(preset.text);
                queuedData.CustomData = node;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while trying to parse Data Preset {queuedData.Type}:\n{e}");
            }
        }

        base.ApplyToMap();
    }

    public override void TransferQueuedToDraggedObject(ref BaseCustomEvent dragged, BaseCustomEvent queued) { }
}
