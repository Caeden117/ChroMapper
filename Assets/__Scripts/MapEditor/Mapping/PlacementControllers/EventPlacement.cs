using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventPlacement : PlacementController<MapEvent, BeatmapEventContainer, EventsContainer>
{
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private InputField laserSpeedInputField;
    private int queuedValue = MapEvent.LIGHT_VALUE_RED_ON;

    public override BeatmapAction GenerateAction(BeatmapEventContainer spawned)
    {
        return new BeatmapEventPlacementAction(spawned, null);
    }

    public override MapEvent GenerateOriginalData()
    {
        return new MapEvent(0, 0, MapEvent.LIGHT_VALUE_RED_ON);
    }

    public override void OnPhysicsRaycast(RaycastHit hit)
    {
        queuedData._type = BeatmapEventContainer.ModifiedTypeToEventType(Mathf.RoundToInt(instantiatedContainer.transform.position.x - transform.position.x));
        queuedData._value = queuedValue;
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
        if (instantiatedContainer is null) return;
        instantiatedContainer.eventData = queuedData;
        eventAppearanceSO.SetEventAppearance(instantiatedContainer);
    }
}
