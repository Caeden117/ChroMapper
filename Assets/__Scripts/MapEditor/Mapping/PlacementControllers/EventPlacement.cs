using System.Collections.Generic;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EventPlacement : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>,
    CMInput.IEventPlacementActions
{
    [FormerlySerializedAs("eventAppearanceSO")] [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private TMP_InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private CreateEventTypeLabels labels;

    public bool PlacePrecisionRotation;
    public int PrecisionRotationValue;

    private bool earlyRotationPlaceNow;
    private bool negativeRotations;

    internal int QueuedValue = MapEvent.LightValueRedON;
    public static bool CanPlaceChromaEvents => Settings.Instance.PlaceChromaColor;

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 3 : 4);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 2 : 5);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 1 : 6);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (QueuedData.IsRotationEvent && context.performed) UpdateQueuedRotation(negativeRotations ? 0 : 7);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context) =>
        negativeRotations = context.performed;

    public void OnRotateInPlaceLeft(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(false, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceRight(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(true, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceModifier(InputAction.CallbackContext context) =>
        earlyRotationPlaceNow = context.performed;

    public void SetGridSize(int gridSize = 16)
    {
        foreach (Transform eachChild in transform)
        {
            switch (eachChild.name)
            {
                case "Event Grid Front Scaling Offset":
                    var newFrontScale = eachChild.transform.localScale;
                    newFrontScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Event Interface Scaling Offset":
                    var newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newInterfaceScale;
                    break;
            }
        }

        GridChild.Size = gridSize;
    }

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");

    public override MapEvent GenerateOriginalData() =>
        //chromaToggle.isOn = Settings.Instance.PlaceChromaEvents;
        new MapEvent(0, 0, MapEvent.LightValueRedON);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        InstantiatedContainer.transform.localPosition = new Vector3(InstantiatedContainer.transform.localPosition.x,
            0.5f,
            InstantiatedContainer.transform.localPosition.z);
        if (ObjectContainerCollection.PropagationEditing == EventsContainer.PropMode.Off)
        {
            QueuedData.Type =
                labels.LaneIdToEventType(Mathf.FloorToInt(InstantiatedContainer.transform.localPosition.x));
            QueuedData.CustomData?.Remove("_lightID");
        }
        else
        {
            var propID = Mathf.FloorToInt(InstantiatedContainer.transform.localPosition.x - 1);
            QueuedData.Type = ObjectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = ObjectContainerCollection.PropagationEditing == EventsContainer.PropMode.Prop
                    ? labels.PropIdToLightIdsJ(ObjectContainerCollection.EventTypeToPropagate, propID)
                    : (JSONNode)labels.EditorToLightID(ObjectContainerCollection.EventTypeToPropagate, propID);
                QueuedData.GetOrCreateCustomData().Add("_lightID", lightIdToApply);
            }
            else
            {
                QueuedData.GetOrCreateCustomData().Remove("_lightID");
            }
        }

        if (CanPlaceChromaEvents && !QueuedData.IsUtilityEvent && QueuedData.Value != MapEvent.LightValueOff)
            QueuedData.GetOrCreateCustomData()["_color"] = colorPicker.CurrentColor;
        else
            QueuedData.CustomData?.Remove("_color");

        UpdateQueuedValue(QueuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        var propID = InstantiatedContainer != null
            ? Mathf.FloorToInt(InstantiatedContainer.transform.localPosition.x - 1)
            : -1;

        if (ObjectContainerCollection.PropagationEditing != EventsContainer.PropMode.Off && propID >= 0 &&
            !QueuedData.IsUtilityEvent)
        {
            if (value != MapEvent.LightValueBlueON && value != MapEvent.LightValueRedON &&
                value != MapEvent.LightValueOff)
            {
                value = value < 5
                    ? MapEvent.LightValueBlueON
                    : MapEvent.LightValueRedON;
            }
        }

        QueuedData.Value = value;

        if ((QueuedData.IsLaserSpeedEvent || QueuedData.IsInterscopeEvent)
            && int.TryParse(laserSpeedInputField.text, out var laserSpeed))
        {
            QueuedData.Value = laserSpeed;
        }

        if (QueuedData.Type == MapEvent.EventTypeBoostLights)
            QueuedData.Value = QueuedData.Value > 0 ? 1 : 0;
    }

    public void UpdateValue(int value)
    {
        QueuedValue = value;
        UpdateQueuedValue(QueuedValue);
        UpdateAppearance();
    }

    private void UpdateQueuedRotation(int value)
    {
        if (QueuedData.IsRotationEvent)
        {
            if (QueuedData.CustomData == null) QueuedData.CustomData = new JSONObject();
            QueuedData.CustomData["_queuedRotation"] = value;
        }

        UpdateValue(value);
    }

    public void SwapColors(bool red)
    {
        if (QueuedData.IsUtilityEvent) return;
        if (QueuedValue >= ColourManager.RgbintOffset || QueuedValue == MapEvent.LightValueOff) return;
        if ((red && QueuedValue >= MapEvent.LightValueRedON) ||
            (!red && QueuedValue >= MapEvent.LightValueBlueON && QueuedValue < MapEvent.LightValueRedON))
        {
            return;
        }

        if (QueuedValue >= MapEvent.LightValueRedON) QueuedValue -= 4;
        else if (QueuedValue >= MapEvent.LightValueBlueON) QueuedValue += 4;
    }

    private void UpdateAppearance()
    {
        if (InstantiatedContainer is null) RefreshVisuals();
        InstantiatedContainer.EventData = QueuedData;
        eventAppearanceSo.SetEventAppearance(InstantiatedContainer, false);
    }

    public void PlaceChroma(bool v) => Settings.Instance.PlaceChromaColor = v;

    internal override void ApplyToMap()
    {
        var mapEvent = QueuedData;

        if (mapEvent.Type == MapEvent.EventTypeEarlyRotation || mapEvent.Type == MapEvent.EventTypeLateRotation)
        {
            if (!GridRotation.IsActive)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }

        mapEvent.Time = RoundedTime;

        if (mapEvent.CustomData != null && mapEvent.CustomData.HasKey("_queuedRotation"))
        {
            if (mapEvent.IsRotationEvent) QueuedValue = QueuedData.CustomData["_queuedRotation"];

            mapEvent.CustomData.Remove("_queuedRotation");
        }

        if (!PlacePrecisionRotation)
            UpdateQueuedValue(QueuedValue);
        else if (mapEvent.IsRotationEvent) mapEvent.Value = 1360 + PrecisionRotationValue;

        if (mapEvent.CustomData?.Count <= 0) mapEvent.CustomData = null;

        base.ApplyToMap();

        if (mapEvent.IsRotationEvent) TracksManager.RefreshTracks();
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged.Time = queued.Time;
        dragged.Type = queued.Type;
        // Instead of copying the whole custom data, only copy prop ID
        if (dragged.CustomData != null && queued.CustomData != null)
        {
            if (queued.CustomData.HasKey("_propID")) dragged.CustomData["_propID"] = queued.CustomData["_propID"];

            if (queued.CustomData.HasKey("_lightID")) dragged.CustomData["_lightID"] = queued.CustomData["_lightID"];
        }
    }

    internal void PlaceRotationNow(bool right, bool early)
    {
        if (!GridRotation.IsActive)
            return;

        var rotationType = early ? MapEvent.EventTypeEarlyRotation : MapEvent.EventTypeLateRotation;
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var mapEvent = ObjectContainerCollection.AllRotationEvents.Find(x =>
            x.Time - epsilon < Atsc.CurrentBeat && x.Time + epsilon > Atsc.CurrentBeat && x.Type == rotationType);

        //todo add support for custom rotation angles

        var startingValue = right ? 4 : 3;
        if (mapEvent != null) startingValue = mapEvent.Value;

        if (mapEvent != null &&
            ((startingValue == 4 && !right) ||
             (startingValue == 3 && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = mapEvent.Value;
            ObjectContainerCollection.DeleteObject(mapEvent, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(mapEvent, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (mapEvent != null) startingValue += right ? 1 : -1;
            var objectData = new MapEvent(Atsc.CurrentBeat, rotationType, startingValue);

            ObjectContainerCollection.SpawnObject(objectData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }

        QueuedData = BeatmapObject.GenerateCopy(QueuedData);
        TracksManager.RefreshTracks();
    }

    public override void ClickAndDragFinished() => TracksManager.RefreshTracks();
}
