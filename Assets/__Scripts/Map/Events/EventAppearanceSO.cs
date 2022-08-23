using System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "EventAppearanceSO", menuName = "Map/Appearance/Event Appearance SO")]
public class EventAppearanceSO : ScriptableObject
{
    [FormerlySerializedAs("LaserSpeedPrefab")] [Space(5)] [SerializeField] private GameObject laserSpeedPrefab;

    [Space(5)] [Header("Default Colors")] public Color RedColor;
    public Color BlueColor;
    public Color RedBoostColor;
    public Color BlueBoostColor;
    [FormerlySerializedAs("OffColor")] [SerializeField] private Color offColor;

    [FormerlySerializedAs("RingEventsColor")]
    [Header("Other Event Colors")]
    [SerializeField]
    private Color ringEventsColor;

    [FormerlySerializedAs("OtherColor")]
    [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
    [SerializeField]
    private Color otherColor;

    private readonly string[] rotationDirectionMark = { "", "↻", "↺" };

    public void SetEventAppearance(BeatmapEventContainer e, bool final = true, bool boost = false)
    {
        var color = Color.white;
        e.UpdateOffset(Vector3.zero, false);
        e.UpdateAlpha(final ? 1.0f : 0.6f, false);
        e.UpdateScale(final ? 0.75f : 0.6f);
        e.ChangeSpotlightSize(1f, false);
        if (e.EventData.IsRotationEvent || e.EventData.IsLaserSpeedEvent || e.EventData.IsInterscopeEvent)
        {
            if (e.EventData.IsRotationEvent)
            {
                var rotation = e.EventData.GetRotationDegreeFromValue();
                e.UpdateTextDisplay(true, rotation != null ? $"{rotation}°" : "Invalid Rotation");
            }
            else if (e.EventData.IsLaserSpeedEvent || e.EventData.IsInterscopeEvent)
            {
                float speed = e.EventData.Value;
                if (e.EventData.CustomData != null)
                {
                    if (e.EventData.CustomData.HasKey("_preciseSpeed"))
                        speed = e.EventData.CustomData["_preciseSpeed"].AsFloat;
                    else if (e.EventData.CustomData.HasKey("_speed"))
                        speed = e.EventData.CustomData["_speed"].AsFloat;
                }

                e.UpdateTextDisplay(true, speed.ToString());
            }
        }
        else
        {
            e.UpdateTextDisplay(false);
        }

        if (e.EventData.IsUtilityEvent)
        {
            e.EventModel = EventModelType.Block;

            if (e.EventData.IsRingEvent)
            {
                e.ChangeColor(ringEventsColor, false);
                e.ChangeBaseColor(ringEventsColor, false);
            }
            else if (e.EventData.Type == MapEvent.EventTypeBoostLights)
            {
                if (e.EventData.Value == 1)
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
                e.ChangeFadeSize(e.BoostEventFadeSize, false);
                e.UpdateMaterials();
                return;
            }
            else
            {
                e.ChangeColor(otherColor, false);
                e.ChangeBaseColor(otherColor, false);
            }

            e.UpdateOffset(Vector3.zero, false);
            e.UpdateGradientRendering();
            e.UpdateMaterials();
            return;
        }

        if (e.EventData.Value >= ColourManager.RgbintOffset)
        {
            color = ColourManager.ColourFromInt(e.EventData.Value);
            e.UpdateAlpha(final ? 0.9f : 0.6f, false);
        }
        else if (e.EventData.Value <= 4)
        {
            color = boost ? BlueBoostColor : BlueColor;
        }
        else if (e.EventData.Value <= 8 && e.EventData.Value >= 5)
        {
            color = boost ? RedBoostColor : RedColor;
        }
        if (Settings.Instance.EmulateChromaLite && e.EventData.CustomData?["_color"] != null && e.EventData.Value > 0)
            color = e.EventData.CustomData["_color"];

        // Display floatValue only where used
        if (!e.EventData.IsUtilityEvent && e.EventData.Value != 0)
        {
            if (Settings.Instance.DisplayFloatValueText)
            {
                if (e.EventData.Value == 4 || e.EventData.Value == 8 || e.EventData.Value == 12) // Transition Event in event v3
                {
                    e.UpdateTextDisplay(true, "T" + (Mathf.Approximately(e.EventData.FloatValue, 1) ? "1" : e.EventData.FloatValue.ToString("n2").Substring(1)));
                }
                else
                {
                    e.UpdateTextDisplay(true, Mathf.Approximately(e.EventData.FloatValue, 1) ? "1" : e.EventData.FloatValue.ToString("n2").Substring(1));
                }
            }
            
            color = Color.Lerp(offColor, color, e.EventData.FloatValue);
            if (color == Color.white) e.UpdateTextColor(Color.black);
            else e.UpdateTextColor(Color.white); // this may overwrite some configs
        }

        e.EventModel = Settings.Instance.EventModel;
        e.ChangeColor(color, false);
        e.ChangeBaseColor(Color.black, false);
        switch (e.EventData.Value)
        {
            case MapEvent.LightValueOff:
                e.ChangeColor(offColor, false);
                e.ChangeBaseColor(offColor, false);
                e.UpdateOffset(Vector3.zero, false);
                break;
            case MapEvent.LightValueBlueON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEvent.LightValueBlueFlash:
                e.UpdateOffset(e.FlashShaderOffset, false);
                break;
            case MapEvent.LightValueBlueFade:
                e.UpdateOffset(e.FadeShaderOffset, false);
                break;
            case MapEvent.LightValueRedON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEvent.LightValueRedFlash:
                e.UpdateOffset(e.FlashShaderOffset, false);
                break;
            case MapEvent.LightValueRedFade:
                e.UpdateOffset(e.FadeShaderOffset, false);
                break;

            case MapEventV3.LightValueBlueTransition:
                e.ChangeBaseColor(color, false);
                break;
            case MapEventV3.LightValueRedTransition:
                e.ChangeBaseColor(color, false);
                break;
            case MapEventV3.LightValueWhiteON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEventV3.LightValueWhiteFlash:
                e.UpdateOffset(e.FlashShaderOffset, false);
                break;
            case MapEventV3.LightValueWhiteFade:
                e.UpdateOffset(e.FadeShaderOffset, false);
                break;
            case MapEventV3.LightValueWhiteTransition:
                e.ChangeBaseColor(color, false);
                break;
        }

        e.ChangeFadeSize(e.DefaultFadeSize, false);

        if (Settings.Instance.VisualizeChromaGradients) e.UpdateGradientRendering();

        e.UpdateMaterials();
    }

    private string GenerateFilterString(BeatmapLightEventFilter filter)
    {
        switch (filter.FilterType)
        {
            case 1: // fraction like text, but default will be ignored for better viewing
                if (filter.Section == 0 && filter.Partition == 1) return "";
                return (filter.Reverse != 0 ? "-" : "") + (filter.Section + 1) + "/" + filter.Partition;
            case 2: // python indexing like text
                return filter.Partition + ": :" + (filter.Reverse != 0 ? "-" : "") + filter.Section;
            default:
                Debug.LogError("Unexpected filter type " + filter.FilterType);
                return "";
        }
    }

    private string GenerateDistributionString(float w, int d)
    {
        if (w == 0.0f) return "";
        switch (d)
        {
            case 1: // Wave
                return "->" + w.ToString("n1");
            case 2: // Step
                return w.ToString("n1") + "->";
            default:
                Debug.LogError("Unexpected distribution type " + d);
                return "";
        }
    }

    public void SetLightColorEventAppearance(BeatmapLightColorEventContainer e, bool boost = false, int dataIdx = 0)
    {
        var color = Color.white;
        var eb = e.ColorEventData.EventBoxes[0];
        if (eb.EventDatas[dataIdx].Color <= 1)
        {
            color = eb.EventDatas[dataIdx].Color == 1
                ? (boost ? BlueBoostColor : BlueColor)
                : (boost ? RedBoostColor : RedColor);
        }
        color = Color.Lerp(offColor, color, eb.EventDatas[dataIdx].Brightness);

        // first line: transition + filter 
        var text = GenerateFilterString(eb.Filter);
        var prefix = "";
        switch (eb.EventDatas[0].TransitionType)
        {
            case 1:
                prefix = "T";
                break;
            case 2:
                prefix = "E";
                break;
            default:
                break;
        }
        text = prefix + text;
        // second line: two distributions
        text += "\n" + GenerateDistributionString(eb.Distribution, eb.DistributionType) 
            + "/" + GenerateDistributionString(eb.BrightnessDistribution, eb.BrightnessDistributionType);

        text += "\n";

        e.UpdateTextDisplay(true, text);


        e.EventModel = Settings.Instance.EventModel;
        e.ChangeColor(color, dataIdx != 0); // idk why we have to update materials for sub notes
        e.ChangeBaseColor(Color.black, dataIdx != 0);
        if (color == Color.white) e.UpdateTextColor(Color.black);
        else e.UpdateTextColor(Color.white);
    }

    public void SetLightRotationEventAppearance(BeatmapLightRotationEventContainer e)
    {
        var eb = e.RotationEventData.EventBoxes[0];
        var ebd = eb.EventDatas[0];
        var text = GenerateFilterString(eb.Filter);
        var prefix = "";
        switch (ebd.Transition)
        {
            case 0:
                prefix = "T";
                break;
            case 1:
                prefix = "E";
                break;
            default:
                break;
        }
        text = prefix + text;

        text += "\n" + GenerateDistributionString(eb.Distribution, eb.DistributionType)
            + "/" + GenerateDistributionString(eb.RotationDistribution, eb.RotationDistributionType);
        text += "\n" + rotationDirectionMark[ebd.RotationDirection] + ebd.AdditionalLoop + "+" + ebd.RotationValue + "°";

        e.UpdateTextDisplay(true, text);
        e.ChangeColor(offColor, false);
    }
}

public enum EventModelType
{
    Block = 0,
    Pyramid = 1,
    FlatPyramid = 2
}
