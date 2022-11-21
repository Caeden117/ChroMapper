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
    private readonly string[] rotationTransitionMark = { "", "T", "Ti", "To", "Tio" };

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
        var ret = "";
        if (filter.Chunk != 0)
        {
            ret += $"{filter.Chunk}C";
        }

        switch (filter.FilterType)
        {
            case 1: // fraction like text, but default will be ignored for better viewing
                if (filter.Section == 0 && filter.Partition == 1 && filter.Reverse == 0) ret += "";
                ret += (filter.Reverse != 0 ? "-" : "") + (filter.Section + 1) + "/" + filter.Partition;
                break;
            case 2: // python indexing like text
                ret += filter.Partition + ": :" + (filter.Reverse != 0 ? "-" : "") + filter.Section;
                break;
            default:
                Debug.LogError("Unexpected filter type " + filter.FilterType);
                ret += "";
                break;
        }

        if (filter.RandomType != 0 || filter.Limit != 0) ret += "\n"; 

        if (filter.RandomType != 0)
        {
            ret += filter.RandomType == 1 ? "RS" : "R";
        }

        if (filter.Limit != 0)
        {
            ret += $"{filter.Limit}%L";
            if (filter.TimeLimited) ret += "T";
            if (filter.DataLimited) ret += "D";
        }
        return ret;
    }

    private string GenerateDistributionString(float w, int d, bool actuallyAffect = true)
    {
        var connector = actuallyAffect ? "->" : "|>";
        if (w == 0.0f) return "";
        switch (d)
        {
            case 1: // Wave
                return connector + w.ToString("n1");
            case 2: // Step
                return w.ToString("n1") + connector;
            default:
                Debug.LogError("Unexpected distribution type " + d);
                return "";
        }
    }

    public void SetLightColorEventAppearance(BeatmapLightColorEventContainer e, bool boost = false, int dataIdx = 0, bool final = true)
    {
        e.UpdateAlpha(final ? 1.0f : 0.6f, true);
        if (!final)
            e.transform.localScale = Vector3.one * 0.6f;
        if (dataIdx == 0 && e.LightEventData.EventBoxes[0].EventDatas.Count == 0)
        {
            e.ChangeColor(offColor);
            e.UpdateTextDisplay(true, "??????"); // why would this happen?
            return;
        }
        var color = Color.white;
        var eb = e.LightEventData.EventBoxes[0];
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
        switch (eb.EventDatas[dataIdx].TransitionType)
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
            + "/" + GenerateDistributionString(eb.BrightnessDistribution, eb.BrightnessDistributionType, dataIdx != 0 || eb.BrightnessAffectFirst == 1);

        text += "\n";

        e.UpdateTextDisplay(true, text);


        e.EventModel = Settings.Instance.EventModel;
        e.ChangeColor(color, dataIdx != 0 || !final); // idk why we have to update materials for sub notes
        e.ChangeBaseColor(Color.black, dataIdx != 0 || !final);
        if (color == Color.white) e.UpdateTextColor(Color.black);
        else e.UpdateTextColor(Color.white);
    }

    public void SetLightRotationEventAppearance(BeatmapLightRotationEventContainer e, int dataIdx = 0, bool final = true)
    {
        e.UpdateAlpha(final ? 1.0f : 0.6f, true);
        if (!final)
            e.transform.localScale = Vector3.one * 0.6f;
        if (dataIdx == 0 && e.RotationEventData.EventBoxes[0].EventDatas.Count == 0)
        {
            e.ChangeColor(offColor);
            e.UpdateTextDisplay(true, "??????"); // why would this happen again?
            return;
        }
        var eb = e.RotationEventData.EventBoxes[0];
        var ebd = eb.EventDatas[dataIdx];
        var text = GenerateFilterString(eb.Filter);
        var prefix = "";
        if (ebd.Transition == 1)
        {
            prefix = "E";
        }
        else
        {
            prefix = rotationTransitionMark[ebd.EaseType + 1];
        }
        text = prefix + text;

        text += "\n" + GenerateDistributionString(eb.Distribution, eb.DistributionType)
            + "/" + GenerateDistributionString(eb.RotationDistribution, eb.RotationDistributionType, dataIdx != 0 || eb.RotationAffectFirst == 1);
        text += "\n" + rotationDirectionMark[ebd.RotationDirection] + ebd.AdditionalLoop + (eb.ReverseRotation == 1 ? "-" : "+") + ebd.RotationValue + "°";

        e.UpdateTextDisplay(true, text);
        e.ChangeColor(offColor, dataIdx != 0 || !final);

        e.SetRotationAxisAppearance(eb.Axis);
    }
}

public enum EventModelType
{
    Block = 0,
    Pyramid = 1,
    FlatPyramid = 2
}
