using System;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class
    CustomEventPlacement : PlacementController<BeatmapCustomEvent, BeatmapCustomEventContainer, CustomEventsContainer>
{
    private readonly List<TextAsset> customEventDataPresets = new List<TextAsset>();

    public override int PlacementXMax => ObjectContainerCollection.CustomEventTypes.Count;

    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    internal override void Start()
    {
        gameObject.SetActive(Settings.Instance.AdvancedShit);
        foreach (var asset in Resources.LoadAll<TextAsset>("Custom Event Presets"))
            customEventDataPresets.Add(asset);
        Debug.Log($"Loaded {customEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a Custom Event.");

    public override BeatmapCustomEvent GenerateOriginalData() => new BeatmapCustomEvent(0, "", null);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        var localPosition = InstantiatedContainer.transform.localPosition;
        localPosition += Vector3.left * 0.5f;
        InstantiatedContainer.transform.localPosition = new Vector3(localPosition.x, 0.5f, localPosition.z);
        var customEventTypeId = Mathf.CeilToInt(InstantiatedContainer.transform.localPosition.x);
        if (customEventTypeId < ObjectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            QueuedData.Type = ObjectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    internal override void ApplyToMap()
    {
        var preset = customEventDataPresets.Find(x => x.name.Contains(QueuedData.Type));
        if (preset != null)
        {
            try
            {
                var node = JSON.Parse(preset.text);
                QueuedData.CustomData = node;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error while trying to parse Data Preset {QueuedData.Type}:\n{e}");
            }
        }

        base.ApplyToMap();
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapCustomEvent dragged, BeatmapCustomEvent queued) { }
}
