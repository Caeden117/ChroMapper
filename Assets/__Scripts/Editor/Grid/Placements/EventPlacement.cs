using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SimpleJSON;

public class EventPlacement : BasePlacement<BaseEvent, EventContainer, EventGridContainer>,
                              CMInput.IEventPlacementActions,
                              IEditorStateProvider
{
    [SerializeField] private EventAppearanceSO eventAppearance;

    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private TMP_InputField laserSpeedInputField;
    [SerializeField] private Toggle chromaToggle;
    [SerializeField] private Toggle redEventToggle;
    [SerializeField] private ToggleColourDropdown dropdown;
    [SerializeField] private CreateEventTypeLabels labels;

    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    private bool isHalfFloatValuePressed;
    private bool isZeroFloatValuePressed;
    internal float queuedFloatValue = 1.0f;

    internal int queuedValue = (int)LightValue.RedOn;

    // Expose basic-event placement state for map-scoped editor metadata without duplicating UI ownership.
    public float QueuedFloatValue => queuedFloatValue;
    public int QueuedValue => queuedValue;
    public string LaserSpeedText => laserSpeedInputField.text;

    // Persist basic-event placement values with their owning placement component.
    public string StateKey => "basicEventPlacement";

    public override void Start()
    {
        base.Start();
        EditorStateService.Register(this);
    }

    // Stop autosaves from retaining this placement after its UI has been destroyed.
    public void OnDestroy() => EditorStateService.Unregister(this);

    // Populate map metadata from this placement's own queued values.
    public void CaptureEditorState(JSONObject data)
    {
        data["value"] = queuedValue;
        data["floatValue"] = queuedFloatValue;
        data["laserSpeed"] = laserSpeedInputField.text;
    }

    // Apply only saved fields so maps created before a field existed retain their normal placement defaults.
    public void LoadEditorState(JSONNode data)
    {
        var value = data.HasKey("value")
            ? data["value"].AsInt
            : queuedValue;
        var floatValue = data.HasKey("floatValue")
            ? data["floatValue"].AsFloat
            : queuedFloatValue;
        var laserSpeed = data.HasKey("laserSpeed")
            ? data["laserSpeed"].Value
            : laserSpeedInputField.text;
        RestoreEditorState(value, floatValue, laserSpeed);
    }

    // Restore basic-event data and its laser-speed field after the editor UI has initialized.
    public void RestoreEditorState(int value, float floatValue, string laserSpeed)
    {
        queuedValue = value;
        queuedFloatValue = floatValue;
        laserSpeedInputField.SetTextWithoutNotify(laserSpeed ?? string.Empty);
        UpdateQueuedValue(queuedValue);
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateAppearance();
    }

    public static bool CanPlaceChromaEvents => Settings.Instance.PlaceChromaColor;

    public void OnHalfFloatValueModifier(InputAction.CallbackContext context) =>
        isHalfFloatValuePressed = context.performed;

    public void OnZeroFloatValueModifier(InputAction.CallbackContext context) =>
        isZeroFloatValuePressed = context.performed;

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed an Event.");

    protected override BaseEvent GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        base.HandleHitToPlacement(hit, localPoint);

        // HoverPastingBasicEventsBeforeBeatZeroAnchorsEarliestAtBeatZero and
        // HoverPastingLightIdEventOntoAllLightsBeforeBeatZeroClampsToBeatZero keep the visible Basic Event anchor and
        // its queued paste data at the map boundary instead of allowing either to retain a negative hover beat.
        if (QueuedData.JsonTime < 0f)
        {
            RoundedJsonTime = 0f;
            QueuedData.JsonTime = 0f;
            var clampedPosition = PlacementVisualContainer.transform.localPosition;
            clampedPosition.z = 0f;
            PlacementVisualContainer.transform.localPosition = clampedPosition;
        }

        // The generic placement grid centers previews; lower this smaller model so Basic Event previews share finalized nodes' grounded base.
        var position = PlacementVisualContainer.transform.localPosition;
        position.y = EventAppearanceSO.GetGroundedNodeCenterY(false);
        PlacementVisualContainer.transform.localPosition = position;
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (ObjectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Off)
        {
            QueuedData.Type =
                labels.LaneIdToEventType(Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x));
            QueuedData.CustomLightID = null;
        }
        else
        {
            var propID = Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x - 1);
            QueuedData.Type = ObjectContainerCollection.EventTypeToPropagate;

            if (propID >= 0)
            {
                var lightIdToApply = ObjectContainerCollection.PropagationEditing == EventGridContainer.PropMode.Prop
                    ? labels.PropIdToLightIds(ObjectContainerCollection.EventTypeToPropagate, propID)
                    : new[] { labels.LaneToLightID(ObjectContainerCollection.EventTypeToPropagate, propID) };
                QueuedData.CustomLightID = lightIdToApply;
            }
            else
                QueuedData.CustomLightID = null;
        }

        // Chroma event placement follows the color tile's setting even after its picker flyout is closed.
        if (CanPlaceChromaEvents
            && beatmapRuntimeContext.TrackDefinitions.GetBasicOrDefault(QueuedData.Type).Kind == BasicEventKind.Lights
            && QueuedData.Value != (int)LightValue.Off)
            QueuedData.CustomColor = colorPicker.CurrentColor;
        else
            QueuedData.CustomColor = null;

        UpdateQueuedValue(queuedValue);
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateAppearance();
    }

    public void UpdateQueuedValue(int value)
    {
        QueuedData.Value = value;

        if (beatmapRuntimeContext.TrackDefinitions.GetBasicOrDefault(QueuedData.Type).Kind == BasicEventKind.IntValue
            && int.TryParse(laserSpeedInputField.text, out var laserSpeed))
            QueuedData.Value = laserSpeed;

        if (QueuedData.IsColorBoostEvent()) QueuedData.Value = QueuedData.Value > 0 ? 1 : 0;
    }

    public void UpdateValue(int value)
    {
        queuedValue = value;
        UpdateQueuedValue(queuedValue);
        UpdateAppearance();
    }

    public void UpdateQueuedFloatValue(float value)
    {
        if (beatmapRuntimeContext.TrackDefinitions.GetBasicOrDefault(QueuedData.Type).Kind != BasicEventKind.Lights)
        {
            QueuedData.FloatValue = 1f;
            return;
        }

        if (isZeroFloatValuePressed)
            QueuedData.FloatValue = 0f;
        else if (isHalfFloatValuePressed)
            QueuedData.FloatValue = value * 0.5f;
        else
            QueuedData.FloatValue = value;
    }

    public void UpdateFloatValue(float value)
    {
        queuedFloatValue = value;
        UpdateQueuedFloatValue(queuedFloatValue);
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (PlacementVisualContainer == null)
        {
            CreateVisual();
            if (IsIdle) HideVisual();
        }

        PlacementVisualContainer!.EventData = QueuedData;
        // Queue previews must resolve the current color-boost state at their own beat like finalized event containers.
        eventAppearance.SetAppearance(
            PlacementVisualContainer,
            false,
            ObjectContainerCollection.IsBoostAt(QueuedData.JsonTime));
    }

    public override void CreateVisual()
    {
        base.CreateVisual();
        PlacementVisualContainer!.TrackDefinitions = beatmapRuntimeContext.TrackDefinitions;
    }

    public void PlaceChroma(bool v) => Settings.Instance.PlaceChromaColor = v;

    public override void HandleApply()
    {
        var evt = QueuedData;
        
        base.HandleApply();

        QueuedData = new BaseEvent(evt); // need to convert back to regular event
        QueuedData.CustomData = null;
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref BaseEvent dragged, BaseEvent queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.Type = queued.Type;
        // Instead of copying the whole custom data, only copy prop ID
        if (dragged.CustomData != null && queued.CustomData != null)
        {
            if (queued.CustomData?[queued.CustomKeyPropID] != null)
                dragged.GetOrCreateCustom()[dragged.CustomKeyPropID] = queued.CustomData[queued.CustomKeyPropID];

            if (queued.CustomLightID != null) dragged.CustomLightID = queued.CustomLightID;
        }
    }

    protected override void HandleDragged() => TracksManager.RefreshTracks();
}
