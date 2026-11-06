using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightColorBase
    {
        public const string CustomKeyEasing = "easing";

        public const string CustomKeyEasingType = "easingType";

        public static bool RequiresCustomEasing(int easing) =>
            (easing >= (int)EaseType.InQuadratic && easing <= (int)EaseType.InOutBounce)
            || (easing >= (int)EaseType.BeatSaberInOutBack && easing <= (int)EaseType.BeatSaberInOutBounce);

        public static BaseLightColorBase GetFromJson(JSONNode node)
        {
            var lightColorBase = new BaseLightColorBase();

            var iInt = node["i"].AsInt;
            lightColorBase.JsonTime = lightColorBase.RelativeJsonTime = node["b"].AsFloat;
            lightColorBase.Color = node["c"].AsInt;
            lightColorBase.Brightness = node["s"].AsFloat;
            lightColorBase.UsePrevious = iInt == (int)TransitionType.Extend ? 1 : 0;
            lightColorBase.Easing =
                (int)(iInt == (int)TransitionType.Instant ? EaseType.None : EaseType.Linear);
            lightColorBase.Frequency = node["f"].AsInt;
            lightColorBase.StrobeBrightness = node["sb"].AsFloat;
            lightColorBase.StrobeFade = node["sf"].AsInt;
            lightColorBase.CustomData = node["customData"];

            var transition = iInt;
            if ((transition == (int)TransitionType.Interpolate || transition == (int)TransitionType.Extend)
                && lightColorBase.CustomData.HasKey(CustomKeyEasing))
            {
                var customEasing = lightColorBase.CustomData[CustomKeyEasing];
                if (customEasing.IsNumber
                    && customEasing.AsDouble == customEasing.AsInt
                    && RequiresCustomEasing(customEasing.AsInt))
                {
                    lightColorBase.Easing = customEasing.AsInt;
                }
            }

            return lightColorBase;
        }

        public static JSONNode ToJson(BaseLightColorBase lightColorBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(lightColorBase.RelativeJsonTime);
            node["c"] = lightColorBase.Color;
            node["s"] = lightColorBase.Brightness;
            node["i"] = (int)(lightColorBase.UsePrevious == 1 ? TransitionType.Extend :
                lightColorBase.Easing == (int)EaseType.None   ? TransitionType.Instant : TransitionType.Interpolate);
            node["f"] = lightColorBase.Frequency;
            node["sb"] = lightColorBase.StrobeBrightness;
            node["sf"] = lightColorBase.StrobeFade;
            // An extension holds the previous color, so a saved curve cannot change its output.
            var requiresCustomEasing = lightColorBase.UsePrevious == 0 && RequiresCustomEasing(lightColorBase.Easing);
            if (requiresCustomEasing || lightColorBase.CustomData.HasKey(CustomKeyEasing))
                lightColorBase.SetCustomData(lightColorBase.CustomData.Clone());

            var customData = lightColorBase.SaveCustom();
            customData = StripExtensionEasingFromExport(lightColorBase, customData);
            if (requiresCustomEasing)
                customData[CustomKeyEasing] = lightColorBase.Easing;
            else
                customData.Remove(CustomKeyEasing);

            // Only normal nodes copy generated custom keys back to the editable node. Extensions use an
            // output-only copy.
            if (lightColorBase.UsePrevious == 0)
                lightColorBase.CustomData = customData;
            if (!customData.Children.Any())
                return node;
            node["customData"] = customData;
            return node;
        }

        // Clone before stripping easing so V3 and V4 exports cannot mutate the editable CustomData.
        public static JSONNode StripExtensionEasingFromExport(BaseLightColorBase evt, JSONNode customData)
        {
            if (evt.UsePrevious != 1)
                return customData;

            var exported = customData.Clone();
            exported.Remove(CustomKeyEasing);
            exported.Remove(evt.CustomKeyColorEasing);
            exported.Remove(evt.CustomKeyStrobeEasing);
            exported.Remove(evt.CustomKeyStrobeColorEasing);
            return exported;
        }
    }
}
