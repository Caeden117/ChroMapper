using SimpleJSON;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EventPlacement : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>, CMInput.IEventPlacementActions
{
    public static readonly string ChromaColorKey = "PlaceChromaColor";
    public static bool CanPlaceChromaEvents {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
            {
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            }
            return false;
        }
    }

    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private EventPlacementUI eventPlacementUI;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private ToggleColourDropdown dropdown;
    private int queuedValue = MapEvent.LIGHT_VALUE_RED_ON;
    private bool negativeRotations = false;

    public override bool IsValid => base.IsValid || (KeybindsController.ShiftHeld && queuedData.IsRotationEvent);

    public bool PlacePrecisionRotation = false;
    public int PrecisionRotationValue = 0;


    public void SetGridSize(int gridSize = 16)
    {
        foreach (Transform eachChild in transform)
        {
            switch (eachChild.name)
            {
                case "Event Grid Front Scaling Offset":
                    Vector3 newFrontScale = eachChild.transform.localScale;
                    newFrontScale.x = 1.61f / 16 * gridSize;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Event Interface Scaling Offset":
                    Vector3 newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = 1.61f / 16 * gridSize;
                    eachChild.transform.localScale = newInterfaceScale;
                    break;
                default:
                    break;
            }
        }
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

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        //this mess of localposition and position assignments are to align the shits up with the grid
        //and to hopefully not cause IndexOutOfRangeExceptions
        instantiatedContainer.transform.localPosition = parentTrack.InverseTransformPoint(hit.point); //fuck transformedpoint we're doing it ourselves
        instantiatedContainer.transform.localPosition = new Vector3( //Time to round
            Mathf.Ceil(instantiatedContainer.transform.localPosition.x) - 0.5f, 0.5f, RoundedTime * EditorScaleController.EditorScale);
        float x = instantiatedContainer.transform.localPosition.x; //Clamp values to prevent exceptions
        instantiatedContainer.transform.localPosition = new Vector3(Mathf.Clamp(x, 0.5f, Mathf.Floor(hit.transform.lossyScale.x * 10) - 0.5f),
            instantiatedContainer.transform.localPosition.y, instantiatedContainer.transform.localPosition.z);

        //now on to the good shit.
        if (!objectContainerCollection.PropagationEditing)
        {
            queuedData._type = BeatmapEventContainer.ModifiedTypeToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x) );
            queuedData._customData?.Remove("_propID");
        }
        else
        {
            queuedData._type = objectContainerCollection.EventTypeToPropagate;
            int propID = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x - 1);
            if (propID >= 0)
            {
                if (queuedData._customData is null) queuedData._customData = new JSONObject();
                queuedData._customData.Remove("_propID");
                if (queuedData._customData is null) queuedData._customData = new JSONObject();
                queuedData._customData.Add("_propID", propID);
            }
            else queuedData._customData?.Remove("_propID");
        }
        if (!PlacePrecisionRotation)
        {
            queuedData._value = queuedValue;
        }
        else queuedData._value = 1360 + PrecisionRotationValue;
        if (queuedData._type == MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED || queuedData._type == MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED)
            if (int.TryParse(laserSpeedInputField.text, out int laserSpeed)) queuedData._value = laserSpeed;
        UpdateAppearance();
    }

    public void UpdateValue(int value)
    {
        queuedValue = value;
        queuedData._value = value;
        UpdateAppearance();
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
        if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
        {
            Settings.NonPersistentSettings[ChromaColorKey] = v;
        }
        else
        {
            Settings.NonPersistentSettings.Add(ChromaColorKey, v);
        }
    }

    internal override void ApplyToMap()
    {
        if (queuedData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || queuedData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
        {
            if (!gridRotation?.IsActive ?? false)
            {
                PersistentUI.Instance.ShowDialogBox("Rotation events are disabled outside of the 360 Degree, 90 Degree, and Lawless characteristics.\n\n" +
                    "If you wish to place these events, please create difficulties for the aformentioned characteristics.", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }
        if (KeybindsController.ShiftHeld) return;
        queuedData._time = RoundedTime;


        if (CanPlaceChromaEvents && !queuedData.IsUtilityEvent && dropdown.Visible)
        {
            if (queuedData._customData == null) queuedData._customData = new JSONObject();
            queuedData._customData["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            queuedData._customData?.Remove("_color");
        }

        objectContainerCollection.SpawnObject(queuedData, out IEnumerable<BeatmapObject> conflicting);
        BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(queuedData, conflicting, "Placed an Event."));
        queuedData = BeatmapObject.GenerateCopy(queuedData);
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged._time = queued._time;
        dragged._type = queued._type;
        dragged._value = queued._value;
        dragged._customData = queued._customData;
    }

    public override void ClickAndDragFinished()
    {
        tracksManager.RefreshTracks();
    }

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateValue(negativeRotations ? 3 : 4);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateValue(negativeRotations ? 2 : 5);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateValue(negativeRotations ? 1 : 6);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (queuedData.IsRotationEvent && context.performed) UpdateValue(negativeRotations ? 0 : 7);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context)
    {
        negativeRotations = context.performed;
    }
}
