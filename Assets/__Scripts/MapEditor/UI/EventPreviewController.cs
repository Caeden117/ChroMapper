using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class EventPreviewController : MonoBehaviour {

    [SerializeField] private Toggle[] EventTypeToggles;
    [SerializeField] private Toggle[] RedBlueToggles;
    internal static EventPreviewController Instance;

    private ToggleGroup test;
    private static bool IsRedNote = true;

    void Start()
    {
        Instance = this;
        if (EventTypeToggles.Length == 0) return;
        foreach (Toggle toggle in EventTypeToggles) toggle.onValueChanged.AddListener((x) => EventTypeChanged(x, toggle.name));
        foreach (Toggle toggle in RedBlueToggles) toggle.onValueChanged.AddListener((x) => EventColorChanged(x, toggle.transform.parent.name));
    }

    void EventColorChanged(bool state, string name)
    {
        if (!state) return;
        string eventColor = name.Split(' ').First();
        switch (eventColor.ToUpper())
        {
            case "RED":
                IsRedNote = true;
                break;
            case "BLUE":
                IsRedNote = false;
                break;
        }
        switch (EventPreview.QueuedValue)
        {
            case MapEvent.LIGHT_VALUE_BLUE_ON: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_ON); break;
            case MapEvent.LIGHT_VALUE_RED_ON: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_ON); break;
            case MapEvent.LIGHT_VALUE_RED_FADE: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_FADE); break;
            case MapEvent.LIGHT_VALUE_BLUE_FADE: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_FADE); break;
            case MapEvent.LIGHT_VALUE_BLUE_FLASH: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_RED_FLASH); break;
            case MapEvent.LIGHT_VALUE_RED_FLASH: EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_BLUE_FLASH); break;
        }
    }

    void EventTypeChanged(bool state, string name)
    {
        if (!state) return;
        List<string> splitName = name.Split(' ').ToList();
        string eventType = splitName.Last();
        switch (eventType.ToUpper())
        {
            case "OFF":
                EventPreview.UpdateHoverEventValue(MapEvent.LIGHT_VALUE_OFF);
                break;
            case "ON":
                EventPreview.UpdateHoverEventValue(IsRedNote ? MapEvent.LIGHT_VALUE_RED_ON : MapEvent.LIGHT_VALUE_BLUE_ON);
                break;
            case "FADE":
                EventPreview.UpdateHoverEventValue(IsRedNote ? MapEvent.LIGHT_VALUE_RED_FADE : MapEvent.LIGHT_VALUE_BLUE_FADE);
                break;
            case "FLASH":
                EventPreview.UpdateHoverEventValue(IsRedNote ? MapEvent.LIGHT_VALUE_RED_FLASH : MapEvent.LIGHT_VALUE_BLUE_FLASH);
                break;
        }
    }
}
