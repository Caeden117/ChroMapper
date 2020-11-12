using SimpleJSON;
using System.Collections.Generic;
using System.Linq;
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
                    newFrontScale.x = (gridSize / 10f) + 0.01f;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Event Interface Scaling Offset":
                    Vector3 newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = (gridSize / 10f) + 0.01f;
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

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.transform.localPosition = new Vector3(instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
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
                if (queuedData._customData == null || queuedData._customData?.Children.Count() == 0)
                {
                    queuedData._customData = new JSONObject();
                }
                queuedData._customData?.Add("_propID", propID);
            }
            else queuedData._customData?.Remove("_propID");
        }

        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        queuedData._value = value;
        if (queuedData.IsLaserSpeedEvent)
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
                PersistentUI.Instance.ShowDialogBox("Mapper", "360warning", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }
        queuedData._time = RoundedTime;


        if (CanPlaceChromaEvents && !queuedData.IsUtilityEvent && dropdown.Visible && queuedData._value != MapEvent.LIGHT_VALUE_OFF)
        {
            if (queuedData._customData == null) queuedData._customData = new JSONObject();
            queuedData._customData["_color"] = colorPicker.CurrentColor;
        }
        else
        {
            queuedData._customData?.Remove("_color");
        }

        if (!PlacePrecisionRotation)
        {
            UpdateQueuedValue(queuedValue);
        }
        else if (queuedData.IsRotationEvent) queuedData._value = 1360 + PrecisionRotationValue;

        if (queuedData._customData?.Count <= 0)
        {
            queuedData._customData = null;
        }

        base.ApplyToMap();

        if (queuedData.IsRotationEvent)
        {
            tracksManager.RefreshTracks();
        }
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged._time = queued._time;
        dragged._type = queued._type;
        //dragged._value = queued._value; //I dont think we need this, could cause confusion.
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
