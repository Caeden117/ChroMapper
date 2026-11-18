using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightTranslationBase
    {
        public static BaseLightTranslationBase GetFromJson(JSONNode node)
        {
            var lightTranslationBase = new BaseLightTranslationBase();

            lightTranslationBase.JsonTime = lightTranslationBase.RelativeJsonTime = node["b"].AsFloat;
            lightTranslationBase.UsePrevious = node["p"].AsInt;
            lightTranslationBase.Easing = node["e"].AsInt;
            lightTranslationBase.Translation = node["t"].AsFloat;
            lightTranslationBase.CustomData = node["customData"];

            return lightTranslationBase;
        }

        public static JSONNode ToJson(BaseLightTranslationBase lightTranslationBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = JSONNumber.RoundBeat(lightTranslationBase.RelativeJsonTime);
            node["p"] = lightTranslationBase.UsePrevious;
            // Translation extensions hold the previous position. Omit the curve that would interpolate
            // between identical values.
            if (lightTranslationBase.UsePrevious == 0)
                node["e"] = lightTranslationBase.Easing;
            node["t"] = lightTranslationBase.Translation;
            lightTranslationBase.CustomData = lightTranslationBase.SaveCustom();
            if (!lightTranslationBase.CustomData.Children.Any()) return node;
            node["customData"] = lightTranslationBase.CustomData;
            return node;
        }
    }
}
