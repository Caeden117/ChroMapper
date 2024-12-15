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
            
            lightTranslationBase.JsonTime = node["b"].AsFloat;
            lightTranslationBase.UsePrevious = node["p"].AsInt;
            lightTranslationBase.EaseType = node["e"].AsInt;
            lightTranslationBase.Translation = node["t"].AsFloat;
            lightTranslationBase.CustomData = node["customData"];

            return lightTranslationBase;
        }

        public static JSONNode ToJson(BaseLightTranslationBase lightTranslationBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = lightTranslationBase.JsonTime;
            node["p"] = lightTranslationBase.UsePrevious;
            node["e"] = lightTranslationBase.EaseType;
            node["t"] = lightTranslationBase.Translation;
            lightTranslationBase.CustomData = lightTranslationBase.SaveCustom();
            if (!lightTranslationBase.CustomData.Children.Any()) return node;
            node["customData"] = lightTranslationBase.CustomData;
            return node;
        }
    }
}
