using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class CustomEventPlacement : PlacementController<BeatmapCustomEvent, BeatmapCustomEventContainer, CustomEventsContainer>
{
    private List<TextAsset> CustomEventDataPresets = new List<TextAsset>();

    public override BeatmapAction GenerateAction(BeatmapCustomEventContainer spawned, BeatmapObjectContainer conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, objectContainerCollection, conflicting);
    }

    public override BeatmapCustomEvent GenerateOriginalData()
    {
        return new BeatmapCustomEvent(0, "", null);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        int customEventTypeId = Mathf.RoundToInt(instantiatedContainer.transform.position.x - transform.position.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData._type = objectContainerCollection.CustomEventTypes[customEventTypeId];
    }

    internal override void Start()
    {
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>("Custom Event Presets"))
            CustomEventDataPresets.Add(asset);
        Debug.Log($"Loaded {CustomEventDataPresets.Count} presets for custom events.");
        base.Start();
    }

    internal override void ApplyToMap()
    {
        TextAsset preset = CustomEventDataPresets.FirstOrDefault(x => x.name.Contains(queuedData._type));
        if (preset != null)
        {
            try
            {
                JSONNode node = JSON.Parse(preset.text);
                queuedData._data = node;
            }catch(System.Exception e)
            {
                Debug.LogError($"Error while trying to parse Data Preset {queuedData._type}:\n{e}");
            }
        }
        base.ApplyToMap();
    }
}
