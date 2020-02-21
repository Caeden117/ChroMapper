using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventPlacement : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private EventPlacementUI eventPlacementUI;
    [SerializeField] private Toggle redEventToggle;
    private int queuedValue = MapEvent.LIGHT_VALUE_RED_ON;

    public bool PlaceRedNote => redEventToggle.isOn;

    public override bool IsValid => base.IsValid || (KeybindsController.ShiftHeld && queuedData.IsRotationEvent);

    public bool PlacePrecisionRotation = false;
    public int PrecisionRotationValue = 0;

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, container, "Placed an Event.");
    }

    public override MapEvent GenerateOriginalData()
    {
        chromaToggle.isOn = Settings.Instance.PlaceChromaEvents;
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
        if (!objectContainerCollection.RingPropagationEditing)
        {
            queuedData._type = BeatmapEventContainer.ModifiedTypeToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x) );
            queuedData._customData?.Remove("_propID");
        }
        else
        {
            queuedData._type = MapEvent.EVENT_TYPE_RING_LIGHTS;
            int propID = Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x - 1);
            if (propID >= 0)
            {
                if (queuedData._customData is null) queuedData._customData = new SimpleJSON.JSONObject();
                queuedData._customData["_propID"] = propID;
            }
            else queuedData._customData?.Remove("_propID");
        }
        if (!PlacePrecisionRotation)
        {
            if (Settings.Instance.PlaceOnlyChromaEvents && Settings.Instance.PlaceChromaEvents && !queuedData.IsRotationEvent)
                queuedData._value = ColourManager.ColourToInt(colorPicker.CurrentColor);
            else queuedData._value = queuedValue;
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
        Settings.Instance.PlaceChromaEvents = v;
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
        if ((KeybindsController.AltHeld || (Settings.Instance.PlaceOnlyChromaEvents && Settings.Instance.PlaceChromaEvents)) && !queuedData.IsUtilityEvent)
        {
            MapEvent justChroma = BeatmapObject.GenerateCopy(queuedData);
            justChroma._value = ColourManager.ColourToInt(colorPicker.CurrentColor);
            BeatmapEventContainer container = objectContainerCollection.SpawnObject(justChroma, out BeatmapObjectContainer conflicting2) as BeatmapEventContainer;
            if (container == null) return;
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(new List<BeatmapObjectContainer>() { conflicting2 },
                new List<BeatmapObjectContainer>() { container }, "Placed a Chroma event." ));
            SelectionController.RefreshMap();
            queuedData = BeatmapObject.GenerateCopy(queuedData);
            return;
        }
        BeatmapEventContainer spawned = objectContainerCollection.SpawnObject(BeatmapObject.GenerateCopy(queuedData), out BeatmapObjectContainer conflicting) as BeatmapEventContainer;
        if (spawned == null) return;
        BeatmapEventContainer chroma = null;
        if (Settings.Instance.PlaceChromaEvents && !queuedData.IsUtilityEvent && (queuedValue != MapEvent.LIGHT_VALUE_OFF))
        {
            MapEvent chromaData = BeatmapObject.GenerateCopy(queuedData);
            chromaData._time -= 1 / 64f;
            chromaData._value = ColourManager.ColourToInt(colorPicker.CurrentColor);
            chroma = objectContainerCollection.SpawnObject(chromaData, out _) as BeatmapEventContainer;
        }
        BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(new List<BeatmapObjectContainer>() { conflicting },
            new List<BeatmapObjectContainer>() { spawned, chroma }, "Placed an Event with an attached Chroma event."));
        SelectionController.RefreshMap();
        queuedData = BeatmapObject.GenerateCopy(queuedData);
        tracksManager.RefreshTracks();
        spawned.transform.localEulerAngles = Vector3.zero;
        if (chroma != null) chroma.transform.localEulerAngles = Vector3.zero;
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged._time = queued._time;
        dragged._type = queued._type;
        dragged._customData = queued._customData;
    }

    public override void ClickAndDragFinished()
    {
        tracksManager.RefreshTracks();
    }

    public override bool IsObjectOverlapping(MapEvent draggedData, MapEvent overlappingData)
    {
        return draggedData._type == overlappingData._type;
    }
}
