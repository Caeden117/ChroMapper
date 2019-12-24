using System.Collections;
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

    public bool PlaceRedNote
    {
        get { return redEventToggle.isOn; }
    }

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned, BeatmapObjectContainer container)
    {
        return new BeatmapObjectPlacementAction(spawned, container);
    }

    public override MapEvent GenerateOriginalData()
    {
        chromaToggle.isOn = Settings.Instance.PlaceChromaEvents;
        return new MapEvent(0, 0, MapEvent.LIGHT_VALUE_RED_ON);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        instantiatedContainer.transform.localPosition = new Vector3(
                   Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                       Mathf.Ceil(hit.collider.bounds.min.x),
                       Mathf.Floor(hit.collider.bounds.max.x)
                   ) - 0.5f,
                   Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                       Mathf.Floor(hit.collider.bounds.max.y)) + 0.5f,
                   RoundedTime * EditorScaleController.EditorScale);
        instantiatedContainer.transform.position -= transform.position - new Vector3(0.5f, 0, 0);
        if (!objectContainerCollection.RingPropagationEditing)
        {
            queuedData._type = BeatmapEventContainer.ModifiedTypeToEventType(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x) );
            queuedData._customData = null;
        }
        else
        {
            queuedData._type = MapEvent.EVENT_TYPE_RING_LIGHTS;
            int propID = Mathf.RoundToInt(instantiatedContainer.transform.localPosition.x - 1);
            if (propID >= 0)
            {
                if (queuedData._customData is null) queuedData._customData = new SimpleJSON.JSONObject();
                queuedData._customData["_propID"] = propID;
            }
            else queuedData._customData?.Remove("_propID");
        }
        queuedData._value = Settings.Instance.PlaceOnlyChromaEvents && Settings.Instance.PlaceChromaEvents ? ColourManager.ColourToInt(colorPicker.CurrentColor) : queuedValue;
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
        eventAppearanceSO.SetEventAppearance(instantiatedContainer);
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
                PersistentUI.Instance.ShowDialogBox("Rotation events are disabled outside of 360 and 90 Degree characteristics.\n\n" +
                    "If you wish to place these events, please create difficulties with the aformentioned characteristics.", null, PersistentUI.DialogBoxPresetType.Ok);
                return;
            }
        }
        queuedData._time = RoundedTime;
        if ((KeybindsController.AltHeld || (Settings.Instance.PlaceOnlyChromaEvents && Settings.Instance.PlaceChromaEvents)) && !queuedData.IsUtilityEvent())
        {
            MapEvent justChroma = BeatmapObject.GenerateCopy(queuedData);
            justChroma._value = ColourManager.ColourToInt(colorPicker.CurrentColor);
            BeatmapEventContainer container = objectContainerCollection.SpawnObject(justChroma, out BeatmapObjectContainer conflicting2) as BeatmapEventContainer;
            BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(new List<BeatmapObjectContainer>() { conflicting2 },
                new List<BeatmapObjectContainer>() { container } ));
            SelectionController.RefreshMap();
            queuedData = BeatmapObject.GenerateCopy(queuedData);
            return;
        }
        BeatmapEventContainer spawned = objectContainerCollection.SpawnObject(BeatmapObject.GenerateCopy(queuedData), out BeatmapObjectContainer conflicting) as BeatmapEventContainer;
        BeatmapEventContainer chroma = null;
        if (Settings.Instance.PlaceChromaEvents && !queuedData.IsUtilityEvent() && (queuedValue != MapEvent.LIGHT_VALUE_OFF))
        {
            MapEvent chromaData = BeatmapObject.GenerateCopy(queuedData);
            chromaData._time -= 1 / 64f;
            chromaData._value = ColourManager.ColourToInt(colorPicker.CurrentColor);
            chroma = objectContainerCollection.SpawnObject(chromaData, out _) as BeatmapEventContainer;
        }
        BeatmapActionContainer.AddAction(new BeatmapObjectPlacementAction(new List<BeatmapObjectContainer>() { conflicting },
            new List<BeatmapObjectContainer>() { spawned, chroma }));
        SelectionController.RefreshMap();
        queuedData = BeatmapObject.GenerateCopy(queuedData);
        if (queuedData._type == MapEvent.EVENT_TYPE_EARLY_ROTATION || queuedData._type == MapEvent.EVENT_TYPE_LATE_ROTATION)
            tracksManager.RefreshTracks();
    }

    public override void TransferQueuedToDraggedObject(ref MapEvent dragged, MapEvent queued)
    {
        dragged._time = queued._time;
        dragged._type = queued._type;
    }
}
