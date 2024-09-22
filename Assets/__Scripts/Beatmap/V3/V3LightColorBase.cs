using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightColorBase
    {
        public static BaseLightColorBase GetFromJson(JSONNode node)
        {
            var lightColorBase = new BaseLightColorBase();
            
            lightColorBase.JsonTime = node["b"].AsFloat;
            lightColorBase.Color = node["c"].AsInt;
            lightColorBase.Brightness = node["s"].AsFloat;
            lightColorBase.TransitionType = node["i"].AsInt;
            lightColorBase.Frequency = node["f"].AsInt;
            lightColorBase.StrobeBrightness = node["sb"].AsFloat;
            lightColorBase.StrobeFade = node["sf"].AsInt;
            lightColorBase.CustomData = node["customData"];

            return lightColorBase;
        }

        public static JSONNode ToJson(BaseLightColorBase lightColorBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = lightColorBase.JsonTime;
            node["c"] = lightColorBase.Color;
            node["s"] = lightColorBase.Brightness;
            node["i"] = lightColorBase.TransitionType;
            node["f"] = lightColorBase.Frequency;
            node["sb"] = lightColorBase.StrobeBrightness;
            node["sf"] = lightColorBase.StrobeFade;
            lightColorBase.CustomData = lightColorBase.SaveCustom();
            if (!lightColorBase.CustomData.Children.Any()) return node;
            node["customData"] = lightColorBase.CustomData;
            return node;
        }
    }
}
