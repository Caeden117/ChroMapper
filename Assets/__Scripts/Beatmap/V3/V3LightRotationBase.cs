using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightRotationBase
    {
        public static BaseLightRotationBase GetFromJson(JSONNode node)
        {
            var lightRotationBase = new BaseLightRotationBase();
            
            lightRotationBase.JsonTime = node["b"].AsFloat;
            lightRotationBase.Rotation = node["r"].AsFloat;
            lightRotationBase.Direction = node["o"].AsInt;
            lightRotationBase.EaseType = node["e"].AsInt;
            lightRotationBase.Loop = node["l"].AsInt;
            lightRotationBase.UsePrevious = node["p"].AsInt;
            lightRotationBase.CustomData = node["customData"];

            return lightRotationBase;
        }

        public static JSONNode ToJson(BaseLightRotationBase lightRotationBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = lightRotationBase.JsonTime;
            node["r"] = lightRotationBase.Rotation;
            node["o"] = lightRotationBase.Direction;
            node["e"] = lightRotationBase.EaseType;
            node["l"] = lightRotationBase.Loop;
            node["p"] = lightRotationBase.UsePrevious;
            lightRotationBase.CustomData = lightRotationBase.SaveCustom();
            if (!lightRotationBase.CustomData.Children.Any()) return node;
            node["customData"] = lightRotationBase.CustomData;
            return node;
        }
    }
}
