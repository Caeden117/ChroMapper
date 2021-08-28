﻿using UnityEngine;
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

    [FormerlySerializedAs("RingEventsColor")] [Header("Other Event Colors")] [SerializeField]
    private Color ringEventsColor;

    [FormerlySerializedAs("OtherColor")] [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")] [SerializeField]
    private Color otherColor;

    [Space(5)] [Header("Shader Parameters")] [Header("Cube")] [SerializeField]
    private Vector3 cubeFlashShaderOffset = new Vector3(0, 1, 0);

    [SerializeField] private Vector3 cubeFadeShaderOffset = new Vector3(0, -1, 0);
    [SerializeField] private float cubeDefaultFadeSize = 0.5f;
    [SerializeField] private float cubeBoostEventFadeSize = 0.1f;

    [Header("Pyramid")] [SerializeField] private Vector3 pyramidFlashShaderOffset = new Vector3(0, 0, 50);

    [SerializeField] private Vector3 pyramidFadeShaderOffset = Vector3.zero;
    [SerializeField] private float pyramidDefaultFadeSize = 50f;

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
            e.UsePyramidModel = false;
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
                e.ChangeFadeSize(cubeBoostEventFadeSize, false);
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
        else if (e.EventData.Value <= 3)
        {
            color = boost ? BlueBoostColor : BlueColor;
        }
        else if (e.EventData.Value <= 7 && e.EventData.Value >= 5)
        {
            color = boost ? RedBoostColor : RedColor;
        }
        else if (e.EventData.Value == 4)
        {
            color = offColor;
        }

        if (e.EventData.CustomData?["_color"] != null && e.EventData.Value > 0)
            color = e.EventData.CustomData["_color"];
        e.UsePyramidModel = Settings.Instance.PyramidEventModels;
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
                e.UpdateOffset(e.UsePyramidModel ? pyramidFlashShaderOffset : cubeFlashShaderOffset, false);
                break;
            case MapEvent.LightValueBlueFade:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFadeShaderOffset : cubeFadeShaderOffset, false);
                break;
            case MapEvent.LightValueRedON:
                e.UpdateOffset(Vector3.zero, false);
                e.ChangeBaseColor(color, false);
                break;
            case MapEvent.LightValueRedFlash:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFlashShaderOffset : cubeFlashShaderOffset, false);
                break;
            case MapEvent.LightValueRedFade:
                e.UpdateOffset(e.UsePyramidModel ? pyramidFadeShaderOffset : cubeFadeShaderOffset, false);
                break;
        }

        e.ChangeFadeSize(e.UsePyramidModel ? pyramidDefaultFadeSize : cubeDefaultFadeSize, false);

        if (Settings.Instance.VisualizeChromaGradients) e.UpdateGradientRendering();

        e.UpdateMaterials();
    }
}
