using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

public class CustomEventPlacement : PlacementController<BeatmapCustomEvent, BeatmapCustomEventContainer, CustomEventsContainer>
{
    private List<TextAsset> CustomEventDataPresets = new List<TextAsset>();

    protected override bool CanClickAndDrag { get; set; } = false;

    public override BeatmapAction GenerateAction(BeatmapCustomEventContainer spawned, BeatmapObjectContainer conflicting)
    {
        return new BeatmapObjectPlacementAction(conflicting, spawned, "Placed a Custom Event.");
    }

    public override BeatmapCustomEvent GenerateOriginalData()
    {
        return new BeatmapCustomEvent(0, "", null);
    }

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 _)
    {
        //this mess of localposition and position assignments are to align the shits up with the grid
        //and to hopefully not cause IndexOutOfRangeExceptions
        instantiatedContainer.transform.localPosition = parentTrack.InverseTransformPoint(hit.point); //fuck transformedpoint we're doing it ourselves
        instantiatedContainer.transform.localPosition = new Vector3( //Time to round
            Mathf.Ceil(instantiatedContainer.transform.localPosition.x) - 0.5f, 0.5f, RoundedTime * EditorScaleController.EditorScale);
        float x = instantiatedContainer.transform.localPosition.x; //Clamp values to prevent exceptions
        instantiatedContainer.transform.localPosition = new Vector3(Mathf.Clamp(x, -0.5f, Mathf.Floor(hit.transform.lossyScale.x * 10) - 1.5f),
            instantiatedContainer.transform.localPosition.y, instantiatedContainer.transform.localPosition.z);
        int customEventTypeId = Mathf.CeilToInt(instantiatedContainer.transform.localPosition.x);
        if (customEventTypeId < objectContainerCollection.CustomEventTypes.Count && customEventTypeId >= 0)
            queuedData._type = objectContainerCollection.CustomEventTypes[customEventTypeId];
        instantiatedContainer.transform.localPosition += new Vector3(0.5f, 0, 0);
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
        TextAsset preset = CustomEventDataPresets.FirstOrDefault(x => x.name.Contains(queuedData._type));
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
