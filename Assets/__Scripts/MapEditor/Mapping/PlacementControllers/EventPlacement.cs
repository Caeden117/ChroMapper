using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.Base;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EventPlacement : PlacementController<IEvent, EventContainer, EventGridContainer>,
    CMInput.IEventPlacementActions
{
    [SerializeField] private EventAppearanceSO eventAppearanceSo;
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
    private bool halfFloatValue;
    private bool zeroFloatValue;

    internal int queuedValue = (int)LightValue.RedOn;
    internal float queuedFloatValue = 1.0f;
    public static bool CanPlaceChromaEvents => Settings.Instance.PlaceChromaColor;

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsLaneRotationEvent() && context.performed) UpdateQueuedRotation(negativeRotations ? 3 : 4);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsLaneRotationEvent() && context.performed) UpdateQueuedRotation(negativeRotations ? 2 : 5);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsLaneRotationEvent() && context.performed) UpdateQueuedRotation(negativeRotations ? 1 : 6);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsLaneRotationEvent() && context.performed) UpdateQueuedRotation(negativeRotations ? 0 : 7);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context) =>
        negativeRotations = context.performed;

    public void OnHalfFloatValueModifier(InputAction.CallbackContext context)
    {
        halfFloatValue = context.performed;
        UpdateFloatValue(halfFloatValue ? 0.5f : 1.0f);
    }

    public void OnZeroFloatValueModifier(InputAction.CallbackContext context)
    {
        zeroFloatValue = context.performed;
        UpdateFloatValue(zeroFloatValue ? 0 : 1);
    }

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

    public override BeatmapAction GenerateAction(IObject spawned, IEnumerable<IObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");

    public override IEvent GenerateOriginalData()
    {
        //chromaToggle.isOn = Settings.Instance.PlaceChromaEvents;
        if (Settings.Instance.Load_MapV3)
        {
            return new V3BasicEvent(0, 0, (int)LightValue.RedOn);
        }
        else
        {
            return new V2Event(0, 0, (int)LightValue.RedOn);
        }
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        if (objectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Off)
        {
            queuedData.Type =
                labels.LaneIdToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
            queuedData.CustomData?.Remove("_lightID");
        }
        else
        {
            var propID = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x - 1);
            queuedData.Type = objectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = objectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Prop
                    ? labels.PropIdToLightIdsJ(objectContainerCollection.EventTypeToPropagate, propID)
                    : (JSONNode)labels.EditorToLightID(objectContainerCollection.EventTypeToPropagate, propID);
                queuedData.GetOrCreateCustom().Add("_lightID", lightIdToApply);
            }
            else
            {
                queuedData.GetOrCreateCustom().Remove("_lightID");
            }
        }

        if (CanPlaceChromaEvents && queuedData.IsLightEvent(EnvironmentInfoHelper.GetName()) && queuedData.Value != (int)LightValue.Off)
        {
            queuedData.CustomColor = colorPicker.CurrentColor;
        }

        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        var envName = EnvironmentInfoHelper.GetName();
        queuedData.Value = value;

        if ((queuedData.IsLaserRotationEvent(envName) || queuedData.IsUtilityEvent(envName))
            && int.TryParse(laserSpeedInputField.text, out var laserSpeed))
        {
            queuedData.Value = laserSpeed;
        }

        if (queuedData.IsColorBoostEvent())
            queuedData.Value = queuedData.Value > 0 ? 1 : 0;
    }

    public void UpdateValue(int value)
    {
        queuedValue = value;
        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedFloatValue(float value)
    {
        queuedData.FloatValue = value;
    }

    public void UpdateFloatValue(float value)
    {
        queuedFloatValue = value;
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateAppearance();
    }

    private void UpdateQueuedRotation(int value)
    {
        if (queuedData.IsLaneRotationEvent())
        {
            if (queuedData.CustomData == null) queuedData.CustomData = new JSONObject();
            queuedData.CustomData["_queuedRotation"] = value;
        }

        UpdateValue(value);
    }

    public void SwapColors(bool red)
    {
        var envName = EnvironmentInfoHelper.GetName();
        if (!queuedData.IsLightEvent(envName)) return;
        if (queuedValue >= ColourManager.RgbintOffset || queuedValue == (int)LightValue.Off) return;
        if ((red && queuedValue >= (int)LightValue.RedOn) ||
            (!red && queuedValue >= (int)LightValue.BlueOn && queuedValue < (int)LightValue.RedOn))
        {
            return;
        }

        if (queuedValue >= (int)LightValue.RedOn) queuedValue -= 4;
        else if (queuedValue >= (int)LightValue.BlueOn) queuedValue += 4;
    }

    private void UpdateAppearance()
    {
        if (instantiatedContainer is null) RefreshVisuals();
        instantiatedContainer.EventData = queuedData;
        eventAppearanceSo.SetEventAppearance(instantiatedContainer, false);
    }

    public void PlaceChroma(bool v) => Settings.Instance.PlaceChromaColor = v;

    internal override void ApplyToMap()
    {
        var evt = queuedData;

        if (evt.Type == (int)EventTypeValue.EarlyLaneRotation || evt.Type == (int)EventTypeValue.LateLaneRotation)
        {
            if (!GridRotation.IsActive)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }

        evt.Time = RoundedTime;

        if (evt.CustomData != null && evt.CustomData.HasKey("_queuedRotation"))
        {
            if (evt.IsLaneRotationEvent()) queuedValue = queuedData.CustomData["_queuedRotation"];

            evt.CustomData.Remove("_queuedRotation");
        }

        if (!PlacePrecisionRotation)
            UpdateQueuedValue(queuedValue);
        else if (evt.IsLaneRotationEvent()) evt.Value = 1360 + PrecisionRotationValue;

        if (evt.CustomData?.Count <= 0) evt.CustomData = null;
        
        evt.FloatValue = 1;

        base.ApplyToMap();

        if (evt.IsLaneRotationEvent()) TracksManager.RefreshTracks();
    }

    public override void TransferQueuedToDraggedObject(ref IEvent dragged, IEvent queued)
    {
        dragged.Time = queued.Time;
        dragged.Type = queued.Type;
        // Instead of copying the whole custom data, only copy prop ID
        if (queued.CustomPropID != null) dragged.CustomPropID = queued.CustomPropID;
        if (queued.CustomLightID != null) dragged.CustomLightID = queued.CustomLightID;
    }

    internal void PlaceRotationNow(bool right, bool early)
    {
        if (!GridRotation.IsActive)
            return;

        var rotationType = early ? (int)EventTypeValue.EarlyLaneRotation : (int)EventTypeValue.LateLaneRotation;
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var IEvent = objectContainerCollection.AllRotationEvents.Find(x =>
            x.Time - epsilon < Atsc.CurrentBeat && x.Time + epsilon > Atsc.CurrentBeat && x.Type == rotationType);

        //todo add support for custom rotation angles

        var startingValue = right ? 4 : 3;
        if (IEvent != null) startingValue = IEvent.Value;

        if (IEvent != null &&
            ((startingValue == 4 && !right) ||
             (startingValue == 3 && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = IEvent.Value;
            objectContainerCollection.DeleteObject(IEvent, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(IEvent, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (IEvent != null) startingValue += right ? 1 : -1;
            var objectData = new V2Event(Atsc.CurrentBeat, rotationType, startingValue);

            objectContainerCollection.SpawnObject(objectData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }

        queuedData = BeatmapFactory.Clone(queuedData);
        TracksManager.RefreshTracks();
    }

    public override void ClickAndDragFinished() => TracksManager.RefreshTracks();
}
