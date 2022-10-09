using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3BasicEvent : BaseEvent
    {
        public V3BasicEvent()
        {
        }

        public V3BasicEvent(BaseEvent other) : base(other) => ParseCustom();

        public V3BasicEvent(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            Type = RetrieveRequiredNode(node, "et").AsInt;
            Value = RetrieveRequiredNode(node, "i").AsInt;
            FloatValue = RetrieveRequiredNode(node, "f").AsFloat;
            CustomData = node["customData"];
            ParseCustom();
        }

        public V3BasicEvent(float time, int type, int value, float floatValue = 1f, JSONNode customData = null) : base(
            time, type,
            value, floatValue, customData) =>
            ParseCustom();

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override int? CustomPropID
        {
            get => -1;
            set { }
        }

        public override float? CustomPreciseSpeed
        {
            get => _customSpeed;
            set
            {
                if (value == null && CustomData?[CustomKeySpeed] != null)
                {
                    CustomData.Remove(CustomKeySpeed);
                }
                else
                {
                    GetOrCreateCustom()[CustomKeySpeed] = value;
                }
                _customSpeed = value;
            }
        }

        public override float? CustomStepMult { get; set; }
        public override float? CustomPropMult { get; set; }
        public override float? CustomSpeedMult { get; set; }
        public override ChromaLightGradient CustomLightGradient { get; set; }

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyPropID { get; } = "propID";

        public override string CustomKeyLightID { get; } = "lightID";

        public override string CustomKeyLerpType { get; } = "lerpType";

        public override string CustomKeyEasing { get; } = "easing";

        public override string CustomKeyLightGradient { get; } = "lightGradient";

        public override string CustomKeyStep { get; } = "step";

        public override string CustomKeyProp { get; } = "prop";

        public override string CustomKeySpeed { get; } = "speed";

        public override string CustomKeyStepMult { get; } = "stepMult";

        public override string CustomKeyPropMult { get; } = "propMult";

        public override string CustomKeySpeedMult { get; } = "speedMult";

        public override string CustomKeyPreciseSpeed { get; } = "preciseSpeed";

        public override string CustomKeyDirection { get; } = "direction";

        public override string CustomKeyLockRotation { get; } = "lockRotation";

        public override string CustomKeyLaneRotation { get; } = "rotation";
        
        public override string CustomKeyNameFilter { get; } = "nameFilter";

        public override bool IsPropagation { get; } = false;

        public override bool IsChroma() =>
            (CustomData?["color"] != null && CustomData["color"].IsArray) ||
            (CustomData?["lightID"] != null &&
             (CustomData["lightID"].IsArray || CustomData["lightID"].IsNumber)) ||
            (CustomData?["easing"] != null && CustomData["easing"].IsString) ||
            (CustomData?["lerpType"] != null && CustomData["lerpType"].IsString) ||
            (CustomData?["nameFilter"] != null && CustomData["nameFilter"].IsString) ||
            (CustomData?["reset"] != null && CustomData["reset"].IsBoolean) ||
            (CustomData?["rotation"] != null && CustomData["rotation"].IsNumber) ||
            (CustomData?["step"] != null && CustomData["step"].IsNumber) ||
            (CustomData?["prop"] != null && CustomData["prop"].IsNumber) ||
            (CustomData?["speed"] != null && CustomData["speed"].IsNumber) ||
            (CustomData?["direction"] != null && CustomData["direction"].IsNumber) ||
            (CustomData?["lockRotation"] != null && CustomData["lockRotation"].IsBoolean) ||
            (CustomData?["direction"] != null && CustomData["direction"].IsNumber);

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Time;
            node["et"] = Type;
            node["i"] = Value;
            node["f"] = FloatValue;
            if (CustomData == null) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3BasicEvent(Time, Type, Value, FloatValue, CustomData?.Clone());
    }
}
