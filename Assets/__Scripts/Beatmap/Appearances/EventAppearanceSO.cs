using System.Globalization;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    public enum EventModelType
    {
        Block = 0,
        Pyramid = 1,
        FlatPyramid = 2
    }

    [CreateAssetMenu(menuName = "Beatmap/Appearance/Event Appearance SO", fileName = "EventAppearanceSO")]
    public class EventAppearanceSO : ScriptableObject
    {
        [Space(5)][SerializeField] private GameObject laserSpeedPrefab;

        [Space(5)][Header("Default Colors")] public Color RedColor;
        public Color BlueColor;
        public Color WhiteColor = new Color(0.7264151f, 0.7264151f, 0.7264151f);
        public Color RedBoostColor;
        public Color BlueBoostColor;
        public Color WhiteBoostColor = new Color(0.7264151f, 0.7264151f, 0.7264151f);

        [SerializeField] private Color offColor;

        [Header("Other Event Colors")]
        [SerializeField]
        private Color ringEventsColor;

        [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
        [SerializeField]
        private Color otherColor;

        public void SetEventAppearance(EventContainer e, bool final = true, bool boost = false)
        {
            var color = Color.white;
            var envName = EnvironmentInfoHelper.GetName();
            e.UpdateOffset(Vector3.zero, false);
            e.UpdateAlpha(final ? 1.0f : 0.6f, false);
            e.UpdateScale(final ? 0.75f : 0.6f);
            e.ChangeSpotlightSize(1f, false);
            if (e.EventData.IsLaneRotationEvent() || e.EventData.IsLaserRotationEvent(envName) ||
                e.EventData.IsUtilityEvent(envName))
            {
                if (e.EventData.IsLaneRotationEvent())
                {
                    var rotation = e.EventData.Rotation;
                    e.UpdateTextDisplay(true, $"{rotation}°");
                }
                else if (e.EventData.IsLaserRotationEvent(envName) || e.EventData.IsUtilityEvent(envName))
                {
                    float speed = e.EventData.Value;
                    if (e.EventData.CustomSpeed != null) speed = (float)e.EventData.CustomSpeed;

                    e.UpdateTextDisplay(true, speed.ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                e.UpdateTextDisplay(false);
            }

            if (!e.EventData.IsLightEvent(envName))
            {
                e.EventModel = EventModelType.Block;

                switch (envName)
                {
                    case "InterscopeEnvironment":
                        if (e.EventData.Type is (int)EventTypeValue.RingRotation
                            or (int)EventTypeValue.UtilityEvent1
                            or (int)EventTypeValue.UtilityEvent2)
                        {
                            e.UpdateTextDisplay(true, e.EventData.Value.ToString());
                        }

                        break;
                    case "BillieEnvironment":
                        if (e.EventData.Type == (int)EventTypeValue.RingRotation)
                        {
                            e.UpdateTextDisplay(true, e.EventData.Value.ToString());
                        }

                        break;
                }

                if (e.EventData.IsRingEvent(envName))
                {
                    e.ChangeColor(ringEventsColor, false);
                    e.ChangeBaseColor(ringEventsColor, false);
                }
                else if (e.EventData.Type == (int)EventTypeValue.ColorBoost)
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
                    e.UpdateGradientRendering();
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
            else if (e.EventData.IsOff)
            {
                color = offColor;
            }
            else if (e.EventData.IsBlue)
            {
                color = boost ? BlueBoostColor : BlueColor;
            }
            else if (e.EventData.IsRed)
            {
                color = boost ? RedBoostColor : RedColor;
            }
            else if (e.EventData.IsWhite)
            {
                color = boost ? WhiteBoostColor : WhiteColor;
            }

            if (Settings.Instance.EmulateChromaLite && e.EventData.CustomColor != null && !e.EventData.IsOff
                    && !e.EventData.IsWhite) // White overrides Chroma
            {
                color = e.EventData.CustomColor.Value;
            }

            // Display floatValue only where used
            if (e.EventData.IsLightEvent(envName) && e.EventData.Value != 0)
            {
                if (Settings.Instance.DisplayFloatValueText)
                {
                    var text = e.EventData.IsTransition
                        ? $"T{Mathf.RoundToInt(e.EventData.FloatValue * 100)}"
                        : $"{Mathf.RoundToInt(e.EventData.FloatValue * 100)}";
                    e.UpdateTextDisplay(true, text); 
                }

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(offColor, color, 0.25f);
                color = Color.Lerp(clampedOffColor, color, e.EventData.FloatValue);
            }

            e.EventModel = Settings.Instance.EventModel;
            e.ChangeColor(color, false);
            e.ChangeBaseColor(Color.black, false);
            switch (e.EventData.Value)
            {
                case (int)LightValue.Off:
                    e.ChangeColor(offColor, false);
                    e.ChangeBaseColor(offColor, false);
                    e.UpdateOffset(Vector3.zero, false);
                    break;
                case (int)LightValue.BlueOn:
                case (int)LightValue.RedOn:
                case (int)LightValue.WhiteOn:
                    e.UpdateOffset(Vector3.zero, false);
                    e.ChangeBaseColor(color, false);
                    break;
                case (int)LightValue.BlueFlash:
                case (int)LightValue.RedFlash:
                case (int)LightValue.WhiteFlash:
                    e.UpdateOffset(e.FlashShaderOffset, false);
                    break;
                case (int)LightValue.BlueFade:
                case (int)LightValue.RedFade:
                case (int)LightValue.WhiteFade:
                    e.UpdateOffset(e.FadeShaderOffset, false);
                    break;
                case (int)LightValue.BlueTransition:
                case (int)LightValue.RedTransition:
                case (int)LightValue.WhiteTransition:
                    e.ChangeBaseColor(color, false);
                    break;
            }

            e.ChangeFadeSize(e.DefaultFadeSize, false);

            // At this point, next Event must be a light event.
            Color? nextColor = null;
            var nextEvent = e.EventData.Next;
            if (!e.EventData.IsFade && !e.EventData.IsFlash && nextEvent != null && nextEvent.IsTransition)
            {
                if (nextEvent.IsBlue)
                {
                    nextColor = boost ? BlueBoostColor : BlueColor;
                }
                else if (nextEvent.IsRed)
                {
                    nextColor = boost ? RedBoostColor : RedColor;
                }
                else if (nextEvent.IsWhite)
                {
                    nextColor = boost ? WhiteBoostColor : WhiteColor;
                }

                if (Settings.Instance.EmulateChromaLite && nextEvent.CustomColor != null && !nextEvent.IsWhite) // White overrides Chroma
                {
                    nextColor = nextEvent.CustomColor.Value;
                }

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(offColor, nextColor.Value, 0.25f);
                nextColor = Color.Lerp(clampedOffColor, nextColor.Value, nextEvent.FloatValue);
            }

            if (Settings.Instance.VisualizeChromaGradients)
            {
                e.UpdateGradientRendering(color, nextColor, e.EventData?.CustomEasing ?? "easeLinear");
            }

            e.UpdateMaterials();
        }
    }
}
