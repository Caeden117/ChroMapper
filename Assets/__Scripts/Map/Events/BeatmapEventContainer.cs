using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BeatmapEventContainer : BeatmapObjectContainer {

	public override BeatmapObject objectData
    {
        get
        {
            return eventData;
        }
    }

    public MapEvent eventData;

    [SerializeField] private EventAppearanceSO eventAppearance;

    private new Renderer renderer = null;
    private Material mat = null;
    private float oldAlpha = -1;

    /// <summary>
    /// Different modes to sort events in the editor.
    /// </summary>
    public static int ModifyTypeMode = 0;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => GetComponentInChildren<Renderer>().materials.Any());
        renderer = GetComponentInChildren<Renderer>();
        mat = renderer.material;
    }

    public static BeatmapEventContainer SpawnEvent(MapEvent data, ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        BeatmapEventContainer container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.eventData = data;
        container.eventAppearance = eventAppearanceSO;
        eventAppearanceSO.SetEventAppearance(container);
        return container;
    }

    public override void UpdateGridPosition()
    {
        transform.localPosition = new Vector3(
            EventTypeToModifiedType(eventData._type) + 0.5f,
            0.5f,
            eventData._time * EditorScaleController.EditorScale
            );
    }


    /// <summary>
    /// Turns an eventType to a modified type for organizational purposes in the Events Grid.
    /// </summary>
    /// <param name="eventType">Type usually found in a MapEvent object.</param>
    /// <returns></returns>
    public static int EventTypeToModifiedType(int eventType)
    {
        if (ModifyTypeMode == -1) return eventType;
        if (ModifyTypeMode == 0)
            switch (eventType)
            {
                case 8: return 5;
                case 9: return 6;
                case 12: return 7;
                case 13: return 8;
                case 5: return 9;
                case 6: return 10;
                case 7: return 11;
                case 10: return 12;
                case 11: return 13;
                default: return eventType;
            }
        else if (ModifyTypeMode == 1)
            switch (eventType)
            {
                case 5: return 1;
                case 1: return 2;
                case 6: return 3;
                case 2: return 4;
                case 7: return 5;
                case 3: return 6;
                case 10: return 7;
                case 4: return 8;
                case 11: return 9;
                case 8: return 10;
                case 9: return 11;
                default: return eventType;
            }
        return -1;
    }

    /// <summary>
    /// Turns a modified type to an event type to be stored in a MapEvent object.
    /// </summary>
    /// <param name="modifiedType">Modified type (Usually from EventPreview)</param>
    /// <returns></returns>
    public static int ModifiedTypeToEventType(int modifiedType)
    {
        if (ModifyTypeMode == -1) return modifiedType;
        if (ModifyTypeMode == 0)
            switch (modifiedType)
            {
                case 5: return 8;
                case 6: return 9;
                case 7: return 12;
                case 8: return 13;
                case 9: return 5;
                case 10: return 6;
                case 11: return 7;
                case 12: return 10;
                case 13: return 11;
                default: return modifiedType;
            }
        else if (ModifyTypeMode == 1)
            switch (modifiedType)
            {
                case 1: return 5;
                case 2: return 1;
                case 3: return 6;
                case 4: return 2;
                case 5: return 7;
                case 6: return 3;
                case 7: return 10;
                case 8: return 4;
                case 9: return 11;
                case 10: return 8;
                case 11: return 9;
                default: return modifiedType;
            }
        return -1;
    }

    public void ChangeColor(Color color)
    {
        if (gameObject.activeSelf) StartCoroutine(changeColor(color));
    }

    public void UpdateOffset(Vector3 offset)
    {
        if (gameObject.activeSelf) StartCoroutine(updateOffset(offset));
    }

    public void UpdateAlpha(float alpha)
    {
        if (gameObject.activeSelf) StartCoroutine(updateAlpha(alpha));
    }

    private IEnumerator changeColor(Color color)
    {
        yield return new WaitUntil(() => mat != null);
        mat.SetColor("_ColorTint", color);
    }

    private IEnumerator updateOffset(Vector3 offset)
    {
        yield return new WaitUntil(() => mat != null);
        mat.SetVector("_Position", offset);
    }

    private IEnumerator updateAlpha(float alpha = -1)
    {
        yield return new WaitUntil(() => mat != null);
        if (mat.GetFloat("_MainAlpha") > 0) oldAlpha = mat.GetFloat("_MainAlpha");
        mat.SetFloat("_MainAlpha", alpha == -1 ? oldAlpha : alpha);
    }

    internal override void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(2) && !KeybindsController.ShiftHeld)
        {
            if (eventData.IsUtilityEvent()) return;
            if (eventData._value > 4 && eventData._value < 8) eventData._value -= 4;
            else if (eventData._value > 0) eventData._value += 4;
            eventAppearance.SetEventAppearance(this);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (KeybindsController.AltHeld && (eventData._type == MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED || eventData._type == MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED))
            {
                eventData._value += Input.GetAxis("Mouse ScrollWheel") > 0 ? 1 : -1;
                if (eventData._value < 0) eventData._value = 0;
                eventAppearance.SetEventAppearance(this);
            }
        }
        else base.OnMouseOver();
    }

    internal override void SafeSetActive(bool active)
    {
        if (active != (renderer is null ? active : renderer.enabled))
        {
            renderer.enabled = active;
            TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.enabled = active;
            boxCollider.enabled = active;
        }
    }
}
