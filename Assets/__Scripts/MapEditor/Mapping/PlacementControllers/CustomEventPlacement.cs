using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class CustomEventPlacement : PlacementController<BeatmapCustomEvent, BeatmapCustomEventContainer, CustomEventsContainer>
{
    private List<TextAsset> CustomEventDataPresets = new List<TextAsset>();

    public override int PlacementXMax => objectContainerCollection.CustomEventTypes.Count;

    [HideInInspector] protected override bool CanClickAndDrag { get; set; } = false;

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a Custom Event.");
    }

    public override BeatmapCustomEvent GenerateOriginalData()
    {
        return new BeatmapCustomEvent(0, "", null);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        Vector3 localPosition = instantiatedContainer.transform.localPosition;
        localPosition += Vector3.left * 0.5f;
        instantiatedContainer.transform.localPosition = new Vector3(localPosition.x, 0.5f, localPosition.z);
        int customEventTypeId = Mathf.CeilToInt(instantiatedContainer.transform.localPosition.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData._type = objectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    internal override void Start()
    {
        gameObject.SetActive(Settings.Instance.AdvancedShit);
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Custom Event Presets"))
            CustomEventDataPresets.Add(asset);
        Debug.Log($"Loaded {CustomEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    internal override void ApplyToMap()
    {
        TextAsset preset = CustomEventDataPresets.Find(x => x.name.Contains(queuedData._type));
        if (preset != null)
        {
            try
            {
                JSONNode node = JSON.Parse(preset.text);
                queuedData._customData = node;
            } catch (System.Exception e)
            {
                Debug.LogError($"Error while trying to parse Data Preset {queuedData._type}:\n{e}");
            }
        }
        base.ApplyToMap();
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapCustomEvent dragged, BeatmapCustomEvent queued) { }
}
