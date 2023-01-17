using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Shared;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3BasicEvent : BaseEvent
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
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
            time, type, value, floatValue, customData) =>
            ParseCustom();

        public override float? CustomPreciseSpeed
        {
            get => customSpeed;
            set => customSpeed = value;
        }

        public override float? CustomStepMult { get; set; }
        public override float? CustomPropMult { get; set; }
        public override float? CustomSpeedMult { get; set; }

        public override ChromaLightGradient CustomLightGradient
        {
            get => null;
            set
            {
            }
        }

        public override string CustomKeyTrack { get; } = "track";

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

        protected sealed override void ParseCustom() => base.ParseCustom();

        public override bool IsChroma() =>
            CustomData != null &&
            ((CustomData.HasKey("color") && CustomData["color"].IsArray) ||
             (CustomData.HasKey("lightID") &&
              (CustomData["lightID"].IsArray || CustomData["lightID"].IsNumber)) ||
             (CustomData.HasKey("easing") && CustomData["easing"].IsString) ||
             (CustomData.HasKey("lerpType") && CustomData["lerpType"].IsString) ||
             (CustomData.HasKey("nameFilter") && CustomData["nameFilter"].IsString) ||
             (CustomData.HasKey("reset") && CustomData["reset"].IsBoolean) ||
             (CustomData.HasKey("rotation") && CustomData["rotation"].IsNumber) ||
             (CustomData.HasKey("step") && CustomData["step"].IsNumber) ||
             (CustomData.HasKey("prop") && CustomData["prop"].IsNumber) ||
             (CustomData.HasKey("speed") && CustomData["speed"].IsNumber) ||
             (CustomData.HasKey("direction") && CustomData["direction"].IsNumber) ||
             (CustomData.HasKey("lockRotation") && CustomData["lockRotation"].IsBoolean) ||
             (CustomData.HasKey("direction") && CustomData["direction"].IsNumber));

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Time;
            node["et"] = Type;
            node["i"] = Value;
            node["f"] = FloatValue;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3BasicEvent(Time, Type, Value, FloatValue, SaveCustom().Clone());
    }
}
