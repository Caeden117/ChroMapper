using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using Beatmap.V2;
using Beatmap.V3;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EventPlacement : PlacementController<BaseEvent, EventContainer, EventGridContainer>,
    CMInput.IEventPlacementActions
{
    [FormerlySerializedAs("eventAppearanceSO")][SerializeField] private EventAppearanceSO eventAppearanceSo;
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

    protected override Vector2 vanillaOffset { get; } = new Vector2(-0.5f, -1.1f);

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

    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");

    public override BaseEvent GenerateOriginalData() => BeatmapFactory.Event(0, 0, (int)LightValue.RedOn);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x + 0.5f,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        if (objectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Off)
        {
            queuedData.Type =
                labels.LaneIdToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
            queuedData.CustomLightID = null;
        }
        else
        {
            var propID = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x - 1);
            queuedData.Type = objectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = objectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Prop
                    ? labels.PropIdToLightIds(objectContainerCollection.EventTypeToPropagate, propID)
                    : new[] { labels.EditorToLightID(objectContainerCollection.EventTypeToPropagate, propID) };
                queuedData.CustomLightID = lightIdToApply;
            }
            else
            {
                queuedData.CustomLightID = null;
            }
        }

        if (CanPlaceChromaEvents && dropdown.Visible && queuedData.IsLightEvent(EnvironmentInfoHelper.GetName()) && queuedData.Value != (int)LightValue.Off)
            queuedData.CustomColor = colorPicker.CurrentColor;
        else
            queuedData.CustomColor = null;

        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        queuedData.Value = value;

        if ((queuedData.IsLaserRotationEvent() || queuedData.IsUtilityEvent())
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
        if (!queuedData.IsLightEvent()) return;
        if (queuedValue >= ColourManager.RgbintOffset || queuedValue == (int)LightValue.Off) return;
        if ((red && queuedValue >= (int)LightValue.RedOn) ||
            (!red && queuedValue >= (int)LightValue.BlueOn && queuedValue < (int)LightValue.RedOn))
        {
            return;
        }

        if (queuedValue > 0 && queuedValue <= 4) queuedValue += 4; // blue to red
        else if (queuedValue > 4 && queuedValue <= 8) queuedValue += 4; // red to white
        else if (queuedValue > 8 && queuedValue <= 12) queuedValue -= 8; // white to blue
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

        if (evt.IsLaneRotationEvent())
        {
            if (!GridRotation.IsActive)
            {
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }

        if (evt.CustomData != null && evt.CustomData.HasKey("_queuedRotation"))
        {
            if (evt.IsLaneRotationEvent()) queuedValue = queuedData.CustomData["_queuedRotation"];

            evt.CustomData.Remove("_queuedRotation");
        }

        if (!PlacePrecisionRotation)
            UpdateQueuedValue(queuedValue);
        else if (evt.IsLaneRotationEvent()) evt.Value = 1360 + PrecisionRotationValue;

        if (evt.CustomData?.Count <= 0) evt.CustomData = null;

        if (!evt.IsLightEvent()) // in case we are placing rotation/boost when pressing half/zero modifier
        {
            evt.FloatValue = 1;
        }

        // convert event to their respective event type
        // TODO: cleaner factory would be better, also this is ugly as hell
        if (Settings.Instance.Load_MapV3)
        {
            if (evt.IsLaneRotationEvent())
            {
                queuedData = evt is V3RotationEvent ? evt : new V3RotationEvent(evt);
            }

            if (evt.IsColorBoostEvent())
            {
                queuedData = evt is V3ColorBoostEvent ? evt : new V3ColorBoostEvent(evt);
            }
            base.ApplyToMap();
            queuedData = new V3BasicEvent(evt); // need to convert back to regular event
            queuedData.CustomData = null;
        }
        else
        {
            base.ApplyToMap();
        }

        if (evt.IsLaneRotationEvent()) TracksManager.RefreshTracks();
    }

    public override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
        dragged.Type = queued.Type;
        // Instead of copying the whole custom data, only copy prop ID
        if (dragged.CustomData != null && queued.CustomData != null)
        {
            if (queued.CustomData?[queued.CustomKeyPropID] != null) dragged.GetOrCreateCustom()[dragged.CustomKeyPropID] = queued.CustomData[queued.CustomKeyPropID];

            if (queued.CustomLightID != null) dragged.CustomLightID = queued.CustomLightID;
        }
    }

    internal void PlaceRotationNow(bool right, bool early)
    {
        if (!GridRotation.IsActive)
            return;

        var rotationType = early ? (int)EventTypeValue.EarlyLaneRotation : (int)EventTypeValue.LateLaneRotation;
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var evt = objectContainerCollection.AllRotationEvents.Find(x =>
            x.JsonTime - epsilon < Atsc.CurrentJsonTime && x.JsonTime + epsilon > Atsc.CurrentJsonTime && x.Type == rotationType);

        //todo add support for custom rotation angles

        var startingValue = right ? 4 : 3;
        if (evt != null) startingValue = evt.Value;

        if (evt != null &&
            ((startingValue == 4 && !right) ||
             (startingValue == 3 && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = evt.Value;
            objectContainerCollection.DeleteObject(evt, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(evt, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (evt != null) startingValue += right ? 1 : -1;
            var objectData = BeatmapFactory.Event(Atsc.CurrentJsonTime, rotationType, startingValue);

            objectContainerCollection.SpawnObject(objectData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }

        queuedData = BeatmapFactory.Clone(queuedData);
        TracksManager.RefreshTracks();
    }

    public override void ClickAndDragFinished() => TracksManager.RefreshTracks();
}
