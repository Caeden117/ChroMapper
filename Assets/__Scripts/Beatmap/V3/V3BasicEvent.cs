using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BasicEvent
    {
        public const string CustomKeyTrack = "track";

        public const string CustomKeyColor = "color";

        public const string CustomKeyPropID = "propID";

        public const string CustomKeyLightID = "lightID";

        public const string CustomKeyLerpType = "lerpType";

        public const string CustomKeyEasing = "easing";

        public const string CustomKeyLightGradient = "lightGradient";

        public const string CustomKeyStep = "step";

        public const string CustomKeyProp = "prop";

        public const string CustomKeySpeed = "speed";

        public const string CustomKeyRingRotation = "rotation";

        public const string CustomKeyStepMult = "stepMult";

        public const string CustomKeyPropMult = "propMult";

        public const string CustomKeySpeedMult = "speedMult";

        public const string CustomKeyPreciseSpeed = "preciseSpeed";

        public const string CustomKeyDirection = "direction";

        public const string CustomKeyLockRotation = "lockRotation";

        public const string CustomKeyLaneRotation = "rotation";

        public const string CustomKeyNameFilter = "nameFilter";
        
        public static BaseEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = node["b"].AsFloat;
            evt.Type = node["et"].AsInt;
            evt.Value = node["i"].AsInt;
            evt.FloatValue = node["f"].AsFloat;
            evt.CustomData = node["customData"];

            return evt;
        }

        public static JSONNode ToJson(BaseEvent evt)
        {
            JSONNode node = new JSONObject();
            node["b"] = evt.JsonTime;
            node["et"] = evt.Type;
            node["i"] = evt.Value;
            node["f"] = evt.FloatValue;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["customData"] = evt.CustomData;
            return node;
        }
    }
}
