using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;
using UnityEngine.UI;

public class EventPlacement : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>, CMInput.IEventPlacementActions
{
    public static bool CanPlaceChromaEvents => Settings.Instance.PlaceChromaColor;

    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private TMP_InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private CreateEventTypeLabels labels;

    internal int queuedValue = MapEvent.LIGHT_VALUE_RED_ON;
    private bool negativeRotations = false;

    public bool PlacePrecisionRotation = false;
    public int PrecisionRotationValue = 0;

    private bool earlyRotationPlaceNow = false;

    public void SetGridSize(int gridSize = 16)
    {
        foreach (Transform eachChild in transform)
        {
            switch (eachChild.name)
            {
                case "Event Grid Front Scaling Offset":
                    Vector3 newFrontScale = eachChild.transform.localScale;
                    newFrontScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Event Interface Scaling Offset":
                    Vector3 newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newInterfaceScale;
                    break;
                default:
                    break;
            }
        }
        gridChild.Size = gridSize;
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");
    }

    public override MapEvent GenerateOriginalData()
    {
        //chromaToggle.isOn = Settings.Instance.PlaceChromaEvents;
        return new MapEvent(0, 0, MapEvent.LIGHT_VALUE_RED_ON);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        if (objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Off)
        {
            queuedData._type = labels.LaneIdToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
            queuedData._customData?.Remove("_lightID");
        }
        else 
        {
            var propID = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x - 1);
            queuedData._type = objectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Prop ?
                    labels.PropIdToLightIdsJ(objectContainerCollection.EventTypeToPropagate, propID) :
                    (JSONNode) labels.EditorToLightID(objectContainerCollection.EventTypeToPropagate, propID);
                queuedData.GetOrCreateCustomData().Add("_lightID", lightIdToApply);
            }
            else queuedData.GetOrCreateCustomData().Remove("_lightID");
        }

        if (CanPlaceChromaEvents && !queuedData.IsUtilityEvent && queuedData._value != MapEvent.LIGHT_VALUE_OFF)
        {
            queuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            queuedData._customData?.Remove("_color");
        }

        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        var propID = Mathf.FloorToInt((instantiatedContainer?.transform.localPosition.x ?? 0) - 1);
        if (objectContainerCollection.PropagationEditing != EventsContainer.PropMode.Off && propID >= 0 && !queuedData.IsUtilityEvent)
        {
            if (value != MapEvent.LIGHT_VALUE_BLUE_ON && value != MapEvent.LIGHT_VALUE_RED_ON && value != MapEvent.LIGHT_VALUE_OFF)
            {
                value = value < 5
                    ? MapEvent.LIGHT_VALUE_BLUE_ON
                    : MapEvent.LIGHT_VALUE_RED_ON;
            }
        }

        queuedData._value = value;

        if (queuedData.IsLaserSpeedEvent || queuedData.IsInterscopeEvent)
            if (int.TryParse(laserSpeedInputField.text, out int laserSpeed)) queuedData._value = laserSpeed;

        if (queuedData._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS)
            queuedData._value = queuedData._value > 0 ? 1 : 0;
    }

    public void UpdateValue(int value)
    {
        queuedValue = value;
        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    private void UpdateQueuedRotation(int value)
    {
        if (queuedData.IsRotationEvent)
        {
            if (queuedData._customData == null) queuedData._customData = new JSONObject();
            queuedData._customData["_queuedRotation"] = value;
        }
        UpdateValue(value);
    }

    public void SwapColors(bool red)
    {
        if (queuedData.IsUtilityEvent) return;
        if (queuedValue >= ColourManager.RGB_INT_OFFSET || queuedValue == MapEvent.LIGHT_VALUE_OFF) return;
        if (red && queuedValue >= MapEvent.LIGHT_VALUE_RED_ON ||
            !red && queuedValue >= MapEvent.LIGHT_VALUE_BLUE_ON && queuedValue < MapEvent.LIGHT_VALUE_RED_ON) return;
        if (queuedValue >= MapEvent.LIGHT_VALUE_RED_ON) queuedValue -= 4;
        else if (queuedValue >= MapEvent.LIGHT_VALUE_BLUE_ON) queuedValue += 4;
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) RefreshVisuals();
        instantiatedContainer.eventData = queuedData;
        eventAppearanceSO.SetEventAppearance(instantiatedContainer, false);
    }

    public void PlaceChroma(bool v)
    {
        Settings.Instance.PlaceChromaColor = v;
    }

    internal override void ApplyToMap()
    {
        var mapEvent = queuedData;

        if (mapEvent._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || mapEvent._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        {
            if (!gridRotation?.IsActive ?? false)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }
        mapEvent._time = RoundedTime;

        if (mapEvent._customData != null && mapEvent._customData.HasKey("_queuedRotation"))
        {
            if (mapEvent.IsRotationEvent)
            {
                queuedValue = queuedData._customData["_queuedRotation"];
            }

            mapEvent._customData.Remove("_queuedRotation");
        }

        if (!PlacePrecisionRotation)
        {
            UpdateQueuedValue(queuedValue);
        }
        else if (mapEvent.IsRotationEvent) mapEvent._value = 1360 + PrecisionRotationValue;

        if (mapEvent._customData?.Count <= 0)
        {
            mapEvent._customData = null;
        }

        base.ApplyToMap();

        if (mapEvent.IsRotationEvent)
        {
            tracksManager.RefreshTracks();
        }
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged._time = queued._time;
        dragged._type = queued._type;
        // Instead of copying the whole custom data, only copy prop ID
        if (dragged._customData != null && queued._customData != null)
        {
            if (queued._customData.HasKey("_propID"))
            {
                dragged._customData["_propID"] = queued._customData["_propID"];
            }

            if (queued._customData.HasKey("_lightID"))
            {
                dragged._customData["_lightID"] = queued._customData["_lightID"];
            }
        }
    }

    internal void PlaceRotationNow(bool right, bool early)
    {
        if (!gridRotation?.IsActive ?? false)
            return;
        int rotationType = early ? MapEvent.EVENT_TYPE_EARLY_ROTATION : MapEvent.EVENT_TYPE_LATE_ROTATION;
        float epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        MapEvent mapEvent = objectContainerCollection.AllRotationEvents.Find(x => x._time - epsilon < atsc.CurrentBeat && x._time + epsilon > atsc.CurrentBeat && x._type == rotationType);

        //todo add support for custom rotation angles

        int startingValue = right ? 4 : 3;
        if (mapEvent != null) startingValue = mapEvent._value;

        if (mapEvent != null && ((startingValue == 4 && !right || startingValue == 3 && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = mapEvent._value;
            objectContainerCollection.DeleteObject(mapEvent, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(mapEvent, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (mapEvent != null) startingValue += right ? 1 : -1;
            MapEvent objectData = new MapEvent(atsc.CurrentBeat, rotationType, startingValue);

            objectContainerCollection.SpawnObject(objectData, out List<BeatmapObject> conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }
        queuedData = BeatmapObject.GenerateCopy(queuedData);
        tracksManager.RefreshTracks();
    }

    public override void ClickAndDragFinished()
    {
        tracksManager.RefreshTracks();
    }

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 3 : 4);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 2 : 5);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 1 : 6);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 0 : 7);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context)
    {
        negativeRotations = context.performed;
    }

    public void OnRotateInPlaceLeft(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(false, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceRight(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(true, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceModifier(InputAction.CallbackContext context)
    {
        earlyRotationPlaceNow = context.performed;
    }
}
