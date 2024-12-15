using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

namespace Beatmap.V2
{
    public class V2Event
    {
        public const string CustomKeyTrack = "_track";

        public const string CustomKeyColor = "_color";

        public const string CustomKeyPropID = "_propID";

        public const string CustomKeyLightID = "_lightID";

        public const string CustomKeyLerpType = "_lerpType";

        public const string CustomKeyEasing = "_easing";

        public const string CustomKeyLightGradient = "_lightGradient";

        public const string CustomKeyStep = "_step";

        public const string CustomKeyProp = "_prop";

        public const string CustomKeySpeed = "_speed";

        public const string CustomKeyRingRotation = "_rotation";

        public const string CustomKeyStepMult = "_stepMult";

        public const string CustomKeyPropMult = "_propMult";

        public const string CustomKeySpeedMult = "_speedMult";

        public const string CustomKeyPreciseSpeed = "_preciseSpeed";

        public const string CustomKeyDirection = "_direction";

        public const string CustomKeyLockRotation = "_lockPosition";

        public const string CustomKeyLaneRotation = "_rotation";

        public const string CustomKeyNameFilter = "_nameFilter";

        public static BaseEvent GetFromJson(JSONNode node)
        {
            var evt = new BaseEvent();
            
            evt.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            evt.Type = BaseItem.GetRequiredNode(node, "_type").AsInt;
            evt.Value = BaseItem.GetRequiredNode(node, "_value").AsInt;
            evt.FloatValue = node.HasKey("_floatValue") ? node["_floatValue"].AsFloat : 1f;
            evt.CustomData = node["_customData"];

            return evt;
        }
        
        public static JSONNode ToJson(BaseEvent evt)
        {
            JSONNode node = new JSONObject();
            node["_time"] = evt.JsonTime;
            node["_type"] = evt.Type;
            node["_value"] = evt.Value;
            node["_floatValue"] = evt.FloatValue;
            evt.CustomData = evt.SaveCustom();
            if (!evt.CustomData.Children.Any()) return node;
            node["_customData"] = evt.CustomData;
            return node;
        }
    }
}
