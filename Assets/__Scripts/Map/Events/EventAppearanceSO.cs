using UnityEngine;

[CreateAssetMenu(fileName = "EventAppearanceSO", menuName = "Map/Appearance/Event Appearance SO")]
public class EventAppearanceSO : ScriptableObject
{
    [Space(5)]
    [SerializeField] private GameObject LaserSpeedPrefab;
    [Space(5)]
    [Header("Default Colors")]
    [SerializeField] public Color RedColor;
    [SerializeField] public Color BlueColor;
    [SerializeField] public Color RedBoostColor;
    [SerializeField] public Color BlueBoostColor;
    [SerializeField] private Color OffColor;
    [Header("Other Event Colors")]
    [SerializeField] private Color RingEventsColor;
    [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
    [SerializeField] private Color OtherColor;
    [Space(5)]
    [Header("Shader Parameters")]
    [Header("Cube")]
    [SerializeField] private Vector3 cubeFlashShaderOffset = new Vector3(0, 1, 0);
    [SerializeField] private Vector3 cubeFadeShaderOffset = new Vector3(0, -1, 0);
    [SerializeField] private float cubeDefaultFadeSize = 0.5f;
    [SerializeField] private float cubeBoostEventFadeSize = 0.1f;
    [Header("Pyramid")]
    [SerializeField] private Vector3 pyramidFlashShaderOffset = new Vector3(0, 0, 50);
    [SerializeField] private Vector3 pyramidFadeShaderOffset = Vector3.zero;
    [SerializeField] private float pyramidDefaultFadeSize = 50f;
    [SerializeField] private float pyramidBoostEventFadeSize = 10f;

    public void SetEventAppearance(BeatmapEventContainer e, bool final = true, bool boost = false) {
        Color color = Color.white;
        e.UpdateOffset(Vector3.zero, false);
        e.UpdateAlpha(final ? 1.0f : 0.6f, false);
        e.UpdateScale(final ? 0.75f : 0.6f);
        e.ChangeSpotlightSize(1f, false);
        if (e.eventData.IsRotationEvent || e.eventData.IsLaserSpeedEvent || e.eventData.IsInterscopeEvent)
        {
            if (e.eventData.IsRotationEvent)
            {
                int? rotation = e.eventData.GetRotationDegreeFromValue();
                e.UpdateTextDisplay(true, rotation != null ? $"{rotation}°" : "Invalid Rotation");
            }
            else if (e.eventData.IsLaserSpeedEvent || e.eventData.IsInterscopeEvent)
            {
                float speed = e.eventData._value;
                if (e.eventData._customData != null && e.eventData._customData.HasKey("_preciseSpeed"))
                {
                    speed = e.eventData._customData["_preciseSpeed"].AsFloat;
                }
                e.UpdateTextDisplay(true, speed.ToString());
            }
        }
        else e.UpdateTextDisplay(false);
        if (e.eventData.IsUtilityEvent)
        {
            e.UsePyramidModel = false;
            if (e.eventData.IsRingEvent)
            {
                e.ChangeColor(RingEventsColor, false);
                e.ChangeBaseColor(RingEventsColor, false);
            }
            else if (e.eventData._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS)
            {
                if (e.eventData._value == 1)
                {
                    e.ChangeBaseColor(RedBoostColor, false);
                    e.ChangeColor(BlueBoostColor, false);
                }
                else
                {
                    e.ChangeBaseColor(RedColor, false);
                    e.ChangeColor(BlueColor, false);
                }
                e.UpdateOffset(Vector3.forward * 1.05f, false);
                e.ChangeFadeSize(cubeBoostEventFadeSize, false);
                e.UpdateMaterials();
                return;
            }
            else
            {
                e.ChangeColor(OtherColor, false);
                e.ChangeBaseColor(OtherColor, false);
            }
            e.UpdateOffset(Vector3.zero, false);
            e.UpdateGradientRendering();
            e.UpdateMaterials();
            return;
        }
        else
        {
            if (e.eventData._value >= ColourManager.RGB_INT_OFFSET)
            {
                color = ColourManager.ColourFromInt(e.eventData._value);
                e.UpdateAlpha(final ? 0.9f : 0.6f, false);
            }
            else if (e.eventData._value <= 3)
            {
                color = boost ? BlueBoostColor : BlueColor;
            }
            else if (e.eventData._value <= 7 && e.eventData._value >= 5)
            {
                color = boost ? RedBoostColor : RedColor;
            }
            else if (e.eventData._value == 4) color = OffColor;

            if (e.eventData._customData?["_color"] != null && e.eventData._value > 0)
            {
                color = e.eventData._customData["_color"];
            }
        }
        e.UsePyramidModel = Settings.Instance.PyramidEventModels;
        e.ChangeColor(color, false);
        e.ChangeBaseColor(Color.black, false);
        switch (e.eventData._value)
        {
            case MapEvent.LIGHT_VALUE_OFF:
                e.ChangeColor(OffColor, false);
                e.ChangeBaseColor(OffColor, false);
                e.UpdateOffset(Vector3.zero, false);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_ON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FLASH:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFlashShaderOffset : cubeFlashShaderOffset, false);
                break;
            case MapEvent.LIGHT_VALUE_BLUE_FADE:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFadeShaderOffset : cubeFadeShaderOffset, false);
                break;
            case MapEvent.LIGHT_VALUE_RED_ON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEvent.LIGHT_VALUE_RED_FLASH:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFlashShaderOffset : cubeFlashShaderOffset, false);
                break;
            case MapEvent.LIGHT_VALUE_RED_FADE:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFadeShaderOffset : cubeFadeShaderOffset, false);
                break;
        }

        e.ChangeFadeSize(e.UsePyramidModel ? pyramidDefaultFadeSize : cubeDefaultFadeSize, false);

        if (Settings.Instance.VisualizeChromaGradients) e.UpdateGradientRendering();

        e.UpdateMaterials();
    }
}
