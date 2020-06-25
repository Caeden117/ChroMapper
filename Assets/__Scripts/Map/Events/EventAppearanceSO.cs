using TMPro;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EventAppearanceSO", menuName = "Map/Appearance/Event Appearance SO")]
public class EventAppearanceSO : ScriptableObject
{
    [SerializeField] private Vector3 FlashShaderOffset;
    [SerializeField] private Vector3 FadeShaderOffset;
    [Space(5)]
    [SerializeField] private GameObject LaserSpeedPrefab;
    [Space(5)]
    [Header("Default Colors")]
    [SerializeField] public Color RedColor;
    [SerializeField] public Color BlueColor;
    [SerializeField] private Color OffColor;
    [Header("Other Event Colors")]
    [SerializeField] private Color RingEventsColor;
    [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
    [SerializeField] private Color OtherColor;

    public void SetEventAppearance(BeatmapEventContainer e, bool final = true, PlatformDescriptor platform = null) {
        if (platform != null)
        {
            RedColor = platform.RedColor;
            BlueColor = platform.BlueColor;
        }
        Color color = Color.white;
        e.UpdateOffset(Vector3.zero);
        e.UpdateAlpha(final ? 1.0f : 0.6f);
        e.UpdateScale(final ? 0.75f : 0.6f);
        if (e.eventData.IsRotationEvent || e.eventData.IsLaserSpeedEvent)
        {
            if (e.eventData.IsRotationEvent)
            {
                int? rotation = e.eventData.GetRotationDegreeFromValue();
                e.UpdateTextDisplay(true, rotation != null ? $"{rotation}°" : "Invalid Rotation");
            }
            else e.UpdateTextDisplay(true, e.eventData._value.ToString());
        }
        else e.UpdateTextDisplay(false);
        if (e.eventData.IsUtilityEvent)
        {
            if (e.eventData.IsRingEvent) e.ChangeColor(RingEventsColor);
            else e.ChangeColor(OtherColor);
            e.UpdateOffset(Vector3.zero);
            e.UpdateGradientRendering();
            return;
        }
        else
        {
            if (e.eventData._value >= ColourManager.RGB_INT_OFFSET)
            {
                color = ColourManager.ColourFromInt(e.eventData._value);
                e.UpdateAlpha(final ? 0.9f : 0.6f);
            }
            else if (e.eventData._value <= 3)
            {
                color = BlueColor;
            }
            else if (e.eventData._value <= 7 && e.eventData._value >= 5)
            {
                color = RedColor;
            }
            else if (e.eventData._value == 4) color = OffColor;
        }
        e.ChangeColor(color);
        switch (e.eventData._value)
        {
            case MapEvent.LIGHT_VALUE_OFF:
                e.ChangeColor(OffColor);
                e.UpdateOffset(Vector3.zero);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_ON:
                e.UpdateOffset(Vector3.zero);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FLASH:
                e.UpdateOffset(FlashShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FADE:
                e.UpdateOffset(FadeShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_RED_ON:
                e.UpdateOffset(Vector3.zero);
                break;
            case MapEvent.LIGHT_VALUE_RED_FLASH:
                e.UpdateOffset(FlashShaderOffset);
                break;
            case MapEvent.LIGHT_VALUE_RED_FADE:
                e.UpdateOffset(FadeShaderOffset);
                break;
        }
        if (e.eventData._customData?["_color"] != null && e.eventData._value > 0)
        {
            e.ChangeColor(e.eventData._customData["_color"]);
        }
        if (Settings.Instance.VisualizeChromaGradients) e.UpdateGradientRendering();
    }
}
